#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:project $(SourceDirectory)timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj
#:project $(TestsDirectory)timewarp-jaribu/tautology-fixture/tautology-fixture.csproj
#:property IsTestingPlatformApplication=false
#:property GenerateTestingPlatformEntryPoint=false
#:property GenerateTestingPlatformConfigurationFile=false
#:property GenerateSelfRegisteredExtensions=false

#region Purpose
// Proves coverage, prove mode, strict failures, known tautologies, and the analyzer pre-filter.
#endregion

#region Design
// The fixture assembly is woven. Add calls Scale, so the implied claim is Scale,
// the last distinct production method entered.
// A test that catches the break and then asserts a constant still passes, so the
// claim is unproven while coverage is non-zero.
// The analyzer fixture builds with warnings as errors off. The build log is the assertion.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Tautology_
{
  using System.Reflection;
  using Microsoft.Testing.Platform.CommandLine;
  using Microsoft.Testing.Platform.Extensions.CommandLine;
  using Microsoft.Testing.Platform.Extensions;
  using Microsoft.Testing.Platform.Extensions.Messages;
  using Microsoft.Testing.Platform.Messages;
  using Microsoft.Testing.Platform.TestHost;
  using TautologyFixture;
  using TimeWarp.Amuru;
  using TimeWarp.Jaribu.TestingPlatform;

  [TestTag("Core")]
  public class Detector_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Detector_Given_>();

    public static async Task Add_Should_ProveTheExplicitClaim()
    {
      TautologyCollectingSink sink = new();
      TestRunStats stats = await RunTestsAsync<Explicit_Given_>(
        sink,
        tautology: new TautologyOptions(CollectCoverage: true, Prove: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Violation.ShouldBeFalse();
      finding.Proven.ShouldBe(1);
      finding.Covered.ShouldBeGreaterThan(0);
      finding.Rule.ShouldContain(nameof(TautologySubject.Add));
      sink.Results[0].State.ShouldBe(TestNodeState.Passed);
      await Task.CompletedTask;
    }

    public static async Task Add_Should_ImplyTheLastProductionMethod()
    {
      TestRunStats stats = await RunTestsAsync<Inferred_Given_>(
        new TautologyCollectingSink(),
        tautology: new TautologyOptions(CollectCoverage: true, Prove: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Violation.ShouldBeFalse();
      finding.Rule.ShouldContain(nameof(TautologySubject.Scale));
      await Task.CompletedTask;
    }

    public static async Task Add_Should_StayUnprovenWhenTheResultIsDiscarded()
    {
      TestRunStats stats = await RunTestsAsync<Discarded_Given_>(
        new TautologyCollectingSink(),
        tautology: new TautologyOptions(CollectCoverage: true, Prove: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Covered.ShouldBeGreaterThan(0);
      finding.Proven.ShouldBe(0);
      finding.Violation.ShouldBeTrue();
      finding.Reason.ShouldContain("Unproven");
      await Task.CompletedTask;
    }

    public static async Task Constant_Should_BeZeroCoverage()
    {
      _ = TautologySubject.Scale(0);
      TestRunStats stats = await RunTestsAsync<Constant_Given_>(
        new TautologyCollectingSink(),
        tautology: new TautologyOptions(CollectCoverage: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Covered.ShouldBe(0);
      finding.Rule.ShouldBe("coverage");
      finding.Violation.ShouldBeTrue();
      finding.Location.ShouldContain(":");
      await Task.CompletedTask;
    }

    public static async Task StrictZeroCoverage_Should_FailTheRun()
    {
      _ = TautologySubject.Scale(0);
      TautologyCollectingSink sink = new();
      TestRunStats stats = await RunTestsAsync<Constant_Given_>(
        sink,
        tautology: new TautologyOptions(CollectCoverage: true, Strict: true));

      stats.Success.ShouldBeFalse();
      sink.Results[0].State.ShouldBe(TestNodeState.Failed);
      sink.Results[0].Exception.ShouldBeOfType<TautologyException>();
      await Task.CompletedTask;
    }

    public static async Task KnownTautology_Should_StayPassingUnderStrict()
    {
      TestRunStats stats = await RunTestsAsync<Known_Given_>(
        new TautologyCollectingSink(),
        tautology: new TautologyOptions(CollectCoverage: true, Prove: true, Strict: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Known.ShouldBeTrue();
      finding.Violation.ShouldBeFalse();
      finding.Reason.ShouldBe("compile-only smoke");
      stats.Success.ShouldBeTrue();
      await Task.CompletedTask;
    }

    public static async Task MissingClaim_Should_ViolateWhenRequired()
    {
      _ = TautologySubject.Scale(0);
      TestRunStats stats = await RunTestsAsync<Inferred_Given_>(
        new TautologyCollectingSink(),
        tautology: new TautologyOptions(RequireClaims: true));

      TautologyFinding finding = stats.TautologyFindings.ShouldHaveSingleItem();
      finding.Violation.ShouldBeTrue();
      finding.Reason.ShouldContain("Missing [Proves]");
      await Task.CompletedTask;
    }

    public static async Task Report_Should_RankTheUnprovenTestFirst()
    {
      using TestTerminal terminal = new();
      int exitCode = await RunTests<Rank_Given_>(
        terminal: terminal,
        tautology: new TautologyOptions(CollectCoverage: true, Prove: true));

      exitCode.ShouldBe(0);
      string text = terminal.Output;
      int report = text.IndexOf("Tautology report (worst first)", StringComparison.Ordinal);
      report.ShouldBeGreaterThanOrEqualTo(0);
      string ranked = text[report..];
      int unproven = ranked.IndexOf("Discard_Should", StringComparison.Ordinal);
      int proven = ranked.IndexOf("Keep_Should", StringComparison.Ordinal);
      unproven.ShouldBeGreaterThanOrEqualTo(0);
      proven.ShouldBeGreaterThan(unproven);
      text.ShouldContain("kills/s=");
      await Task.CompletedTask;
    }

    public static async Task Rank_Should_PutViolationsAheadOfSlowerProofs()
    {
      TautologyFinding slowProof = new(
        "proof", "claim:Slow", "Proved 1 claim(s).", "file:1", 1, 1, 5, 0.2, false, false);
      TautologyFinding unproven = new(
        "gap", "claim:Gap", "Unproven: Gap.", "file:2", 1, 0, 0.1, 0, true, false);

      IReadOnlyList<TautologyFinding> ranked = TautologyReport.Rank([slowProof, unproven]);
      ranked[0].TestId.ShouldBe("gap");
      ranked[1].TestId.ShouldBe("proof");
      await Task.CompletedTask;
    }

    public static async Task Mtp_Should_PublishTheScore()
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      TautologyFinding finding = new(
        "Tautology_.Mtp.Score",
        "claim:TautologyFixture.TautologySubject.Add",
        "Proved 1 claim(s).",
        "file:10",
        2,
        1,
        0.5,
        2,
        false,
        false);
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        finding.TestId,
        "Score",
        TestNodeState.Passed,
        TimeSpan.FromMilliseconds(10),
        Tautology: finding));

      TestNodeUpdateMessage message = bus.Messages.ShouldHaveSingleItem();
      string? rule = Metadata(message, "jaribu.tautology.rule");
      rule.ShouldBe(finding.Rule);
      Metadata(message, "jaribu.tautology.covered").ShouldBe("2");
      Metadata(message, "jaribu.tautology.proven").ShouldBe("1");
      Metadata(message, "jaribu.tautology.location").ShouldBe("file:10");
      await Task.CompletedTask;
    }

    public static async Task Options_Should_ExposeProveAndStrict()
    {
      JaribuCommandLineOptionsProvider provider = new();
      IReadOnlyCollection<CommandLineOption> options = provider.GetCommandLineOptions();
      options.Select(option => option.Name).ShouldContain(JaribuCommandLineOptionsProvider.ProveOption);
      options.Select(option => option.Name).ShouldContain(JaribuCommandLineOptionsProvider.StrictTautologyOption);
      options.Select(option => option.Name).ShouldContain(JaribuCommandLineOptionsProvider.PerTestCoverageOption);
      options.Select(option => option.Name).ShouldContain(JaribuCommandLineOptionsProvider.RequireClaimsOption);
      await Task.CompletedTask;
    }

    public static async Task Analyzer_Should_FlagOnlyThePositiveRules()
    {
      string root = FindRepoRoot();
      string project = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu",
        "tautology-analyzer-fixture",
        "tautology-analyzer-fixture.csproj");
      CommandOutput output = await Shell.Builder("dotnet")
        .WithArguments("build", project, "-t:Rebuild", "--nologo")
        .WithWorkingDirectory(root)
        .WithNoValidation()
        .CaptureAsync();

      string text = output.Combined;
      text.ShouldContain("JARIBU1001");
      text.ShouldContain("JARIBU1002");
      text.ShouldContain("JARIBU1003");
      text.ShouldContain("JARIBU1004");
      text.ShouldNotContain("Negative_.Compute_Should_NotFlag");
      text.ShouldNotContain("Negative_.DifferentSymbols_Should_NotFlag");
      text.ShouldNotContain("Negative_.Known_Should_NotFlag");
      await Task.CompletedTask;
    }

    private static string? Metadata(TestNodeUpdateMessage message, string key)
    {
      foreach (IProperty property in message.TestNode.Properties)
      {
        if (property is TestMetadataProperty metadata && metadata.Key == key)
        {
          return metadata.Value;
        }
      }

      return null;
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
      public string Uid => "test-runner.tautology";

      public string Version => "1";

      public string DisplayName => "test-runner.tautology";

      public string Description => "records MTP messages";

      public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

      public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
  }

  internal sealed class TautologyCollectingSink : ITestResultSink
  {
    public List<TestNodeInfo> Results { get; } = [];

    public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestCompletedAsync(TestNodeInfo node)
    {
      Results.Add(node);
      return Task.CompletedTask;
    }

    public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;

    public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
  }

  public class Explicit_Given_
  {
    [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
    public static async Task Add_Should_ReturnTheSum()
    {
      TautologySubject.Add(2, 3).ShouldBe(5);
      await Task.CompletedTask;
    }
  }

  public class Inferred_Given_
  {
    public static async Task Add_Should_ImplyScale()
    {
      TautologySubject.Add(2, 3).ShouldBe(5);
      await Task.CompletedTask;
    }
  }

  public class Discarded_Given_
  {
    [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
    public static async Task Add_Should_DiscardTheResult()
    {
      try
      {
        _ = TautologySubject.Add(2, 3);
      }
      catch (ProbeBrokenException broken)
      {
        _ = broken.Token;
      }

      true.ShouldBeTrue();
      await Task.CompletedTask;
    }
  }

  public class Constant_Given_
  {
    public static async Task Value_Should_EqualTheSameLiteral()
    {
      1.ShouldBe(1);
      await Task.CompletedTask;
    }
  }

  public class Known_Given_
  {
    [KnownTautology("compile-only smoke")]
    public static async Task Smoke_Should_StayListed()
    {
      await Task.CompletedTask;
    }
  }

  public class Rank_Given_
  {
    [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
    public static async Task Keep_Should_ReturnTheSum()
    {
      TautologySubject.Add(2, 3).ShouldBe(5);
      await Task.CompletedTask;
    }

    [Proves(typeof(TautologySubject), nameof(TautologySubject.Add))]
    public static async Task Discard_Should_IgnoreTheThrow()
    {
      try
      {
        _ = TautologySubject.Add(2, 3);
      }
      catch (ProbeBrokenException broken)
      {
        _ = broken.Token;
      }

      true.ShouldBeTrue();
      await Task.CompletedTask;
    }
  }
}
