#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:project $(SourceDirectory)timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj

#region Purpose
// MTP checks for sequence parameter text, missing-exception messages, real timing, PDB misses, and extension identity.
#endregion

#region Design
// MtpSink, MethodSourceLocation, JaribuTestFramework, and the two extensions are internal.
// InternalsVisibleTo names run-ci-tests, timewarp-jaribu-tests-mtp, and this file.
// Timing is asserted from the timestamps on the node. A duration with no timestamps publishes none.
// An unsupported request publishes one Error node and returns. A bus that throws makes
// an AggregateException that keeps the original exception and the publish failure.
// On a host that cannot emit a DynamicMethod, the PDB case is a smoke check of an ordinary method.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Mtp_
{
  using System.IO.Compression;
  using System.Reflection;
  using System.Reflection.Emit;
  using System.Runtime.CompilerServices;
  using DevCli.Commands;
  using Microsoft.Testing.Platform.Extensions;
  using Microsoft.Testing.Platform.Extensions.Messages;
  using Microsoft.Testing.Platform.Extensions.TestFramework;
  using Microsoft.Testing.Platform.Messages;
  using Microsoft.Testing.Platform.Requests;
  using Microsoft.Testing.Platform.TestHost;
  using TimeWarp.Jaribu.TestingPlatform;

  [TestTag("Jaribu")]
  public class ReviewCleanups_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ReviewCleanups_Given_>();

    public static async Task FormatParameter_Should_PrintSequenceElementsAndLeaveStringsUnquoted()
    {
      List<int> numbers = [1, 2];
      List<string> words = ["a", "b"];
      MtpNodeMetadata.FormatParameter(numbers).ShouldBe("[1,2]");
      MtpNodeMetadata.FormatParameter(words).ShouldBe("[a,b]");
      MtpNodeMetadata.FormatParameter("fail").ShouldBe("fail");
      MtpNodeMetadata.FormatParameter(null).ShouldBe("null");
      await Task.CompletedTask;
    }

    public static async Task MissingException_Should_SayTheCoreSuppliedNone()
    {
      await AssertMissingAsync(TestNodeState.Failed);
      await AssertMissingAsync(TestNodeState.Timeout);
      await AssertMissingAsync(TestNodeState.Error);
    }

    public static async Task Timing_Should_UseNodeTimestamps()
    {
      DateTimeOffset start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
      DateTimeOffset end = start.AddMilliseconds(12);
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));

      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Timed",
        "Timed",
        TestNodeState.Passed,
        Duration: TimeSpan.FromMilliseconds(12),
        StartTime: start,
        EndTime: end));

      TimingInfo timing = bus.Messages.Single().TestNode.Properties.Single<TimingProperty>().GlobalTiming;
      timing.StartTime.ShouldBe(start);
      timing.EndTime.ShouldBe(end);
      timing.Duration.ShouldBe(TimeSpan.FromMilliseconds(12));
    }

    public static async Task DurationWithoutTimestamps_Should_PublishNoTiming()
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));

      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.DurationOnly",
        "DurationOnly",
        TestNodeState.Passed,
        Duration: TimeSpan.FromMilliseconds(12)));

      bus.Messages.Single().TestNode.Properties.OfType<TimingProperty>().ShouldBeEmpty();
    }

    public static async Task PdbLookup_Should_MissADynamicMethod_SmokeCheckWhenDynamicCodeIsUnsupported()
    {
      if (!RuntimeFeature.IsDynamicCodeSupported)
      {
        MethodInfo fallback = typeof(ReviewCleanups_Given_).GetMethod(
          nameof(PdbLookup_Should_MissADynamicMethod_SmokeCheckWhenDynamicCodeIsUnsupported))!;
        Should.NotThrow(() => MethodSourceLocation.TryCreate(fallback));
        await Task.CompletedTask;
        return;
      }

      DynamicMethod method = new("DynamicProbe", typeof(void), Type.EmptyTypes);
      MethodSourceLocation.TryCreate(method).ShouldBeNull();
      await Task.CompletedTask;
    }

    public static async Task UnsupportedRequest_Should_PublishAnErrorAndReturn()
    {
      RecordingBus bus = new();
      CompletionNotifier notifier = new();
      JaribuTestFramework framework = new(new JaribuExtension(), new UnusedServices());

      await framework.ExecuteRequestAsync(UnsupportedContext(bus, notifier));

      notifier.Completed.ShouldBeTrue();
      TestNode node = bus.Messages.Single().TestNode;
      Exception exception = ExceptionOf(node, TestNodeState.Error);
      exception.ShouldBeOfType<NotSupportedException>();
      exception.Message.ShouldContain("Unsupported MTP request type");
    }

    public static async Task UnsupportedRequest_Should_AggregateAFailedPublishWithTheOriginal()
    {
      CompletionNotifier notifier = new();
      JaribuTestFramework framework = new(new JaribuExtension(), new UnusedServices());
      InvalidOperationException publishFailure = new("publish failed");

      AggregateException aggregate = await Should.ThrowAsync<AggregateException>(
        async () => await framework.ExecuteRequestAsync(UnsupportedContext(new ThrowingBus(publishFailure), notifier)));

      notifier.Completed.ShouldBeTrue();
      aggregate.Message.ShouldContain("could not publish the unhandled-exception node");
      aggregate.InnerExceptions.Count.ShouldBe(2);
      aggregate.InnerExceptions[0].ShouldBeOfType<NotSupportedException>();
      aggregate.InnerExceptions[1].ShouldBeSameAs(publishFailure);
    }

    public static async Task Extensions_Should_UseDistinctUidsAndInformationalVersion()
    {
      JaribuExtension framework = new();
      JaribuCommandLineOptionsProvider commandLine = new();
      framework.Uid.ShouldBe("TimeWarp.Jaribu");
      commandLine.Uid.ShouldBe("TimeWarp.Jaribu.CommandLine");
      framework.Version.ShouldBe(commandLine.Version);

      string? informational = typeof(JaribuExtension).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;
      string expected = informational ?? string.Empty;
      int plus = expected.IndexOf('+', StringComparison.Ordinal);
      if (plus > 0)
      {
        expected = expected[..plus];
      }

      framework.Version.ShouldBe(expected);
      await Task.CompletedTask;
    }

    public static Task PackedTestingPlatform_Should_StillGrantTestInternalsUntilTheReleasePack()
    {
      string granted = RepoFile(
        "source",
        "timewarp-jaribu-testing-platform",
        "bin",
        "Debug",
        "net10.0",
        "timewarp-jaribu-testing-platform.dll");
      File.Exists(granted).ShouldBeTrue();
      IReadOnlyList<string> friends = ReleaseGate.ReadInternalsVisibleTo(granted);
      friends.ShouldContain("mtp-review-cleanups");
      friends.ShouldContain("run-ci-tests");
      friends.ShouldContain("timewarp-jaribu-tests-mtp");
      InvalidOperationException exposed = Should.Throw<InvalidOperationException>(
        () => ReleaseGate.RequireNoInternalsVisibleTo(granted));
      exposed.Message.ShouldContain("mtp-review-cleanups");

      string root = Directory.CreateTempSubdirectory("jaribu-ivt-granted-").FullName;
      try
      {
        string packagePath = Path.Combine(root, "TimeWarp.Jaribu.TestingPlatform.0.0.0.nupkg");
        WriteLibPackage(packagePath, "lib/net10.0/TimeWarp.Jaribu.TestingPlatform.dll", granted);
        InvalidOperationException packed = Should.Throw<InvalidOperationException>(
          () => ReleaseGate.RequirePackageGrantsNoInternals(packagePath));
        packed.Message.ShouldContain("mtp-review-cleanups");
      }
      finally
      {
        Directory.Delete(root, recursive: true);
      }

      return Task.CompletedTask;
    }

    private static async Task AssertMissingAsync(TestNodeState state)
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        $"Probe.{state}",
        state.ToString(),
        state,
        Message: "detail"));

      Exception exception = ExceptionOf(bus.Messages.Single().TestNode, state);
      exception.ShouldBeOfType<InvalidOperationException>();
      exception.Message.ShouldContain("supplied no exception");
      exception.Message.ShouldContain("detail");
      exception.ShouldNotBeOfType<TimeoutException>();
    }

    private static Exception ExceptionOf(TestNode node, TestNodeState state)
    {
      TestNodeStateProperty property = node.Properties.Single<TestNodeStateProperty>();
      return state switch
      {
        TestNodeState.Failed => property.ShouldBeOfType<FailedTestNodeStateProperty>().Exception!,
        TestNodeState.Timeout => property.ShouldBeOfType<TimeoutTestNodeStateProperty>().Exception!,
        TestNodeState.Error => property.ShouldBeOfType<ErrorTestNodeStateProperty>().Exception!,
        _ => throw new InvalidOperationException($"Unexpected state {state}.")
      };
    }

