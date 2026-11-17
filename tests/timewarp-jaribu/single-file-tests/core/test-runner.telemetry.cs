#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.telemetry.fixtures.cs

#region Purpose
// Proves per-test span shape, outcome mapping, TRACEPARENT parenting, and OTLP flush.
#endregion

#region Design
// Fixtures are not registered. Telemetry_Given_ drives them through RunTestsAsync
// under an ActivityListener. The export case launches telemetry-export-probe.cs
// so the provider sees the endpoint at process start. The quiet half leaves the
// base endpoint blank and points OTEL_EXPORTER_OTLP_TRACES_ENDPOINT at its listener.
// Clearing Activity.Current for the TRACEPARENT case keeps the outer test span
// from becoming the parent. The listener is disposed before this method returns,
// so the outer span is not in the capture.
// Fixtures and the capture types live in a sibling file so this file stays
// within the line cap. A single-file run includes that sibling.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using TimeWarp.Amuru;

  [TestTag("Jaribu")]
  public class Telemetry_Given_
  {
    private const string TraceId = "07107107107107107107107107107107";
    private const string ParentSpanId = "b071b071b071b071";

    [ModuleInitializer]
    internal static void Register() => RegisterTests<Telemetry_Given_>();

    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task Cases_Should_EmitClassMethodAndHookSpans()
    {
      using TelemetryCapture capture = new();
      await TestRunner.RunTestsAsync<TelemetrySpanFixture>(NullSink.Instance);

      string className = typeof(TelemetrySpanFixture).FullName!;
      CapturedSpan classSpan = capture.ForClass(className).Single(span =>
        span.Name.StartsWith(JaribuTelemetry.ClassSpanPrefix, StringComparison.Ordinal));
      classSpan.Tags[JaribuTelemetry.ClassAttribute].ShouldBe(className);
      classSpan.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeFailed);
      classSpan.Status.ShouldBe(ActivityStatusCode.Error);

      List<CapturedSpan> tests = capture.TestsFor(className);
      tests.Count.ShouldBe(6);

      CapturedSpan passed = One(tests, nameof(TelemetrySpanFixture.Should_Pass));
      passed.ParentSpanId.ShouldBe(classSpan.SpanId);
      passed.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomePassed);
      passed.Status.ShouldBe(ActivityStatusCode.Ok);
      passed.Tags.ContainsKey(JaribuTelemetry.CaseIndexAttribute).ShouldBeFalse();

      CapturedSpan failed = One(tests, nameof(TelemetrySpanFixture.Should_Fail));
      failed.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeFailed);
      failed.Status.ShouldBe(ActivityStatusCode.Error);

      CapturedSpan skipped = One(tests, nameof(TelemetrySpanFixture.Should_Skip));
      skipped.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeSkipped);
      skipped.Status.ShouldBe(ActivityStatusCode.Unset);

      CapturedSpan timedOut = One(tests, nameof(TelemetrySpanFixture.Should_TimeOut));
      timedOut.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeTimedOut);
      timedOut.Status.ShouldBe(ActivityStatusCode.Error);

      CapturedSpan alpha = One(tests, nameof(TelemetrySpanFixture.Should_EchoInput) + "[0]");
      alpha.Tags[JaribuTelemetry.CaseIndexAttribute].ShouldBe("0");
      alpha.Tags[JaribuTelemetry.CaseArgumentsAttribute].ShouldBe("\"alpha\"");
      alpha.Tags[JaribuTelemetry.MethodAttribute].ShouldBe(nameof(TelemetrySpanFixture.Should_EchoInput));
      alpha.ParentSpanId.ShouldBe(classSpan.SpanId);

      CapturedSpan beta = One(tests, nameof(TelemetrySpanFixture.Should_EchoInput) + "[1]");
      beta.Tags[JaribuTelemetry.CaseIndexAttribute].ShouldBe("1");
      beta.Tags[JaribuTelemetry.CaseArgumentsAttribute].ShouldBe("\"beta\"");

      List<CapturedSpan> hooks = capture.Children(classSpan.SpanId, JaribuTelemetry.HookSpanPrefix);
      hooks.ShouldContain(span => span.Tags[JaribuTelemetry.HookNameAttribute] == JaribuTelemetry.HookSetupOnce);
      hooks.ShouldContain(span => span.Tags[JaribuTelemetry.HookNameAttribute] == JaribuTelemetry.HookCleanUpOnce);

      List<CapturedSpan> setup = capture.Children(passed.SpanId, JaribuTelemetry.HookSpanPrefix);
      setup.ShouldContain(span => span.Tags[JaribuTelemetry.HookNameAttribute] == JaribuTelemetry.HookSetup);
      setup.ShouldContain(span => span.Tags[JaribuTelemetry.HookNameAttribute] == JaribuTelemetry.HookCleanUp);
      capture.Children(skipped.SpanId, JaribuTelemetry.HookSpanPrefix).ShouldBeEmpty();
    }

    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task FaultingSetupOnce_Should_MarkTheHookFailed()
    {
      using TelemetryCapture capture = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<TelemetryFaultingSetupOnceFixture>(NullSink.Instance);
      stats.Success.ShouldBeFalse();

      string className = typeof(TelemetryFaultingSetupOnceFixture).FullName!;
      CapturedSpan classSpan = capture.ForClass(className).Single(span =>
        span.Name.StartsWith(JaribuTelemetry.ClassSpanPrefix, StringComparison.Ordinal));
      CapturedSpan hook = capture.Children(classSpan.SpanId, JaribuTelemetry.HookSpanPrefix)
        .Single(span => span.Tags[JaribuTelemetry.HookNameAttribute] == JaribuTelemetry.HookSetupOnce);
      hook.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeFailed);
      hook.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task TraceParent_Should_ParentTheClassSpan()
    {
      string? previousParent = Environment.GetEnvironmentVariable("TRACEPARENT");
      string? previousState = Environment.GetEnvironmentVariable("TRACESTATE");
      Activity? ambient = Activity.Current;
      Environment.SetEnvironmentVariable(
        "TRACEPARENT",
        $"00-{TraceId}-{ParentSpanId}-01");
      Environment.SetEnvironmentVariable("TRACESTATE", "vendor=jaribu");
      Activity.Current = null;
      try
      {
        using TelemetryCapture capture = new();
        await TestRunner.RunTestsAsync<TelemetryParentFixture>(NullSink.Instance);
        CapturedSpan classSpan = capture.ForClass(typeof(TelemetryParentFixture).FullName!)
          .Single(span => span.Name.StartsWith(JaribuTelemetry.ClassSpanPrefix, StringComparison.Ordinal));
        classSpan.TraceId.ShouldBe(TraceId);
        classSpan.ParentSpanId.ShouldBe(ParentSpanId);
        CapturedSpan test = capture.TestsFor(typeof(TelemetryParentFixture).FullName!).Single();
        test.TraceId.ShouldBe(TraceId);
        test.ParentSpanId.ShouldBe(classSpan.SpanId);
      }
      finally
      {
        Activity.Current = ambient;
        Environment.SetEnvironmentVariable("TRACEPARENT", previousParent);
        Environment.SetEnvironmentVariable("TRACESTATE", previousState);
      }
    }

    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task InvalidTraceParent_Should_StartARootSpan()
    {
      string? previousParent = Environment.GetEnvironmentVariable("TRACEPARENT");
      Activity? ambient = Activity.Current;
      Environment.SetEnvironmentVariable("TRACEPARENT", "not-a-traceparent");
      Activity.Current = null;
      try
      {
        using TelemetryCapture capture = new();
        await TestRunner.RunTestsAsync<TelemetryParentFixture>(NullSink.Instance);
        CapturedSpan classSpan = capture.ForClass(typeof(TelemetryParentFixture).FullName!)
          .Single(span => span.Name.StartsWith(JaribuTelemetry.ClassSpanPrefix, StringComparison.Ordinal));
        classSpan.ParentSpanId.ShouldBe("0000000000000000");
        classSpan.TraceId.ShouldNotBe(TraceId);
      }
      finally
      {
        Activity.Current = ambient;
        Environment.SetEnvironmentVariable("TRACEPARENT", previousParent);
      }
    }

    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task Cancellation_Should_MarkTheSpanCancelled()
    {
      TelemetryCancelFixture.Reset();
      using CancellationTokenSource cts = new();
      using TelemetryCapture capture = new();
      Task<TestRunStats> run = TestRunner.RunTestsAsync<TelemetryCancelFixture>(
        NullSink.Instance,
        cancellationToken: cts.Token);
      await TelemetryCancelFixture.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
      cts.Cancel();
      try
      {
        TestRunStats stats = await run.WaitAsync(TimeSpan.FromSeconds(5));
        stats.Success.ShouldBeFalse();
      }
      finally
      {
        TelemetryCancelFixture.Release.TrySetResult(true);
      }

      CapturedSpan test = capture.TestsFor(typeof(TelemetryCancelFixture).FullName!).Single();
      test.Tags[JaribuTelemetry.OutcomeAttribute].ShouldBe(JaribuTelemetry.OutcomeCancelled);
      test.Status.ShouldBe(ActivityStatusCode.Error);
    }

    [Proves(typeof(JaribuTelemetry), nameof(JaribuTelemetry.SetSourceFile))]
    public static async Task SourceFile_Should_StickToTheCurrentJaribuSpan()
    {
      using TelemetryCapture capture = new();
      using Activity? activity = JaribuTelemetry.ActivitySource.StartActivity("jaribu.test source");
      activity.ShouldNotBeNull();
      JaribuTelemetry.SetSourceFile("tests/example.cs");
      JaribuTelemetry.SetSourceFile(null);
      activity.SetTag(JaribuTelemetry.OutcomeAttribute, JaribuTelemetry.OutcomePassed);
      activity.Dispose();

      CapturedSpan span = capture.Spans.Single(item => item.Name == "jaribu.test source");
      span.Tags[JaribuTelemetry.SourceFileAttribute].ShouldBe("tests/example.cs");
      await Task.CompletedTask;
    }

    [KnownTautology("child process asserts the OTLP payload, and a traces endpoint alone stays quiet")]
    public static async Task Endpoint_Should_ExportOnFailureAndStayQuietWhenUnset()
    {
      string root = FindRepoRoot();
      string probe = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "telemetry-export-probe.cs");
      using TelemetryListener listener = TelemetryListener.Start();
      int exitCode = await LaunchProbe(
        root,
        probe,
        listener.Endpoint,
        $"00-{TraceId}-{ParentSpanId}-01");
      exitCode.ShouldBe(1);
      byte[] payload = listener.TakeBody();
      payload.Length.ShouldBeGreaterThan(0);
      Contains(payload, "jaribu.test").ShouldBeTrue();
      Contains(payload, "jaribu.class").ShouldBeTrue();
      Contains(payload, "jaribu.hook").ShouldBeTrue();
      Contains(payload, Convert.FromHexString(TraceId)).ShouldBeTrue();
      Contains(payload, Convert.FromHexString(ParentSpanId)).ShouldBeTrue();

      using TelemetryListener quiet = TelemetryListener.Start();
      int quietExit = await LaunchProbe(
        root,
        probe,
        endpoint: null,
        traceParent: null,
        tracesEndpoint: $"{quiet.Endpoint}/v1/traces");
      quietExit.ShouldBe(1);
      byte[] quietPayload = quiet.TakeBody();
      quietPayload.Length.ShouldBe(0);
    }

    private static CapturedSpan One(List<CapturedSpan> tests, string suffix)
    {
      return tests.Single(span => span.Name.EndsWith(suffix, StringComparison.Ordinal));
    }

    private static Task<int> LaunchProbe(
      string root,
      string probe,
      string? endpoint,
      string? traceParent,
      string? tracesEndpoint = null)
    {
      bool export = endpoint is not null || tracesEndpoint is not null;
      return Shell.Builder("dotnet")
        .WithArguments("run", probe)
        .WithWorkingDirectory(root)
        .WithEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint ?? string.Empty)
        .WithEnvironmentVariable(
          "OTEL_EXPORTER_OTLP_TRACES_ENDPOINT",
          tracesEndpoint ?? endpoint ?? string.Empty)
        .WithEnvironmentVariable(
          "OTEL_EXPORTER_OTLP_PROTOCOL",
          export ? "http/protobuf" : string.Empty)
        .WithEnvironmentVariable("TRACEPARENT", traceParent ?? string.Empty)
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3))
        .RunAsync();
    }

    private static string FindRepoRoot()
    {
      DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
      while (directory is not null)
      {
        if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      throw new InvalidOperationException(
        $"Repository root not found from '{Directory.GetCurrentDirectory()}'.");
    }

    private static bool Contains(byte[] haystack, string needle)
    {
      return Contains(haystack, System.Text.Encoding.UTF8.GetBytes(needle));
    }

    private static bool Contains(byte[] haystack, byte[] needle)
    {
      return haystack.AsSpan().IndexOf(needle) >= 0;
    }
  }
}
