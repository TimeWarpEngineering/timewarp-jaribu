#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:project $(SourceDirectory)timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj

#region Purpose
// MTP checks for input-row identity, filter precedence, in-progress publication,
// cleanup text on Failed, Timeout, and Error explanations, and --treenode-filter.
#endregion

#region Design
// MtpSink is exercised in-process through InternalsVisibleTo for run-ci-tests,
// run-tests, mtp-node-identity, and the multi-file project timewarp-jaribu-tests-mtp.
// Filter and list/run parity go through dotnet test on the validation assembly,
// because those decisions live behind the MTP host. The validation project is
// allowed to fail; assertions read its report.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Mtp_
{
  using System.Reflection;
  using System.Text.RegularExpressions;
  using Microsoft.Testing.Platform.Extensions;
  using Microsoft.Testing.Platform.Extensions.Messages;
  using Microsoft.Testing.Platform.Messages;
  using Microsoft.Testing.Platform.TestHost;
  using TimeWarp.Amuru;
  using TimeWarp.Jaribu.TestingPlatform;

  [TestTag("Jaribu")]
  public partial class NodeIdentity_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NodeIdentity_Given_>();

    public static async Task StartedFailure_Should_PublishInProgressAndParameters()
    {
      MethodInfo method = typeof(Probe).GetMethod(nameof(Probe.Sample))!;
      method.ShouldNotBeNull();

      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      TestNodeInfo failed = new(
        "Probe.Sample[0]",
        "Sample(fail)",
        TestNodeState.Failed,
        Message: "boom",
        Parameters: new object?[] { "fail" },
        Method: method);

      await sink.OnTestStartedAsync(failed);
      await sink.OnTestCompletedAsync(failed);

      bus.Messages.Count.ShouldBe(2);

      TestNode started = bus.Messages[0].TestNode;
      started.Uid.Value.ShouldBe("Probe.Sample[0]");
      started.Properties.Single<TestNodeStateProperty>().ShouldBeOfType<InProgressTestNodeStateProperty>();
      TestMethodIdentifierProperty startedId = started.Properties.Single<TestMethodIdentifierProperty>();
      startedId.MethodName.ShouldBe(nameof(Probe.Sample));
      startedId.ParameterTypeFullNames.ShouldBe(["System.String"]);
      startedId.Namespace.ShouldBe("Mtp_");
      startedId.TypeName.ShouldBe("NodeIdentity_Given_+Probe");
      TestMetadataProperty startedParameter = started.Properties.OfType<TestMetadataProperty>().Single();
      startedParameter.Key.ShouldBe("jaribu.parameter.0");
      startedParameter.Value.ShouldBe("fail");

      TestNode completed = bus.Messages[1].TestNode;
      completed.Properties.Single<TestNodeStateProperty>().ShouldBeOfType<FailedTestNodeStateProperty>();
      completed.Properties.OfType<TestMetadataProperty>().Single().Value.ShouldBe("fail");

      TestFileLocationProperty? location = started.Properties.SingleOrDefault<TestFileLocationProperty>();
      // IL3000: single-file hosts report an empty Location, and the check then treats the PDB as absent.
#pragma warning disable IL3000
      string? assemblyPath = method.Module.Assembly.Location;
#pragma warning restore IL3000
      bool pdbExists = !string.IsNullOrEmpty(assemblyPath) &&
        File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"));
      if (pdbExists)
      {
        TestFileLocationProperty present = location.ShouldNotBeNull();
        present.FilePath.ShouldContain("mtp-node-identity.cs");
      }
    }

    public static async Task CleanupMessage_Should_ReachFailedTimeoutAndErrorExplanations()
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      ArgumentException body = new("body boom");
      const string FailedMessage = "body boom; CleanUp InvalidOperationException: cleanup boom";
      const string TimeoutMessage = "Timeout after 1ms; CleanUp InvalidOperationException: cleanup boom";
      const string ErrorMessage =
        "InvalidOperationException: body boom; CleanUp InvalidOperationException: cleanup boom";

      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Failed",
        "Failed",
        TestNodeState.Failed,
        Exception: body,
        Message: FailedMessage));
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Timeout",
        "Timeout",
        TestNodeState.Timeout,
        Exception: new TimeoutException("timed out"),
        Message: TimeoutMessage));
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Error",
        "Error",
        TestNodeState.Error,
        Exception: new InvalidOperationException("body boom"),
        Message: ErrorMessage));

      bus.Messages.Count.ShouldBe(3);

      FailedTestNodeStateProperty failed = bus.Messages[0].TestNode.Properties
        .Single<TestNodeStateProperty>()
        .ShouldBeOfType<FailedTestNodeStateProperty>();
      failed.Exception.ShouldBeSameAs(body);
      failed.Explanation.ShouldBe(FailedMessage);

      TimeoutTestNodeStateProperty timeout = bus.Messages[1].TestNode.Properties
        .Single<TestNodeStateProperty>()
        .ShouldBeOfType<TimeoutTestNodeStateProperty>();
      timeout.Explanation.ShouldBe(TimeoutMessage);
      timeout.Exception.ShouldBeOfType<TimeoutException>();

      ErrorTestNodeStateProperty error = bus.Messages[2].TestNode.Properties
        .Single<TestNodeStateProperty>()
        .ShouldBeOfType<ErrorTestNodeStateProperty>();
      error.Explanation.ShouldBe(ErrorMessage);
      Exception errorException = error.Exception.ShouldNotBeNull();
      errorException.Message.ShouldBe("body boom");
    }

    public static async Task Cancelled_Should_MapToCancelledStateProperty()
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      OperationCanceledException cancelled = new("stopped");
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Cancelled",
        "Cancelled",
        TestNodeState.Cancelled,
        Exception: cancelled,
        Message: "stopped"));

      bus.Messages.Count.ShouldBe(1);
