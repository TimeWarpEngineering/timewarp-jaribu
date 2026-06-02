namespace TimeWarp.Jaribu;

/// <summary>
/// Represents the lifecycle state of a test node, aligned with Microsoft.Testing.Platform's TestNodeStateProperty types.
/// </summary>
public enum TestNodeState
{
  /// <summary>Test has been discovered but not yet executed.</summary>
  Discovered,
  /// <summary>Test is executing.</summary>
  InProgress,
  /// <summary>Test passed successfully.</summary>
  Passed,
  /// <summary>Test failed due to assertion or exception.</summary>
  Failed,
  /// <summary>Test was skipped.</summary>
  Skipped,
  /// <summary>Test exceeded its timeout.</summary>
  Timeout,
  /// <summary>Test encountered an unexpected error during setup/teardown.</summary>
  Error,
  /// <summary>Test execution was cancelled.</summary>
  Cancelled
}
