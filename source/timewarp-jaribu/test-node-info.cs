namespace TimeWarp.Jaribu;

using System.Reflection;

/// <summary>
/// Contains information about a single test node, aligned with Microsoft.Testing.Platform's TestNode concept.
/// </summary>
/// <param name="Uid">
/// Fully qualified test identifier. A method with no <c>[Input]</c> is <c>Namespace.Class.Method</c>.
/// Each <c>[Input]</c> row appends its metadata ordinal: <c>Namespace.Class.Method[0]</c>.
/// </param>
/// <param name="DisplayName">Human-readable test name, e.g. "MethodName" or "MethodName(param1, param2)".</param>
/// <param name="State">The current lifecycle state of the test node.</param>
/// <param name="Duration">How long the test took to execute, if completed.</param>
/// <param name="Exception">The exception if the test failed or errored, null otherwise.</param>
/// <param name="Message">Additional context such as skip reason or failure message.</param>
/// <param name="Parameters">The parameters passed to the test, if parameterized.</param>
/// <param name="Method">Declaring method when the runner knows it. Hosts use it for method identity and source location.</param>
public record TestNodeInfo
(
  string Uid,
  string DisplayName,
  TestNodeState State,
  TimeSpan? Duration = null,
  Exception? Exception = null,
  string? Message = null,
  IReadOnlyList<object?>? Parameters = null,
  MethodInfo? Method = null
);
