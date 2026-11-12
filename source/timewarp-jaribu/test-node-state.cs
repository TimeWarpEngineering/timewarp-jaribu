namespace TimeWarp.Jaribu;

#region Purpose
// Lifecycle states a test node can report, aligned with the testing platform.
#endregion

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
  /// <summary>
  /// Assertion or other exception thrown by the test, Setup, or a hook body, synchronously or from the returned task.
  /// </summary>
  Failed,
  /// <summary>Test was skipped.</summary>
  Skipped,
  /// <summary>Test exceeded its timeout.</summary>
  Timeout,
  /// <summary>
  /// Infrastructure fault: hook resolution, session fixture creation, a runner failure to invoke the test,
  /// or CleanUp throwing after a passing body.
  /// </summary>
  Error,
  /// <summary>Test execution was cancelled.</summary>
  Cancelled
}
