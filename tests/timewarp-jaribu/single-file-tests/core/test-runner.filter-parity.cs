#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Discovery and run report the same node ids for every tag, method-name, and uid combination.
#endregion

#region Design
// Fixtures stay unregistered. The matrix calls DiscoverTestCases and RunTestsAsync
// with the same arguments, including a blank tag and a trimmed tag.
// A class tag miss must be Skipped nodes, not an empty run, and must not run hooks.
// JARIBU_FILTER_TAG is saved and restored. A blank value is not a filter.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;

[TestTag("Jaribu")]
public class FilterParity_Given_
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<FilterParity_Given_>();

  public static async Task Combinations_Should_ListTheSameUidsAsTheRun()
  {
    string? previous = Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");
    try
    {
      Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", null);
      await AssertMatrixAsync(typeof(ClassFilterParityFixture), ClassUids(), "Alpha");
      await AssertMatrixAsync(typeof(MethodFilterParityFixture), MethodUids(), "Keep");
      await AssertPredicateParityAsync();
    }
    finally
    {
      Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", previous);
    }
  }

  public static async Task ClassTagMiss_Should_SkipEveryMethodWithoutHooks()
  {
    ClassFilterParityFixture.Reset();
    FilterParitySink sink = new();
    TestRunStats stats = await TestRunner.RunTestsAsync<ClassFilterParityFixture>(sink, filterTag: "missing");

    stats.Success.ShouldBeTrue();
    stats.PassedCount.ShouldBe(0);
    stats.SkippedCount.ShouldBe(4);
    ClassFilterParityFixture.BodyRuns.ShouldBe(0);
    ClassFilterParityFixture.SetupOnceRuns.ShouldBe(0);
    sink.Results.ShouldAllBe(node => node.State == TestNodeState.Skipped);
    sink.Results.ShouldAllBe(node => node.Message == "No matching tag 'missing'");
    sink.Results.ShouldContain(node => node.DisplayName == nameof(ClassFilterParityFixture.Rows));
    sink.Results.ShouldNotContain(node => node.Uid.Contains("[0]", StringComparison.Ordinal));

    List<string> discovered =
    [
      .. TestRunner.DiscoverTestCases(typeof(ClassFilterParityFixture), "missing").Select(testCase => testCase.Uid)
    ];
    discovered.ShouldBe(CompletedUids(sink));
  }

  public static async Task MethodTagMiss_Should_SkipThatMethodAndRunTheRest()
  {
    MethodFilterParityFixture.Reset();
    FilterParitySink sink = new();
    TestRunStats stats = await TestRunner.RunTestsAsync<MethodFilterParityFixture>(sink, filterTag: "Keep");

    stats.PassedCount.ShouldBe(4);
    stats.SkippedCount.ShouldBe(1);
    MethodFilterParityFixture.BodyRuns.ShouldBe(4);
    TestNodeInfo skipped = sink.Results.Single(node => node.State == TestNodeState.Skipped);
    skipped.DisplayName.ShouldBe(nameof(MethodFilterParityFixture.Drop));
    skipped.Message.ShouldBe("No matching tag 'Keep'");

    List<string> discovered =
    [
      .. TestRunner.DiscoverTestCases(typeof(MethodFilterParityFixture), "Keep").Select(testCase => testCase.Uid)
    ];
    discovered.ShouldBe(CompletedUids(sink));
  }

  public static async Task EnvironmentTag_Should_IgnoreBlankTrimAndLoseToExplicit()
  {
    string? previous = Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");
    try
    {
      string[] blanks = ["", "   ", "\t"];
      foreach (string blank in blanks)
      {
        Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", blank);
        MethodFilterParityFixture.Reset();
        FilterParitySink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync<MethodFilterParityFixture>(sink);
        stats.SkippedCount.ShouldBe(0);
        stats.PassedCount.ShouldBe(5);
        MethodFilterParityFixture.BodyRuns.ShouldBe(5);

        List<string> discovered =
        [
          .. TestRunner.DiscoverTestCases(typeof(MethodFilterParityFixture)).Select(testCase => testCase.Uid)
        ];
        discovered.ShouldBe(CompletedUids(sink));
      }

      Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", " Keep ");
      MethodFilterParityFixture.Reset();
      FilterParitySink trimmed = new();
      TestRunStats trimmedStats = await TestRunner.RunTestsAsync<MethodFilterParityFixture>(trimmed);
      trimmedStats.SkippedCount.ShouldBe(1);
      trimmed.Results.Single(node => node.State == TestNodeState.Skipped).Message.ShouldBe("No matching tag 'Keep'");

      List<string> trimmedDiscovered =
      [
        .. TestRunner.DiscoverTestCases(typeof(MethodFilterParityFixture)).Select(testCase => testCase.Uid)
      ];
      trimmedDiscovered.ShouldBe(CompletedUids(trimmed));

      Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", "missing");
      MethodFilterParityFixture.Reset();
      FilterParitySink explicitTag = new();
      TestRunStats explicitStats = await TestRunner.RunTestsAsync<MethodFilterParityFixture>(
        explicitTag,
        filterTag: "Keep");
      explicitStats.PassedCount.ShouldBe(4);
      explicitStats.SkippedCount.ShouldBe(1);
      explicitTag.Results.Single(node => node.State == TestNodeState.Skipped)
        .DisplayName.ShouldBe(nameof(MethodFilterParityFixture.Drop));
      List<string> explicitDiscovered =
      [
        .. TestRunner.DiscoverTestCases(typeof(MethodFilterParityFixture), "Keep")
          .Select(testCase => testCase.Uid)
      ];
      explicitDiscovered.ShouldBe(CompletedUids(explicitTag));
    }
    finally
    {
      Environment.SetEnvironmentVariable("JARIBU_FILTER_TAG", previous);
    }
  }

  public static async Task InheritedClassTag_Should_SkipWhenTheBaseTagMisses()
  {
    FilterParitySink matched = new();
    TestRunStats matchedStats = await TestRunner.RunTestsAsync<DerivedFilterParityFixture>(matched, filterTag: "Suite");
    matchedStats.PassedCount.ShouldBe(1);
    matchedStats.SkippedCount.ShouldBe(0);

    FilterParitySink missed = new();
    TestRunStats missedStats = await TestRunner.RunTestsAsync<DerivedFilterParityFixture>(missed, filterTag: "missing");
    missedStats.PassedCount.ShouldBe(0);
    missedStats.SkippedCount.ShouldBe(1);
    missed.Results.Single().Message.ShouldBe("No matching tag 'missing'");

    List<string> discovered =
    [
      .. TestRunner.DiscoverTestCases(typeof(DerivedFilterParityFixture), "missing").Select(testCase => testCase.Uid)
    ];
    discovered.ShouldBe(CompletedUids(missed));
  }

  private static async Task AssertMatrixAsync(Type testClass, string?[] uids, string nameFragment)
  {
    string?[] tags = [null, "Suite", "suite", "missing", "  ", " Suite ", "Keep", "Drop"];
    string?[] names = [null, nameFragment, "Rows", "Numbers", "no-such-method"];
    foreach (string? tag in tags)
    {
      foreach (string? name in names)
      {
        foreach (string? uid in uids)
        {
          await AssertSameUidsAsync(testClass, tag, name, uid, methodPredicate: null);
        }
      }
    }
  }

  private static async Task AssertPredicateParityAsync()
  {
    await AssertSameUidsAsync(
      typeof(ClassFilterParityFixture),
      filterTag: "Suite",
      methodNameContains: null,
      uid: null,
      methodPredicate: method => method.Name == nameof(ClassFilterParityFixture.Alpha));
    await AssertSameUidsAsync(
      typeof(MethodFilterParityFixture),
      filterTag: "Keep",
      methodNameContains: "e",
      uid: MethodUid(typeof(MethodFilterParityFixture), nameof(MethodFilterParityFixture.Keep)),
      methodPredicate: method => method.Name.Contains('e'));
    await AssertSameUidsAsync(
      typeof(ClassFilterParityFixture),
      filterTag: "missing",
      methodNameContains: "Rows",
      uid: MethodUid(typeof(ClassFilterParityFixture), nameof(ClassFilterParityFixture.Rows)),
      methodPredicate: method => method.Name.Contains("Row", StringComparison.Ordinal));
  }

  private static async Task AssertSameUidsAsync(
    Type testClass,
    string? filterTag,
    string? methodNameContains,
    string? uid,
    Func<MethodInfo, bool>? methodPredicate)
  {
    Func<ReportedTestCase, bool>? casePredicate = uid is null
      ? null
      : testCase => string.Equals(testCase.Uid, uid, StringComparison.Ordinal);

    List<string> discovered =
    [
      .. TestRunner.DiscoverTestCases(
        testClass,
        filterTag,
        methodNameContains,
        methodPredicate,
        casePredicate).Select(testCase => testCase.Uid)
    ];

    FilterParitySink sink = new();
    await TestRunner.RunTestsAsync(
      testClass,
      sink,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate);

    List<string> ran = [.. sink.Results.Select(node => node.Uid)];
    ran.ShouldBe(
      discovered,
      $"tag '{filterTag ?? "<null>"}' name '{methodNameContains ?? "<null>"}' uid '{uid ?? "<null>"}'");
  }

  private static string?[] ClassUids()
  {
    Type type = typeof(ClassFilterParityFixture);
    return
    [
      null,
      MethodUid(type, nameof(ClassFilterParityFixture.Alpha)),
      MethodUid(type, nameof(ClassFilterParityFixture.Rows)),
      $"{MethodUid(type, nameof(ClassFilterParityFixture.Rows))}[0]",
      $"{type.FullName}.Missing"
    ];
  }

  private static string?[] MethodUids()
  {
    Type type = typeof(MethodFilterParityFixture);
    return
    [
      null,
      MethodUid(type, nameof(MethodFilterParityFixture.Keep)),
      MethodUid(type, nameof(MethodFilterParityFixture.Numbers)),
      $"{MethodUid(type, nameof(MethodFilterParityFixture.Numbers))}[0]",
      $"{type.FullName}.Missing"
    ];
  }

  private static string MethodUid(Type type, string name) => $"{type.FullName}.{name}";

  private static List<string> CompletedUids(FilterParitySink sink) => [.. sink.Results.Select(node => node.Uid)];

  private sealed class FilterParitySink : ITestResultSink
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
}

