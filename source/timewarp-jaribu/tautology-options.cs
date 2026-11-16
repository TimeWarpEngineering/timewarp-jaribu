namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Opt-in switches for per-test coverage, prove mode, and strict tautology failures.
#endregion

#region Design
// A normal run leaves every switch off. Prove mode implies coverage, because a
// claim with no hits still has to be scored. Strict mode implies coverage so a
// zero-coverage test can fail. Require-claims does not by itself break members.
// Environment variables and assembly markers from MSBuild are read by Resolve.
// A nested runner call does not read them again. The outer run passes options
// explicitly, and a null argument inside a run stays off.
#endregion

/// <summary>
/// Coverage, prove mode, and strict-failure switches for one run.
/// </summary>
/// <param name="CollectCoverage">Record production methods each test enters.</param>
/// <param name="Prove">Break each claim and require the test to fail, then pass again.</param>
/// <param name="Strict">Turn unproven and zero-coverage tests into failures.</param>
/// <param name="RequireClaims">Fail or warn when a test has no <see cref="ProvesAttribute"/>.</param>
public sealed record TautologyOptions(
  bool CollectCoverage = false,
  bool Prove = false,
  bool Strict = false,
  bool RequireClaims = false)
{
  /// <summary>Environment variable that turns on per-test coverage.</summary>
  public const string CoverageVariableName = "JARIBU_PER_TEST_COVERAGE";

  /// <summary>Environment variable that turns on prove mode.</summary>
  public const string ProveVariableName = "JARIBU_PROVE";

  /// <summary>Environment variable that turns warnings into failures.</summary>
  public const string StrictVariableName = "JARIBU_STRICT_TAUTOLOGY";

  /// <summary>Environment variable that requires a <see cref="ProvesAttribute"/> on every test.</summary>
  public const string RequireClaimsVariableName = "JARIBU_REQUIRE_CLAIMS";

  /// <summary>All switches off.</summary>
  public static TautologyOptions Off { get; } = new();

  /// <summary>True when the run should score tautology at all.</summary>
  public bool Enabled => CollectCoverage || Prove || Strict || RequireClaims;

  /// <summary>
  /// Turns coverage on when prove or strict mode needs it.
  /// </summary>
  /// <returns>Options a run can execute.</returns>
  public TautologyOptions Normalize()
  {
    if (!Enabled)
    {
      return Off;
    }

    return this with { CollectCoverage = true };
  }

  /// <summary>
  /// Reads environment variables and assembly markers. An unset variable and a missing marker stay off.
  /// </summary>
  /// <returns>Normalized options for the outer run.</returns>
  public static TautologyOptions Resolve()
  {
    bool coverage = Flag(CoverageVariableName) || HasAttribute<PerTestCoverageAttribute>();
    bool prove = Flag(ProveVariableName);
    bool strict = Flag(StrictVariableName) || HasAttribute<StrictTautologyAttribute>();
    bool require = Flag(RequireClaimsVariableName) || HasAttribute<RequireClaimsAttribute>();
    return new TautologyOptions(coverage, prove, strict, require).Normalize();
  }

  /// <summary>
  /// Returns <paramref name="requested"/> when the caller passed it.
  /// The outer run resolves the environment. A nested run with a null argument stays off.
  /// </summary>
  internal static TautologyOptions ForRun(TautologyOptions? requested, int depth)
  {
    if (requested is not null)
    {
      return requested.Normalize();
    }

    if (depth > 0)
    {
      return Off;
    }

    return Resolve();
  }

  private static bool Flag(string name)
  {
    string? value = Environment.GetEnvironmentVariable(name);
    return value is "1" or "true" or "True";
  }

  private static bool HasAttribute<T>() where T : Attribute
  {
    Assembly? entry = Assembly.GetEntryAssembly();
    if (entry is null)
    {
      return false;
    }

    try
    {
      return entry.GetCustomAttributes(typeof(T), inherit: false).Length > 0;
    }
    catch (Exception exception) when (exception is FileNotFoundException
      or FileLoadException
      or TypeLoadException
      or BadImageFormatException)
    {
      return false;
    }
  }
}