#pragma warning disable TPEXP // ExecuteRequestContext is the MTP host boundary this test drives
    private static string RepoFile(params string[] parts)
    {
      DirectoryInfo? dir = new(Directory.GetCurrentDirectory());
      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir.FullName, "timewarp-jaribu.slnx")))
        {
          return Path.Combine([dir.FullName, .. parts]);
        }

        dir = dir.Parent;
      }

      throw new InvalidOperationException("timewarp-jaribu.slnx was not found from the working directory.");
    }

    private static ExecuteRequestContext UnsupportedContext(IMessageBus bus, CompletionNotifier notifier)
    {
      ConstructorInfo ctor = typeof(TestSessionContext).GetConstructor(
        BindingFlags.Instance | BindingFlags.NonPublic,
        binder: null,
        [typeof(SessionUid)],
        modifiers: null)
        ?? throw new InvalidOperationException("TestSessionContext constructor was not found.");
      TestSessionContext session = (TestSessionContext)ctor.Invoke([new SessionUid("unhandled")]);
      return new ExecuteRequestContext(new UnsupportedRequest(session), bus, notifier, CancellationToken.None);
    }
#pragma warning restore TPEXP

    private static void WriteLibPackage(string packagePath, string entryName, string assemblyPath)
    {
      using FileStream stream = File.Create(packagePath);
      using ZipArchive zip = new(stream, ZipArchiveMode.Create);
      ZipArchiveEntry entry = zip.CreateEntry(entryName);
      using Stream entryStream = entry.Open();
      using FileStream assembly = File.OpenRead(assemblyPath);
      assembly.CopyTo(entryStream);
    }

    private sealed class UnsupportedRequest(TestSessionContext session) : IRequest
    {
      public TestSessionContext Session { get; } = session;
    }

#pragma warning disable TPEXP // The completion notifier is the MTP host signal ExecuteRequestAsync calls
    private sealed class CompletionNotifier : IExecuteRequestCompletionNotifier
    {
      public bool Completed { get; private set; }

      public void Complete() => Completed = true;
    }
#pragma warning restore TPEXP

    private sealed class UnusedServices : IServiceProvider
    {
      public object? GetService(Type serviceType) => null;
    }

    private sealed class ThrowingBus(Exception publishFailure) : IMessageBus
    {
      public Task PublishAsync(IDataProducer dataProducer, IData data) => throw publishFailure;
    }

    private sealed class RecordingBus : IMessageBus
    {
      public List<TestNodeUpdateMessage> Messages { get; } = [];

      public Task PublishAsync(IDataProducer dataProducer, IData data)
      {
        if (data is TestNodeUpdateMessage message)
        {
          Messages.Add(message);
        }

        return Task.CompletedTask;
      }
    }

    private sealed class FakeProducer : IDataProducer
    {
      public string Uid => "mtp-review-cleanups";

      public string Version => "1";

      public string DisplayName => "mtp-review-cleanups";

      public string Description => "records MTP messages";

      public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

      public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
  }
}