[TestTag("Suite")]
public class ClassFilterParityFixture
{
  public static int BodyRuns;
  public static int SetupOnceRuns;

  public static void Reset()
  {
    BodyRuns = 0;
    SetupOnceRuns = 0;
  }

  public static async Task SetupOnce()
  {
    SetupOnceRuns++;
    await Task.CompletedTask;
  }

  public static async Task Alpha()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }

  [TestTag("Other")]
  public static async Task Beta()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }

  [Input("a")]
  [Input("b")]
  public static async Task Rows(string value)
  {
    _ = value;
    BodyRuns++;
    await Task.CompletedTask;
  }

  [Skip("held")]
  public static async Task Held()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }
}

public class MethodFilterParityFixture
{
  public static int BodyRuns;

  public static void Reset() => BodyRuns = 0;

  [TestTag("Keep")]
  public static async Task Keep()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }

  [TestTag("Drop")]
  public static async Task Drop()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }

  public static async Task Plain()
  {
    BodyRuns++;
    await Task.CompletedTask;
  }

  [Input(1)]
  [Input(2)]
  public static async Task Numbers(int value)
  {
    _ = value;
    BodyRuns++;
    await Task.CompletedTask;
  }
}

[TestTag("Suite")]
public class TaggedBaseFixture;

public class DerivedFilterParityFixture : TaggedBaseFixture
{
  public static async Task Child()
  {
    await Task.CompletedTask;
  }
}
}
