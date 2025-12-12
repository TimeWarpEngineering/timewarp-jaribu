namespace TimeWarp.Jaribu;

/// <summary>
/// Marker attribute indicating that this test class's runfile should be cleaned before execution.
/// Used by test orchestrators (like run-all-tests.cs) to determine which files need cleaning.
/// </summary>
/// <remarks>
/// <para>
/// <b>Important:</b> A runfile cannot clean itself while executing - that would corrupt the running process.
/// This attribute is a marker for ORCHESTRATORS that spawn test runfiles as subprocesses.
/// </para>
/// <para>
/// For orchestrators: Check for this attribute and run <c>dotnet clean &lt;file&gt;</c> BEFORE executing the runfile.
/// For manual execution: Run <c>dotnet clean yourtest.cs</c> before <c>dotnet yourtest.cs</c>.
/// </para>
/// <para>
/// This attribute aligns with the <c>dotnet clean &lt;runfile&gt;</c> command introduced in .NET 10.
/// <see cref="ClearRunfileCacheAttribute"/> is maintained for backward compatibility.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In orchestrator (run-all-tests.cs):
/// if (testClass.GetCustomAttribute&lt;CleanAttribute&gt;()?.Enabled == true)
/// {
///   await Shell.Builder("dotnet").WithArguments("clean", testFilePath).RunAsync();
/// }
/// await Shell.Builder("dotnet").WithArguments(testFilePath).RunAsync();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class CleanAttribute(bool enabled = true) : Attribute
{
  /// <summary>
  /// Gets whether the clean operation is enabled for this test class.
  /// </summary>
  public bool Enabled { get; } = enabled;
}
