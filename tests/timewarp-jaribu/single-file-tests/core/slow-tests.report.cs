#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include ../../../../tools/dev-cli/endpoints/slow-tests-report.cs
#:include ../../../../tools/dev-cli/endpoints/slow-tests-signoz.cs

#region Purpose
// Proves local ranking, cumulative share, and the SigNoz response fixture.
#endregion

#region Design
// The helpers are the same files the dev CLI compiles. This file does not
// include the Nuru command. SigNoz mode parses a saved JSON response and
// does not call SigNoz. The MTP project lists those helpers itself because
// a normal project ignores #:include.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace SlowTests_
{
  using DevCli.Commands;

  [TestTag("Jaribu")]
  public class Report_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Report_Given_>();

    public static async Task Ranking_Should_SortByDurationAndAccumulateTheShare()
    {
      IReadOnlyList<DurationRow> rows = SlowTestReport.Read(Sample);
      string share = SlowTestReport.FormatLocal(rows, top: 2, outcome: null, markdown: false, source: "sample.json");
      share.ShouldContain("Source: sample.json");
      share.ShouldContain("Top 2 of 4 hold 80.0% of 100.0 ms.");

      string text = SlowTestReport.FormatLocal(rows, top: 20, outcome: null, markdown: false, source: null);
      int slow = text.IndexOf("Slow", StringComparison.Ordinal);
      int mid = text.IndexOf("Mid", StringComparison.Ordinal);
      int fast = text.IndexOf("Fast", StringComparison.Ordinal);
      int skip = text.IndexOf("Skip", StringComparison.Ordinal);
      slow.ShouldBeGreaterThanOrEqualTo(0);
      mid.ShouldBeGreaterThan(slow);
      fast.ShouldBeGreaterThan(mid);
      skip.ShouldBeGreaterThan(fast);
      text.ShouldContain("50.0%");
      text.ShouldContain("80.0%");
      text.ShouldContain("100.0%");

      string passed = SlowTestReport.FormatLocal(rows, top: 20, outcome: "passed", markdown: true, source: null);
      passed.ShouldContain("| Class | Method | Case | Arguments | Outcome | Duration ms | Cumulative | Tags |");
      passed.ShouldContain("Slow");
      passed.ShouldContain("Mid");
      passed.ShouldNotContain("Fast");
      passed.ShouldNotContain("Skip");
      passed.ShouldContain("Top 2 of 2 hold 100.0% of 80.0 ms.");
      await Task.CompletedTask;
    }

    public static async Task SignozFixture_Should_RankByP95AndJoinTheFailureRate()
    {
      string request = SignozSlowTests.BuildRequestJson(1, 2, "passed");
      request.ShouldContain("TimeWarp.Jaribu");
      request.ShouldContain("jaribu.test");
      request.ShouldContain("jaribu.test.outcome = 'passed'");
      request.ShouldContain("durationNano");
      request.ShouldContain("\"alias\":\"count\"");
      request.ShouldContain("\"alias\":\"p50\"");
      request.ShouldContain("\"alias\":\"p95\"");
      request.ShouldContain("\"alias\":\"max\"");
      request.ShouldContain("\"alias\":\"failures\"");
      request.ShouldContain("\"fieldContext\":\"attribute\"");
      request.ShouldContain("jaribu.test.case.index");
      request.ShouldContain("1000");

      string fixture = File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "signoz-slow-tests.json"));
      IReadOnlyList<SignozGroup> groups = SignozSlowTests.ParseResponse(fixture);
      groups.Count.ShouldBe(2);
      groups[0].Method.ShouldBe("Should_Pass");
      groups[0].Count.ShouldBe(4);
      groups[0].P50Milliseconds.ShouldBe(1500);
      groups[0].P95Milliseconds.ShouldBe(2000);
      groups[0].MaxMilliseconds.ShouldBe(3000);
      groups[0].FailureRate.ShouldBe(0.25);
      groups[1].Method.ShouldBe("Should_Echo");
      groups[1].CaseIndex.ShouldBe(0);
      groups[1].FailureRate.ShouldBe(0);
      string markdown = SignozSlowTests.Format(groups, top: 20, markdown: true, window: "7d");
      markdown.ShouldContain("Source: SigNoz TimeWarp.Jaribu jaribu.test since 7d");
      markdown.ShouldContain("| Class | Method | Case | Count | p50 ms | p95 ms | Max ms | Failure rate |");
      markdown.ShouldContain("1500.0");
      markdown.ShouldContain("25.0%");
      SignozSlowTests.RequirementMessage(null, null).ShouldBe("Set SIGNOZ_URL and SIGNOZ_API_KEY.");
      await Task.CompletedTask;
    }

    private const string Sample =
      """
      {
        "records": [
          {"class":"A","method":"Fast","caseIndex":null,"arguments":null,"outcome":"failed","durationMilliseconds":20,"tags":[]},
          {"class":"A","method":"Slow","caseIndex":null,"arguments":null,"outcome":"passed","durationMilliseconds":50,"tags":["Suite"]},
          {"class":"A","method":"Skip","caseIndex":null,"arguments":null,"outcome":"skipped","durationMilliseconds":null,"tags":[]},
          {"class":"A","method":"Mid","caseIndex":null,"arguments":null,"outcome":"passed","durationMilliseconds":30,"tags":[]}
        ]
      }
      """;

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

      throw new DirectoryNotFoundException("timewarp-jaribu.slnx was not found.");
    }
  }
}