#pragma warning disable MTP0001 // MTP 2.3.3 still exposes this property. The sink maps Cancelled to it.
      CancelledTestNodeStateProperty state = bus.Messages[0].TestNode.Properties
        .Single<TestNodeStateProperty>()
        .ShouldBeOfType<CancelledTestNodeStateProperty>();
#pragma warning restore MTP0001
      state.Explanation.ShouldBe("stopped");
      state.Exception.ShouldBeSameAs(cancelled);
    }

    public static async Task FilterClass_Should_OmitOtherClasses()
    {
      CommandOutput basic = await RunValidationAsync("--list-tests", "--", "--filter-class", "Basic");
      string basicText = Normalize(basic.Combined);
      basic.ExitCode.ShouldBe(0, basicText);
      basicText.ShouldContain("PassingTest");
      basicText.ShouldNotContain("NullReferenceExceptionTest");
      basicText.ShouldNotContain("SetupWasCalledTest");

      CommandOutput none = await RunValidationAsync("--list-tests", "--", "--filter-class", "NoSuchClass036001");
      string noneText = Normalize(none.Combined);
      // MTP exit 8 means discovery finished and found nothing.
      none.ExitCode.ShouldBe(8, noneText);
      DiscoveredCount(noneText).ShouldBe(0);
      noneText.ShouldNotContain("PassingTest");
    }

    public static async Task FilterTagCli_Should_WinOverEnvironment()
    {
      CommandOutput environmentOnly = await RunValidationAsync(
        ["--list-tests"],
        ("JARIBU_FILTER_TAG", "EdgeCases"));
      string environmentText = Normalize(environmentOnly.Combined);
      environmentOnly.ExitCode.ShouldBe(0, environmentText);
      environmentText.ShouldContain("NullReferenceExceptionTest");
      environmentText.ShouldNotContain("PassingTest");

      CommandOutput cliWins = await RunValidationAsync(
        ["--list-tests", "--", "--filter-tag", "MtpValidation"],
        ("JARIBU_FILTER_TAG", "EdgeCases"));
      string cliText = Normalize(cliWins.Combined);
      cliWins.ExitCode.ShouldBe(0, cliText);
      cliText.ShouldContain("PassingTest");
      cliText.ShouldNotContain("NullReferenceExceptionTest");
    }

    public static async Task InputRows_Should_ListAndRunTheSameNodes()
    {
      CommandOutput listed = await RunValidationAsync("--list-tests", "--", "--filter-class", "InputIdentity");
      string listedText = Normalize(listed.Combined);
      listed.ExitCode.ShouldBe(0, listedText);
      DiscoveredCount(listedText).ShouldBe(2);
      listedText.ShouldContain("FailThenPass(fail)");
      listedText.ShouldContain("FailThenPass(pass)");

      CommandOutput ran = await RunValidationAsync("--", "--filter-class", "InputIdentity");
      string ranText = Normalize(ran.Combined);
      ranText.ShouldContain("failed FailThenPass(fail)");
      SummaryCount(ranText, "total").ShouldBe(2);
      SummaryCount(ranText, "failed").ShouldBe(1);
      SummaryCount(ranText, "succeeded").ShouldBe(1);
    }

    public static async Task TreeNodeFilter_Should_SelectTheRequestedSet()
    {
      const string PassingPath =
        "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest";
      const string InputPath =
        "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/InputIdentityTests/FailThenPass";

      CommandOutput oneMethod = await RunValidationAsync("--list-tests", "--", "--treenode-filter", PassingPath);
      string oneMethodText = Normalize(oneMethod.Combined);
      oneMethod.ExitCode.ShouldBe(0, oneMethodText);
      DiscoveredCount(oneMethodText).ShouldBe(1);
      oneMethodText.ShouldContain("PassingTest");
      oneMethodText.ShouldNotContain("AssertionPassingTest");
      oneMethodText.ShouldNotContain("NullReferenceExceptionTest");
      oneMethodText.ShouldNotContain("FailThenPass");

      CommandOutput bothRows = await RunValidationAsync("--list-tests", "--", "--treenode-filter", InputPath);
      string bothRowsText = Normalize(bothRows.Combined);
      bothRows.ExitCode.ShouldBe(0, bothRowsText);
      DiscoveredCount(bothRowsText).ShouldBe(2);
      bothRowsText.ShouldContain("FailThenPass(fail)");
      bothRowsText.ShouldContain("FailThenPass(pass)");
      bothRowsText.ShouldNotContain("PassingTest");

      CommandOutput tagged = await RunValidationAsync(
        "--list-tests",
        "--",
        "--treenode-filter",
        "/**[TestTag=EdgeCases]");
      string taggedText = Normalize(tagged.Combined);
      tagged.ExitCode.ShouldBe(0, taggedText);
      DiscoveredCount(taggedText).ShouldBe(5);
      taggedText.ShouldContain("NullReferenceExceptionTest");
      taggedText.ShouldContain("ArgumentExceptionTest");
      taggedText.ShouldNotContain("PassingTest");
      taggedText.ShouldNotContain("FailThenPass");

      CommandOutput oneRow = await RunValidationAsync(
        "--list-tests",
        "--",
        "--treenode-filter",
        "/**[jaribu.parameter.0=fail]");
      string oneRowText = Normalize(oneRow.Combined);
      oneRow.ExitCode.ShouldBe(0, oneRowText);
      DiscoveredCount(oneRowText).ShouldBe(1);
      oneRowText.ShouldContain("FailThenPass(fail)");
      oneRowText.ShouldNotContain("FailThenPass(pass)");

      CommandOutput none = await RunValidationAsync(
        "--list-tests",
        "--",
        "--treenode-filter",
        "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/NoSuchMethod");
      string noneText = Normalize(none.Combined);
      none.ExitCode.ShouldBe(8, noneText);
      DiscoveredCount(noneText).ShouldBe(0);

      CommandOutput ran = await RunValidationAsync("--", "--treenode-filter", PassingPath);
      string ranText = Normalize(ran.Combined);
      ran.ExitCode.ShouldBe(0, ranText);
      ranText.ShouldNotContain("AssertionPassingTest");
      ranText.ShouldNotContain("FailThenPass");
      SummaryCount(ranText, "total").ShouldBe(1);
      SummaryCount(ranText, "succeeded").ShouldBe(1);
      SummaryCount(ranText, "failed").ShouldBe(0);
    }

    public static async Task SignatureWarnings_Should_ShowAndHonorMtpFlags()
    {
      CommandOutput warned = await RunValidationAsync("--", "--filter-class", "SignatureWarning");
      string warnedText = Normalize(warned.Combined);
      warned.ExitCode.ShouldBe(0, warnedText);
      warnedText.ShouldContain("Near-miss");
      warnedText.ShouldContain("ReturnsTaskOfInt");
      // One Near-miss sentence names ReturnsTaskOfInt. A forwarding host prints the
      // warning, and a host that drops it prints the skipped message. Printing both
      // means HostDropsOutputDeviceWarnings misread ProxyOutputDevice.OriginalOutputDevice,
      // DotnetTestPassthroughOutputDevice, or IPushOnlyProtocol.IsDisplayMessageForwardingSupported.
      int nearMissSentences = Regex.Count
      (
        warnedText,
        "Near-miss test '[^']*ReturnsTaskOfInt'"
      );
      nearMissSentences.ShouldBe(1, warnedText);
      SummaryCount(warnedText, "skipped").ShouldBeLessThanOrEqualTo(1);
      SummaryCount(warnedText, "succeeded").ShouldBe(1);
      SummaryCount(warnedText, "failed").ShouldBe(0);

      CommandOutput strict = await RunValidationAsync(
        "--",
        "--filter-class",
        "SignatureWarning",
        "--strict-signatures");
      string strictText = Normalize(strict.Combined);
      strict.ExitCode.ShouldNotBe(0, strictText);
      strictText.ShouldContain("ReturnsTaskOfInt");
      SummaryCount(strictText, "failed").ShouldBeGreaterThan(0);
      SummaryCount(strictText, "skipped").ShouldBe(0);

      CommandOutput empty = await RunValidationAsync("--", "--filter-class", "AllowEmptyProbe");
      string emptyText = Normalize(empty.Combined);
      empty.ExitCode.ShouldNotBe(0, emptyText);
      emptyText.ShouldContain("No tests executed");

      CommandOutput allowed = await RunValidationAsync(
        "--",
        "--filter-class",
        "AllowEmptyProbe",
        "--allow-empty");
      string allowedText = Normalize(allowed.Combined);
      allowedText.ShouldNotContain("No tests executed");
      allowed.ExitCode.ShouldNotBe(2, allowedText);

      const string PassingPath =
        "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest";
      CommandOutput selected = await RunValidationAsync("--", "--treenode-filter", PassingPath);
      string selectedText = Normalize(selected.Combined);
      selected.ExitCode.ShouldBe(0, selectedText);
      selectedText.ShouldNotContain("No tests executed");
      selectedText.ShouldNotContain("AllowEmptyProbe");
      selectedText.ShouldNotContain("ReturnsTaskOfInt");
      SummaryCount(selectedText, "total").ShouldBe(1);

      CommandOutput strictSelected = await RunValidationAsync(
        "--",
        "--treenode-filter",
        PassingPath,
        "--strict-signatures");
      string strictSelectedText = Normalize(strictSelected.Combined);
      strictSelected.ExitCode.ShouldBe(0, strictSelectedText);
      strictSelectedText.ShouldNotContain("ReturnsTaskOfInt");
      strictSelectedText.ShouldNotContain("No tests executed");
      SummaryCount(strictSelectedText, "total").ShouldBe(1);
      SummaryCount(strictSelectedText, "failed").ShouldBe(0);
      SummaryCount(strictSelectedText, "succeeded").ShouldBe(1);
    }

    private static string Normalize(string output)
    {
      string withoutCarriageReturns = output.Replace("\r", "", StringComparison.Ordinal);
      return AnsiEscape().Replace(withoutCarriageReturns, "");
    }

    [GeneratedRegex(@"\x1b\[[0-9;]*m", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiEscape();

    private static int DiscoveredCount(string output)
    {
      Match match = Regex.Match(output, @"^Discovered (\d+) tests\.$", RegexOptions.Multiline);
      match.Success.ShouldBeTrue(output);
      return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static int SummaryCount(string output, string label)
    {
      Match match = Regex.Match(output, $@"^\s*{label}: (\d+)\s*$", RegexOptions.Multiline);
      match.Success.ShouldBeTrue(output);
      return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static Task<CommandOutput> RunValidationAsync(params string[] arguments)
    {
      return RunValidationAsync(arguments, environment: null);
    }

    private static Task<CommandOutput> RunValidationAsync(
      string[] arguments,
      (string Name, string Value)? environment)
    {
      string root = FindRepoRoot();
      string project = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu-mtp-validation",
        "timewarp-jaribu-mtp-validation.csproj");

      List<string> allArguments = ["test", project, "--nologo", .. arguments];
      ShellBuilder builder = Shell.Builder("dotnet")
        .WithArguments([.. allArguments])
        .WithWorkingDirectory(root)
        .WithNoValidation();

      if (environment is (string name, string value))
      {
        builder = builder.WithEnvironmentVariable(name, value);
      }

      return builder.RunAndCaptureAsync();
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

    private sealed class Probe
    {
      public static async Task Sample(string value)
      {
        _ = value;
        await Task.CompletedTask;
      }
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
      public string Uid => "mtp-node-identity";

      public string Version => "1";

      public string DisplayName => "mtp-node-identity";

      public string Description => "records MTP messages";

      public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

      public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
  }
}
