#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

// This is a meta-test file that tests the RegisterTests/RunAllTests API.
// It manipulates registration state directly, so it should NOT be included in multi-mode.
// These tests verify the multi-class registration feature works correctly.
//
// WHY THIS FILE DOESN'T FOLLOW THE STANDARD PATTERN:
// - Standard pattern: [ModuleInitializer] for registration + RunAllTests() in conditional
// - This file tests the registration API itself, calling ClearRegisteredTests() and RegisterTests<T>()
// - Using [ModuleInitializer] + RunAllTests() would cause "Collection was modified during enumeration"
//   because the tests modify the same RegisteredTestClasses collection that RunAllTests() iterates
// - Solution: Use RunTests<T>() directly, which doesn't use the registration collection

#if !JARIBU_MULTI
return await RunTests<MultiClassRegistrationTests>();
#endif

/// <summary>
/// A collecting sink that captures results per-class for multi-class inspection.
/// </summary>
sealed class MultiClassCollectingSink : ITestResultSink
{
  public List<TestRunStats> ClassStats { get; } = [];
  public List<TestNodeInfo> AllResults { get; } = [];

  public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
  public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    AllResults.Add(node);
    return Task.CompletedTask;
  }

  public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;

  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results)
  {
    ClassStats.Add(stats);
    return Task.CompletedTask;
  }
}

/// <summary>
/// Helper to run all registered test classes using a sink and collect per-class stats.
/// </summary>
static class MultiClassHelper
{
  public static async Task<MultiClassCollectingSink> RunAllRegisteredAsync(string? filterTag = null)
  {
    MultiClassCollectingSink sink = new();

    foreach (Type testClass in TestRunner.RegisteredTestClasses)
    {
      await TestRunner.RunTestsAsync(testClass, sink, filterTag);
    }

    return sink;
  }
}

/// <summary>
/// Meta-tests that validate the RegisterTests and RunAllTests API.
/// Note: This class manipulates the static registration state, so it cannot use [ModuleInitializer].
/// It must NOT be included in multi-mode orchestration.
/// </summary>
[TestTag("Jaribu")]
public class MultiClassRegistrationTests
{
  public static async Task SingleClassRegistration()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    sink.ClassStats.Count.ShouldBe(1);
    sink.ClassStats[0].ClassName.ShouldBe("SampleTestClassA");

    TestRunner.ClearRegisteredTests();
  }

  public static async Task MultipleClassRegistration()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    sink.ClassStats.Count.ShouldBe(2);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task DuplicateRegistrationIgnored()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassA>(); // Duplicate

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    sink.ClassStats.Count.ShouldBe(1);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task ClearRegisteredTestsWorks()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();
    TestRunner.ClearRegisteredTests();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    sink.ClassStats.Count.ShouldBe(0);
    sink.AllResults.Count.ShouldBe(0);
  }

  public static async Task RunAllTestsReturnsCorrectExitCode()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();

    int exitCode = await TestRunner.RunAllTests();

    exitCode.ShouldBe(0);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task TestSuiteStatsAggregateCorrectly()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    int expectedTotal = sink.ClassStats.Sum(r => r.TotalTests);
    int expectedPassed = sink.ClassStats.Sum(r => r.PassedCount);

    expectedTotal.ShouldBeGreaterThan(0);
    expectedPassed.ShouldBe(expectedTotal);
    sink.ClassStats.All(s => s.Success).ShouldBeTrue();

    TestRunner.ClearRegisteredTests();
  }

  public static async Task EmptyRegistrationHandledGracefully()
  {
    TestRunner.ClearRegisteredTests();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

    sink.ClassStats.Count.ShouldBe(0);
    sink.AllResults.Count.ShouldBe(0);
  }

  public static async Task FilterTagWorksWithRunAllRegistered()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<TaggedSampleTestClass>();

    MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync(filterTag: "Integration");

    // Both classes should be included (SampleTestClassA has no tags, TaggedSampleTestClass matches)
    sink.ClassStats.Any(r => r.ClassName == "TaggedSampleTestClass").ShouldBeTrue();
    sink.ClassStats.Count.ShouldBe(2);

    TestRunner.ClearRegisteredTests();
  }
}

// Sample test classes for testing the registration feature
public class SampleTestClassA
{
  public static async Task TestOne()
  {
    await Task.Delay(1);
  }

  public static async Task TestTwo()
  {
    await Task.Delay(1);
  }
}

public class SampleTestClassB
{
  public static async Task TestThree()
  {
    await Task.Delay(1);
  }
}

[TestTag("Integration")]
public class TaggedSampleTestClass
{
  public static async Task IntegrationTest()
  {
    await Task.Delay(1);
  }
}
