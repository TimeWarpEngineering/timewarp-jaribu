#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

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
// RunAllTestsCore and JaribuTestFramework snapshot RegisteredTestClasses before they iterate.
// This class registers with ModuleInitializer. Each test swaps the live list and restores it,
// so ClearRegisteredTests cannot drop later classes from that snapshot.
// SampleTestClass* fixtures are not ModuleInitializer-registered.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
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

      foreach (Type testClass in TestRunner.RegisteredTestClasses.ToArray())
      {
        await TestRunner.RunTestsAsync(testClass, sink, filterTag);
      }

      return sink;
    }
  }

  [TestTag("Api")]
  public class MultiClassRegistration_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<MultiClassRegistration_Given_>();

    public static async Task SingleClass_Should_RegisterOne()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        sink.ClassStats.Count.ShouldBe(1);
        sink.ClassStats[0].ClassName.ShouldBe("SampleTestClassA");
      });
    }

    public static async Task MultipleClasses_Should_RegisterAll()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();
        TestRunner.RegisterTests<SampleTestClassB>();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        sink.ClassStats.Count.ShouldBe(2);
      });
    }

    public static async Task DuplicateRegistration_Should_BeIgnored()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();
        TestRunner.RegisterTests<SampleTestClassA>();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        sink.ClassStats.Count.ShouldBe(1);
      });
    }

    public static async Task ClearRegistered_Should_RemoveAll()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();
        TestRunner.RegisterTests<SampleTestClassB>();
        TestRunner.ClearRegisteredTests();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        sink.ClassStats.Count.ShouldBe(0);
        sink.AllResults.Count.ShouldBe(0);
      });
    }

    public static async Task AllPassing_Should_ReturnExitCodeZero()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();

        int exitCode = await TestRunner.RunAllTests();

        exitCode.ShouldBe(0);
      });
    }

    public static async Task MultipleClasses_Should_AggregateStatsCorrectly()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();
        TestRunner.RegisterTests<SampleTestClassB>();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        int expectedTotal = sink.ClassStats.Sum(r => r.TotalTests);
        int expectedPassed = sink.ClassStats.Sum(r => r.PassedCount);

        expectedTotal.ShouldBeGreaterThan(0);
        expectedPassed.ShouldBe(expectedTotal);
        sink.ClassStats.All(s => s.Success).ShouldBeTrue();
      });
    }

    public static async Task EmptyRegistration_Should_HandleGracefully()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync();

        sink.ClassStats.Count.ShouldBe(0);
        sink.AllResults.Count.ShouldBe(0);
      });
    }

    public static async Task FilterTag_Should_IncludeMatchingClasses()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        TestRunner.RegisterTests<SampleTestClassA>();
        TestRunner.RegisterTests<TaggedSampleTestClass>();

        MultiClassCollectingSink sink = await MultiClassHelper.RunAllRegisteredAsync(filterTag: "Integration");

        // Both classes should be included (SampleTestClassA has no tags, TaggedSampleTestClass matches)
        sink.ClassStats.Any(r => r.ClassName == "TaggedSampleTestClass").ShouldBeTrue();
        sink.ClassStats.Count.ShouldBe(2);
      });
    }

    private static async Task WithIsolatedRegistryAsync(Func<Task> body)
    {
      Type[] saved = [.. TestRunner.RegisteredTestClasses];
      try
      {
        TestRunner.ClearRegisteredTests();
        await body();
      }
      finally
      {
        TestRunner.ClearRegisteredTests();
        System.Reflection.MethodInfo register = typeof(TestRunner).GetMethod(nameof(TestRunner.RegisterTests))!;
        foreach (Type type in saved)
        {
          register.MakeGenericMethod(type).Invoke(null, null);
        }
      }
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
