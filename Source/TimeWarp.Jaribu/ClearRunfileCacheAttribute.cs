namespace TimeWarp.Jaribu;

/// <summary>
/// Marker attribute indicating that this test class's runfile cache should be cleared before execution.
/// Maintained for backward compatibility - prefer <see cref="CleanAttribute"/> for new code.
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> A runfile cannot clean itself while executing - that would corrupt the running process.
/// This attribute is a marker for ORCHESTRATORS that spawn test runfiles as subprocesses.
/// </para>
/// <para>
/// This attribute is functionally equivalent to <see cref="CleanAttribute"/> and uses the
/// <c>dotnet clean &lt;runfile&gt;</c> command introduced in .NET 10.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ClearRunfileCacheAttribute(bool enabled = true) : Attribute
{
  /// <summary>
  /// Gets whether the clean operation is enabled for this test class.
  /// </summary>
  public bool Enabled { get; } = enabled;
}
