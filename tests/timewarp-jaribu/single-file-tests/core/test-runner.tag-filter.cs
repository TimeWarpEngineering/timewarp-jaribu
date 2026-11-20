#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.tag-filter.fixtures.cs

#region Purpose
// Proves multi-tag include, exclude, and the skip text on the runfile path.
#endregion

#region Design
// Fixtures stay unregistered. Explicit arguments cover include-any, exclude-any,
// include then exclude, class tags, a comma that stays one tag, and a nested run
// that does not read the environment. The child probe is the depth-0 environment
// comma list, including a trimmed value. Discovery uids match the run.
// Fixtures live in the included sibling.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Text.RegularExpressions;
  using TimeWarp.Amuru;

  /// <summary>Include and exclude tag selection.</summary>
  [TestTag("Jaribu")]
  public class TagFilter_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<TagFilter_Given_>();

    /// <summary>Any include tag runs that method. Untagged methods stay.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task IncludeAny_Should_RunAMethodThatCarriesOneTag()
    {
      TagFilterIncludeFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterIncludeFixture>(
        filterTags: ["ci", "smoke"]);

      stats.PassedCount.ShouldBe(3);
      stats.SkippedCount.ShouldBe(1);
      sink.FilterLabel.ShouldBe("ci, smoke");
      TagFilterIncludeFixture.CiRuns.ShouldBe(1);
      TagFilterIncludeFixture.SmokeRuns.ShouldBe(1);
      TagFilterIncludeFixture.PlainRuns.ShouldBe(1);
      TagFilterIncludeFixture.UnitRuns.ShouldBe(0);
      Message(sink, nameof(TagFilterIncludeFixture.Unit)).ShouldBe("No matching tag 'ci', 'smoke'");
      Discovered<TagFilterIncludeFixture>(filterTags: ["ci", "smoke"]).ShouldBe(CompletedUids(sink));
      await Task.CompletedTask;
    }

    /// <summary>The single-string parameter is one tag, even when it contains a comma.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task FilterTag_Should_KeepACommaInsideOneTag()
    {
      TagFilterIncludeFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterIncludeFixture>(
        filterTag: "ci,smoke");

      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(3);
      sink.FilterLabel.ShouldBe("ci,smoke");
      TagFilterIncludeFixture.PlainRuns.ShouldBe(1);
      TagFilterIncludeFixture.CiRuns.ShouldBe(0);
      sink.Results.Where(node => node.State == TestNodeState.Skipped)
        .ShouldAllBe(node => node.Message == "No matching tag 'ci,smoke'");
      await Task.CompletedTask;
    }

    /// <summary>Any exclude tag skips that method and names the filter's spelling.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task ExcludeAny_Should_SkipAMethodThatCarriesOneTag()
    {
      TagFilterExcludeFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterExcludeFixture>(
        excludeTags: ["Slow", "flaky"]);

      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(2);
      sink.FilterLabel.ShouldBeNull();
      TagFilterExcludeFixture.PlainRuns.ShouldBe(1);
      TagFilterExcludeFixture.SlowRuns.ShouldBe(0);
      TagFilterExcludeFixture.FlakyRuns.ShouldBe(0);
      Message(sink, nameof(TagFilterExcludeFixture.Slow)).ShouldBe("excluded by tag \"Slow\"");
      Message(sink, nameof(TagFilterExcludeFixture.Flaky)).ShouldBe("excluded by tag \"flaky\"");
      Discovered<TagFilterExcludeFixture>(excludeTags: ["Slow", "flaky"]).ShouldBe(CompletedUids(sink));
      await Task.CompletedTask;
    }

    /// <summary>Include is applied first. A method that passes include can still be excluded.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task IncludeThenExclude_Should_SkipTheMethodThatCarriesBoth()
    {
      TagFilterBothFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterBothFixture>(
        filterTags: ["smoke"],
        excludeTags: ["slow"]);

      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(2);
      TagFilterBothFixture.SmokeRuns.ShouldBe(1);
      TagFilterBothFixture.SmokeSlowRuns.ShouldBe(0);
      TagFilterBothFixture.SlowRuns.ShouldBe(0);
      Message(sink, nameof(TagFilterBothFixture.SmokeSlow)).ShouldBe("excluded by tag \"slow\"");
      Message(sink, nameof(TagFilterBothFixture.Slow)).ShouldBe("No matching tag 'smoke'");
      Discovered<TagFilterBothFixture>(filterTags: ["smoke"], excludeTags: ["slow"])
        .ShouldBe(CompletedUids(sink));
      await Task.CompletedTask;
    }

    /// <summary>A class exclude skips every method and does not run hooks.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task ClassExclude_Should_SkipEveryMethodWithoutHooks()
    {
      TagFilterClassExcludeFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterClassExcludeFixture>(
        excludeTag: "slow");

      stats.PassedCount.ShouldBe(0);
      stats.SkippedCount.ShouldBe(2);
      TagFilterClassExcludeFixture.BodyRuns.ShouldBe(0);
      TagFilterClassExcludeFixture.SetupOnceRuns.ShouldBe(0);
      sink.Results.ShouldAllBe(node => node.Message == "excluded by tag \"slow\"");
      sink.Results.ShouldAllBe(node => !node.Uid.Contains("[0]", StringComparison.Ordinal));
      Discovered<TagFilterClassExcludeFixture>(excludeTag: "slow").ShouldBe(CompletedUids(sink));
      await Task.CompletedTask;
    }

    /// <summary>A class include tag qualifies the class. The filter spelling is the skip text.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.RunTestsAsync))]
    public static async Task ClassInclude_Should_RunTheClassWhenAnyTagMatches()
    {
      TagFilterClassIncludeFixture.Reset();
      (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterClassIncludeFixture>(
        filterTags: ["CI", "nightly"]);

      stats.PassedCount.ShouldBe(1);
      stats.SkippedCount.ShouldBe(1);
      sink.FilterLabel.ShouldBe("CI, nightly");
      TagFilterClassIncludeFixture.PlainRuns.ShouldBe(1);
      TagFilterClassIncludeFixture.OtherRuns.ShouldBe(0);
      Message(sink, nameof(TagFilterClassIncludeFixture.Other)).ShouldBe("No matching tag 'CI', 'nightly'");
      await Task.CompletedTask;
    }

    /// <summary>A nested run does not read either tag variable and does not inherit an outer filter.</summary>
    [Proves(typeof(TestRunner), nameof(TestRunner.DiscoverTestCases))]
    public static async Task NestedRun_Should_IgnoreTagEnvironment()
    {
      string? previousInclude = Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");
      string? previousExclude = Environment.GetEnvironmentVariable("JARIBU_EXCLUDE_TAG");
      try
      {
        Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", "missing");
        Environment.SetEnvironmentVariable("JARIBU_EXCLUDE_TAG", "slow");
        TagFilterNestedFixture.Reset();
        (TestRunStats stats, TagFilterSink sink) = await RunAsync<TagFilterNestedFixture>();
        stats.PassedCount.ShouldBe(2);
        stats.SkippedCount.ShouldBe(0);
        TagFilterNestedFixture.SlowRuns.ShouldBe(1);
        Discovered<TagFilterNestedFixture>().ShouldBe(CompletedUids(sink));

        TagFilterNestedFixture.Reset();
        (TestRunStats excluded, TagFilterSink excludedSink) = await RunAsync<TagFilterNestedFixture>(
          excludeTag: "SLOW");
        excluded.PassedCount.ShouldBe(1);
        excluded.SkippedCount.ShouldBe(1);
        Message(excludedSink, nameof(TagFilterNestedFixture.Slow)).ShouldBe("excluded by tag \"SLOW\"");
      }
      finally
      {
        Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", previousInclude);
        Environment.SetEnvironmentVariable("JARIBU_EXCLUDE_TAG", previousExclude);
      }

      await Task.CompletedTask;
    }

    /// <summary>The outer process reads a comma list and trims each entry.</summary>
    [KnownTautology("child-process test")]
    public static async Task EnvironmentList_Should_TrimAndApplyIncludeThenExclude()
    {
      string trimmed = await LaunchProbeAsync(" Keep ", "");
      trimmed.ShouldContain("(filtered by tag: Keep)");
      trimmed.ShouldContain("Passed: 2");
      trimmed.ShouldContain("Skipped: 3");
      trimmed.ShouldContain("No matching tag 'Keep'");
      ShouldShowOutcome(trimmed, "Untagged", "PASSED");
      ShouldShowOutcome(trimmed, "Keep", "PASSED");

      string both = await LaunchProbeAsync("smoke,Keep", "slow");
      both.ShouldContain("(filtered by tag: smoke, Keep)");
      both.ShouldContain("Passed: 3");
      both.ShouldContain("Skipped: 2");
      both.ShouldContain("No matching tag 'smoke', 'Keep'");
      both.ShouldContain("excluded by tag \"slow\"");
      both.ShouldContain("Smoke Slow");

      string excludeOnly = await LaunchProbeAsync("", "SLOW");
      excludeOnly.ShouldNotContain("filtered by tag");
      excludeOnly.ShouldContain("Passed: 3");
      excludeOnly.ShouldContain("Skipped: 2");
      excludeOnly.ShouldContain("excluded by tag \"SLOW\"");
      excludeOnly.ShouldContain("Test: Slow");
      excludeOnly.ShouldContain("Smoke Slow");
      await Task.CompletedTask;
    }

    private static async Task<(TestRunStats Stats, TagFilterSink Sink)> RunAsync<T>(
      string? filterTag = null,
      IEnumerable<string>? filterTags = null,
      string? excludeTag = null,
      IEnumerable<string>? excludeTags = null) where T : class
    {
      TagFilterSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<T>(
        sink,
        filterTag: filterTag,
        filterTags: filterTags,
        excludeTag: excludeTag,
        excludeTags: excludeTags);
      return (stats, sink);
    }

    private static List<string> Discovered<T>(
      string? filterTag = null,
      IEnumerable<string>? filterTags = null,
      string? excludeTag = null,
      IEnumerable<string>? excludeTags = null)
    {
      return
      [
        .. TestRunner.DiscoverTestCases(
          typeof(T),
          filterTag,
          filterTags: filterTags,
          excludeTag: excludeTag,
          excludeTags: excludeTags).Select(testCase => testCase.Uid)
      ];
    }

    private static List<string> CompletedUids(TagFilterSink sink) => [.. sink.Results.Select(node => node.Uid)];

    private static void ShouldShowOutcome(string report, string testName, string marker)
    {
      string header = "Test: " + testName;
      int headerAt = report.IndexOf(header, StringComparison.Ordinal);
      headerAt.ShouldBeGreaterThanOrEqualTo(0, report);
      int lineEnd = report.IndexOf('\n', headerAt + header.Length);
      lineEnd.ShouldBeGreaterThanOrEqualTo(0, report);
      int nextEnd = report.IndexOf('\n', lineEnd + 1);
      if (nextEnd < 0)
      {
        nextEnd = report.Length;
      }

      string outcome = report[(lineEnd + 1)..nextEnd].TrimEnd('\r');
      outcome.ShouldContain(marker);
    }

    private static string Message(TagFilterSink sink, string displayName)
    {
      return sink.Results.Single(node => node.DisplayName == displayName).Message ?? string.Empty;
    }

    private static async Task<string> LaunchProbeAsync(string filter, string exclude)
    {
      string root = FindRepoRoot();
      string probe = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "tag-filter-probe.cs");
      CommandOutput output = await DotNet.Builder()
        .WithArguments("run", probe)
        .WithWorkingDirectory(root)
        .WithEnvironmentVariable("JARIBU_FILTER_TAG", filter)
        .WithEnvironmentVariable("JARIBU_EXCLUDE_TAG", exclude)
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3))
        .CaptureAsync();
      string text = Regex.Replace(output.Combined, @"\x1b\[[0-9;]*m", string.Empty);
      output.ExitCode.ShouldBe(0, text);
      return text;
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

    private sealed class TagFilterSink : ITestResultSink
    {
      public List<TestNodeInfo> Results { get; } = [];

      public string? FilterLabel { get; private set; }

      public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;

      public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

      public Task OnTestCompletedAsync(TestNodeInfo node)
      {
        Results.Add(node);
        return Task.CompletedTask;
      }

      public Task OnRunStartedAsync(string className, string? filterTag = null)
      {
        FilterLabel = filterTag;
        return Task.CompletedTask;
      }

      public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) =>
        Task.CompletedTask;
    }
  }
}
