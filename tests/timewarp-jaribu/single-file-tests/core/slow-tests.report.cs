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
// does not call SigNoz. Rank is p50, then p95. A high p95 with a low p50
// stays below a steady high p50. The MTP project lists those helpers itself
// because a normal project ignores #:include. Empty text is an invalid file.
// A records array with no rows is a report of no rows.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace SlowTests_
{
  using System.Text.Json;
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

    public static async Task EmptyFile_Should_ThrowInvalidOperation()
    {
      InvalidOperationException exception = Should.Throw<InvalidOperationException>(
        () => SlowTestReport.Read(string.Empty));
      exception.Message.ShouldBe("Durations file is empty.");
      SlowTestReport.Read("""{"records":[]}""").Count.ShouldBe(0);
      await Task.CompletedTask;
    }

    public static async Task SignozFixture_Should_RankByP50AheadOfAOneOffStall()
    {
      string request = SignozSlowTests.BuildRequestJson(1, 2, "passed");
      request.ShouldContain("TimeWarp.Jaribu");
      request.ShouldContain("jaribu.test");
      request.ShouldContain("durationNano");
      request.ShouldContain("\"alias\":\"count\"");
      request.ShouldContain("\"alias\":\"p50\"");
      request.ShouldContain("\"alias\":\"p95\"");
      request.ShouldContain("\"alias\":\"max\"");
      request.ShouldContain("\"alias\":\"failures\"");
      request.ShouldContain("\"fieldContext\":\"attribute\"");
      request.ShouldContain("jaribu.test.case.index");
      request.ShouldContain("1000");
      QueryParts durations = Query(request, "A");
      durations.Filter.ShouldContain("jaribu.test.outcome = 'passed'");
      durations.Order.ShouldBe("p50");
      QueryParts failures = Query(request, "B");
      failures.Filter.ShouldContain("jaribu.test.outcome IN ('failed', 'timed_out', 'error', 'cancelled')");
      failures.Filter.ShouldNotContain("jaribu.test.outcome = 'passed'");
      failures.Order.ShouldBe("failures");
      QueryParts open = Query(SignozSlowTests.BuildRequestJson(1, 2, null), "A");
      open.Filter.ShouldNotContain("jaribu.test.outcome =");
      open.Order.ShouldBe("p50");

      string fixture = File.ReadAllText(Path.Combine(
        FindRepoRoot(),
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "signoz-slow-tests.json"));
      IReadOnlyList<SignozGroup> groups = SignozSlowTests.ParseResponse(fixture);
      groups.Count.ShouldBe(4);
      groups[0].Method.ShouldBe("Should_StaySlow");
      groups[0].P50Milliseconds.ShouldBe(2000);
      groups[0].P95Milliseconds.ShouldBe(2100);
      groups[1].Method.ShouldBe("Should_Pass");
      groups[1].Count.ShouldBe(4);
      groups[1].P50Milliseconds.ShouldBe(1500);
      groups[1].P95Milliseconds.ShouldBe(2000);
      groups[1].MaxMilliseconds.ShouldBe(3000);
      groups[1].FailureRate.ShouldBe(0.25);
      groups[2].Method.ShouldBe("Should_Echo");
      groups[2].CaseIndex.ShouldBe(0);
      groups[2].FailureRate.ShouldBe(0);
      groups[3].Method.ShouldBe("Should_StallOnce");
      groups[3].P50Milliseconds.ShouldBe(100);
      groups[3].P95Milliseconds.ShouldBe(10000);
      groups[0].P50Milliseconds.ShouldBeGreaterThan(groups[3].P50Milliseconds);
      groups[3].P95Milliseconds.ShouldBeGreaterThan(groups[0].P95Milliseconds);
      string markdown = SignozSlowTests.Format(groups, top: 20, markdown: true, window: "7d", showFailureRate: true);
      markdown.ShouldContain("Source: SigNoz TimeWarp.Jaribu jaribu.test since 7d");
      markdown.ShouldContain("groups by p50.");
      markdown.ShouldContain("| Class | Method | Case | Count | p50 ms | p95 ms | Max ms | Failure rate |");
      markdown.ShouldContain("1500.0");
      markdown.ShouldContain("25.0%");
      int steady = markdown.IndexOf("Should_StaySlow", StringComparison.Ordinal);
      int stall = markdown.IndexOf("Should_StallOnce", StringComparison.Ordinal);
      steady.ShouldBeGreaterThanOrEqualTo(0);
      stall.ShouldBeGreaterThan(steady);
      string filtered = SignozSlowTests.Format(groups, top: 20, markdown: true, window: "7d", showFailureRate: false);
      filtered.ShouldContain("Failure rate is blank because --outcome filters the group count.");
      filtered.ShouldNotContain("%");
      SignozSlowTests.RequirementMessage(null, null).ShouldBe("Set SIGNOZ_URL and SIGNOZ_API_KEY.");
      await Task.CompletedTask;
    }

    private static QueryParts Query(string json, string name)
    {
      using JsonDocument document = JsonDocument.Parse(json);
      JsonElement queries = document.RootElement.GetProperty("compositeQuery").GetProperty("queries");
      foreach (JsonElement query in queries.EnumerateArray())
      {
        JsonElement spec = query.GetProperty("spec");
        if (spec.GetProperty("name").GetString() != name)
        {
          continue;
        }

        string filter = spec.GetProperty("filter").GetProperty("expression").GetString() ?? string.Empty;
        string order = spec.GetProperty("order")[0].GetProperty("key").GetProperty("name").GetString() ?? string.Empty;
        return new QueryParts(filter, order);
      }

      throw new InvalidOperationException("Query " + name + " was not in the request.");
    }

    private readonly record struct QueryParts(string Filter, string Order);

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
