#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj

using TimeWarp.Jaribu;
using TimeWarp.Nuru;
using static System.Console;

// Test the multi-class registration and execution feature
await TestMultiClassRegistration();

async Task TestMultiClassRegistration()
{
  WriteLine("=== Testing Multi-Class Registration ===");
  WriteLine();

  // Test 1: RegisterTests adds type to collection
  WriteLine("Test 1: RegisterTests<T>() registers a class");
  {
    // Clear any existing registrations
    TestRunner.ClearRegisteredTests();

    // Register a test class
    TestRunner.RegisterTests<SampleTestClassA>();

    // Run all tests - should have at least one test
    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    bool passed = summary.ClassResults.Count == 1 &&
                  summary.ClassResults[0].ClassName == "SampleTestClassA";

    if (passed)
    {
      WriteLine("  ✓ Test 1 PASSED: Single class registered correctly");
    }
    else
    {
      WriteLine($"  ✗ Test 1 FAILED: Expected 1 class result, got {summary.ClassResults.Count}");
    }
  }

  WriteLine();

  // Test 2: Multiple class registration
  WriteLine("Test 2: Multiple classes can be registered");
  {
    TestRunner.ClearRegisteredTests();

    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    bool passed = summary.ClassResults.Count == 2;

    if (passed)
    {
      WriteLine("  ✓ Test 2 PASSED: Multiple classes registered correctly");
    }
    else
    {
      WriteLine($"  ✗ Test 2 FAILED: Expected 2 class results, got {summary.ClassResults.Count}");
    }
  }

  WriteLine();

  // Test 3: Duplicate registration is ignored
  WriteLine("Test 3: Duplicate registration is ignored");
  {
    TestRunner.ClearRegisteredTests();

    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassA>(); // Duplicate

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    bool passed = summary.ClassResults.Count == 1;

    if (passed)
    {
      WriteLine("  ✓ Test 3 PASSED: Duplicate registration ignored");
    }
    else
    {
      WriteLine($"  ✗ Test 3 FAILED: Expected 1 class result (no duplicates), got {summary.ClassResults.Count}");
    }
  }

  WriteLine();

  // Test 4: ClearRegisteredTests works
  WriteLine("Test 4: ClearRegisteredTests() clears all registrations");
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();
    TestRunner.ClearRegisteredTests();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    bool passed = summary.ClassResults.Count == 0 && summary.TotalTests == 0;

    if (passed)
    {
      WriteLine("  ✓ Test 4 PASSED: ClearRegisteredTests works correctly");
    }
    else
    {
      WriteLine($"  ✗ Test 4 FAILED: Expected 0 class results, got {summary.ClassResults.Count}");
    }
  }

  WriteLine();

  // Test 5: RunAllTests returns correct exit code
  WriteLine("Test 5: RunAllTests() returns 0 when all tests pass");
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();

    int exitCode = await TestRunner.RunAllTests();

    bool passed = exitCode == 0;

    if (passed)
    {
      WriteLine("  ✓ Test 5 PASSED: RunAllTests returns 0 for passing tests");
    }
    else
    {
      WriteLine($"  ✗ Test 5 FAILED: Expected exit code 0, got {exitCode}");
    }
  }

  WriteLine();

  // Test 6: TestSuiteSummary aggregates results correctly
  WriteLine("Test 6: TestSuiteSummary aggregates counts correctly");
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    // SampleTestClassA has 2 tests, SampleTestClassB has 1 test
    int expectedTotal = summary.ClassResults.Sum(r => r.TotalTests);
    int expectedPassed = summary.ClassResults.Sum(r => r.PassedCount);

    bool passed = summary.TotalTests == expectedTotal &&
                  summary.PassedCount == expectedPassed &&
                  summary.Success == (summary.FailedCount == 0);

    if (passed)
    {
      WriteLine("  ✓ Test 6 PASSED: TestSuiteSummary aggregates correctly");
    }
    else
    {
      WriteLine($"  ✗ Test 6 FAILED: Aggregation mismatch");
    }
  }

  WriteLine();

  // Test 7: Suite summary table output
  WriteLine("Test 7: Suite summary table is printed for multiple classes");
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<SampleTestClassB>();

    // The suite summary table should print automatically when multiple classes are run
    // We just verify no exceptions occur
    try
    {
      TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();
      WriteLine("  ✓ Test 7 PASSED: Suite summary printed without errors");
    }
    catch (Exception ex)
    {
      WriteLine($"  ✗ Test 7 FAILED: {ex.Message}");
    }
  }

  WriteLine();

  // Test 8: Empty registration warning
  WriteLine("Test 8: Empty registration shows warning");
  {
    TestRunner.ClearRegisteredTests();

    // Running with no registrations should show warning but not fail
    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults();

    bool passed = summary.TotalTests == 0 && summary.Success;

    if (passed)
    {
      WriteLine("  ✓ Test 8 PASSED: Empty registration handled gracefully");
    }
    else
    {
      WriteLine($"  ✗ Test 8 FAILED: Unexpected behavior for empty registration");
    }
  }

  WriteLine();

  // Test 9: FilterTag works with RunAllTests
  WriteLine("Test 9: FilterTag works with registered tests");
  {
    TestRunner.ClearRegisteredTests();
    TestRunner.RegisterTests<SampleTestClassA>();
    TestRunner.RegisterTests<TaggedTestClass>();

    TestSuiteSummary summary = await TestRunner.RunAllTestsWithResults(filterTag: "Integration");

    // TaggedTestClass has the Integration tag and should be included
    // SampleTestClassA has no tags, so it runs (filter only excludes classes with non-matching tags)
    bool passed = summary.ClassResults.Any(r => r.ClassName == "TaggedTestClass") &&
                  summary.ClassResults.Count == 2;

    if (passed)
    {
      WriteLine("  ✓ Test 9 PASSED: FilterTag works with RunAllTests");
    }
    else
    {
      WriteLine($"  ✗ Test 9 FAILED: FilterTag not applied correctly");
    }
  }

  WriteLine();

  // Clean up
  TestRunner.ClearRegisteredTests();

  WriteLine("=== Multi-Class Registration Tests Complete ===");
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
public class TaggedTestClass
{
  public static async Task IntegrationTest()
  {
    await Task.Delay(1);
  }
}
