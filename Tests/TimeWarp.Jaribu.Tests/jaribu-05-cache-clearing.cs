#!/usr/bin/dotnet --

// Test cache clearing functionality
// Note: [Clean] and [ClearRunfileCache] attributes are for ORCHESTRATORS to clean OTHER files,
// not for a runfile to clean itself (which would corrupt the running process).
// Use: dotnet clean <file> BEFORE running, or use run-all-tests.cs --clean

await RunTests<CacheAttributeDocTest>();
await RunTests<CleanMethodTest>();

[TestTag("Jaribu")]
public class CacheAttributeDocTest
{
  /// <summary>
  /// Verify that [Clean] and [ClearRunfileCache] attributes exist and have correct properties.
  /// These attributes are markers for orchestrators, not for self-cleaning.
  /// </summary>
  public static async Task AttributesExistWithCorrectProperties()
  {
    // Verify CleanAttribute exists and has Enabled property
    var cleanAttr = new CleanAttribute(true);
    cleanAttr.Enabled.ShouldBe(true);

    var cleanAttrDisabled = new CleanAttribute(false);
    cleanAttrDisabled.Enabled.ShouldBe(false);

    // Verify ClearRunfileCacheAttribute exists (backward compatibility)
    var cacheAttr = new ClearRunfileCacheAttribute(true);
    cacheAttr.Enabled.ShouldBe(true);

    WriteLine("✓ Both [Clean] and [ClearRunfileCache] attributes exist with Enabled property");
    await Task.CompletedTask;
  }

  /// <summary>
  /// Verify default attribute values.
  /// </summary>
  public static async Task AttributeDefaultsToEnabled()
  {
    var cleanAttr = new CleanAttribute();
    cleanAttr.Enabled.ShouldBe(true);

    var cacheAttr = new ClearRunfileCacheAttribute();
    cacheAttr.Enabled.ShouldBe(true);

    WriteLine("✓ Attributes default to Enabled=true");
    await Task.CompletedTask;
  }
}

[TestTag("Jaribu")]
public class CleanMethodTest
{
  /// <summary>
  /// Verify TestRunner.RunClean() handles non-runfile execution gracefully.
  /// When not running as a runfile, it should return without error.
  /// </summary>
  public static async Task RunCleanHandlesNonRunfile()
  {
    // This test IS running as a runfile, but we can test the method exists
    // and is callable. The actual clean would be skipped for self-cleaning.
    // The method should handle edge cases gracefully.

    // Test that RunClean can be called with a non-existent file (should not throw)
    await TestRunner.RunClean("/nonexistent/path/test.cs");

    WriteLine("✓ RunClean handles non-existent paths gracefully");
  }

  /// <summary>
  /// Verify that attempting to clean the currently running file is skipped with a warning.
  /// </summary>
  public static async Task RunCleanSkipsSelfCleaning()
  {
    // Get current runfile path
    string? currentPath = AppContext.GetData("EntryPointFilePath") as string;
    currentPath.ShouldNotBeNull();

    // Capture console output
    var originalOut = Console.Out;
    using var sw = new StringWriter();
    Console.SetOut(sw);

    try
    {
      await TestRunner.RunClean(currentPath);
    }
    finally
    {
      Console.SetOut(originalOut);
    }

    string output = sw.ToString();
    output.ShouldContain("Skipping dotnet clean");
    output.ShouldContain("cannot clean currently executing runfile");

    WriteLine("✓ RunClean correctly skips self-cleaning with helpful message");
  }
}
