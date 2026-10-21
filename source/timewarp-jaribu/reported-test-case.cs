namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// One node that discovery and a normal run both agree to report for a test method.
#endregion

/// <summary>
/// Identity of a single reported test node: the method itself, or one <see cref="InputAttribute"/> row.
/// </summary>
/// <param name="Method">The test method this node runs or describes.</param>
/// <param name="InputOrdinal">
/// Metadata order of the <see cref="InputAttribute"/> row, or null when the node is the whole method.
/// </param>
/// <param name="Uid">Stable node id shared by discovery and execution.</param>
/// <param name="DisplayName">Human-readable name, including formatted arguments when the row has any.</param>
/// <param name="Parameters">Argument values for the row, or null when the node takes none.</param>
public readonly record struct ReportedTestCase(
  MethodInfo Method,
  int? InputOrdinal,
  string Uid,
  string DisplayName,
  IReadOnlyList<object?>? Parameters)
{
  /// <summary>
  /// Copies <see cref="Parameters"/> into the array <see cref="MethodInfo.Invoke(object, object[])"/> expects.
  /// </summary>
  public object?[] ToParameterArray() => Parameters is null ? [] : [.. Parameters];
}
