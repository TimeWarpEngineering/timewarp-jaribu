namespace TimeWarp.Jaribu;

#region Purpose
// Markers an instrumented build and an MSBuild property leave on an assembly.
#endregion

/// <summary>
/// Applied by the instrumenter to an assembly whose methods call <see cref="TautologyProbe.Enter"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class InstrumentedAssemblyAttribute : Attribute;

/// <summary>
/// Turns on per-test coverage for a test assembly when the MSBuild property <c>JaribuPerTestCoverage</c> is set.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class PerTestCoverageAttribute : Attribute;

/// <summary>
/// Turns unproven and zero-coverage tests into failures when the MSBuild property <c>JaribuStrictTautology</c> is set.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class StrictTautologyAttribute : Attribute;

/// <summary>
/// Requires a <see cref="ProvesAttribute"/> on every test when the MSBuild property <c>JaribuRequireClaims</c> is set.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class RequireClaimsAttribute : Attribute;
