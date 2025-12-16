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

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    summary.ClassResults.Count.ShouldBe(1);
    summary.ClassResults[0].ClassName.ShouldBe("SampleTestClassA");

    TestRunner.ClearRegisteredTests();
  }

  public static async Task MultipleClassRegistration()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    summary.ClassResults.Count.ShouldBe(2);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task DuplicateRegistrationIgnored()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassA>(); // Duplicate

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    summary.ClassResults.Count.ShouldBe(1);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task ClearRegisteredTestsWorks()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();
    TestRunner.ClearRegisteredTests();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    summary.ClassResults.Count.ShouldBe(0);
    summary.TotalTests.ShouldBe(0);
  }

  public static async Task RunAllTestsReturnsCorrectExitCode()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();

    int exitCode = await TestRunner.RunAllTests();

    exitCode.ShouldBe(0);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task TestSuiteSummaryAggregatesCorrectly()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    int expectedTotal = summary.ClassResults.Sum(r => r.TotalTests);
    int expectedPassed = summary.ClassResults.Sum(r => r.PassedCount);

    summary.TotalTests.ShouldBe(expectedTotal);
    summary.PassedCount.ShouldBe(expectedPassed);
    summary.Success.ShouldBe(summary.FailedCount == 0);

    TestRunner.ClearRegisteredTests();
  }

  public static async Task EmptyRegistrationHandledGracefully()
  {
    TestRunner.ClearRegisteredTests();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    summary.TotalTests.ShouldBe(0);
    summary.Success.ShouldBeTrue();
  }

  public static async Task FilterTagWorksWithRunAllTests()
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<TaggedSampleTestClass>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults(filterTag: "Integration");

    // Both classes should be included (SampleTestClassA has no tags, TaggedSampleTestClass matches)
    summary.ClassResults.Any(r => r.ClassName == "TaggedSampleTestClass").ShouldBeTrue();
    summary.ClassResults.Count.ShouldBe(2);

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
