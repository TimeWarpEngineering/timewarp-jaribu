#!/usr/bin/dotnet --
#:project ../../../../source/timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests for the RegisterTests/RunAllTests multi-class registration API
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (MultiClassRegistration_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.MultiClassRegistration_Given_.SingleClass_Should_RegisterOne
//
// WHY THIS FILE DOESN'T FOLLOW THE STANDARD PATTERN:
// - Standard pattern: [ModuleInitializer] for registration + RunAllTests() in conditional
// - This file tests the registration API itself, calling ClearRegisteredTests() and RegisterTests<T>()
// - Using [ModuleInitializer] + RunAllTests() would cause "Collection was modified during enumeration"
//   because the tests modify the same RegisteredTestClasses collection that RunAllTests() iterates
// - Solution: Use RunTests<T>() directly, which doesn't use the registration collection
//
// Helper classes (MultiClassCollectingSink, MultiClassHelper, SampleTestClass*, etc.)
// are test fixtures that support the meta-tests.
#endregion

#if !JARIBU_MULTI
return await RunTests<TestRunner_.MultiClassRegistration_Given_>();
#endif

namespace TestRunner_
{
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

  [TestTag("Api")]
  public class MultiClassRegistration_Given_
  {
    public static async Task SingleClass_Should_RegisterOne()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<SampleTestClassA>();

      MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

      sink.ClassStats.Count.ShouldBe(1);
      sink.ClassStats[0].ClassName.ShouldBe("SampleTestClassA");

      TestRunner.ClearRegisteredTests();
    }

    public static async Task MultipleClasses_Should_RegisterAll()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<SampleTestClassA>();
      TestRunner.RegisterTests<SampleTestClassB>();

      MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

      sink.ClassStats.Count.ShouldBe(2);

      TestRunner.ClearRegisteredTests();
    }

    public static async Task DuplicateRegistration_Should_BeIgnored()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<SampleTestClassA>();
      TestRunner.RegisterTests<SampleTestClassA>(); // Duplicate

      MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

      sink.ClassStats.Count.ShouldBe(1);

      TestRunner.ClearRegisteredTests();
    }

    public static async Task ClearRegistered_Should_RemoveAll()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<SampleTestClassA>();
      TestRunner.RegisterTests<SampleTestClassB>();
      TestRunner.ClearRegisteredTests();

      MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

      sink.ClassStats.Count.ShouldBe(0);
      sink.AllResults.Count.ShouldBe(0);
    }

    public static async Task AllPassing_Should_ReturnExitCodeZero()
    {
      TestRunner.ClearRegisteredTests();
      TestRunner.RegisterTests<SampleTestClassA>();

      int exitCode = await TestRunner.RunAllTests();

      exitCode.ShouldBe(0);

      TestRunner.ClearRegisteredTests();
    }

    public static async Task MultipleClasses_Should_AggregateStatsCorrectly()
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

    public static async Task EmptyRegistration_Should_HandleGracefully()
    {
      TestRunner.ClearRegisteredTests();

      MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

      sink.ClassStats.Count.ShouldBe(0);
      sink.AllResults.Count.ShouldBe(0);
    }

    public static async Task FilterTag_Should_IncludeMatchingClasses()
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
}
