namespace TimeWarp.Jaribu;

#region Purpose
// Options that turn on Cobertura collection for a RunAllTests runfile launch.
#endregion

#region Design
// Opt-in is the object passed to RunAllTests, or JARIBU_COVERAGE=1 when that
// argument is omitted. Results directory and module filter have matching
// environment variables for the environment opt-in. JARIBU_COVERAGE_TOOL
// pins the dotnet-coverage executable. JARIBU_COVERAGE_ACTIVE is the marker
// the relaunch sets on the child so that child does not relaunch again.
// Module patterns match the assembly file name. A runfile cache directory
// such as run-ci-tests keeps product dlls that do not themselves contain tests.
// The settings file includes this repository's project assemblies. Sources match
// the repository directory or the deterministic /_/ prefix. Dependencies stay out.
// JARIBU_COVERAGE_USER_TOOLS replaces ~/.dotnet/tools in the tool search.
#endregion

/// <summary>
/// Requests a Cobertura report for a <see cref="TestRunner.RunAllTests"/> launch.
/// The default report includes this repository's project assemblies and excludes
/// test, Jaribu, and MSBuild assemblies. Dependency assemblies stay out, including
/// when a deterministic build records their sources under <c>/_/</c>.
/// </summary>
public sealed record RunfileCoverageOptions
{
  /// <summary>
  /// Set to <c>1</c> or <c>true</c> to collect coverage when <see cref="TestRunner.RunAllTests"/>
  /// is called without a <see cref="RunfileCoverageOptions"/> argument.
  /// </summary>
  public const string VariableName = "JARIBU_COVERAGE";

  /// <summary>
  /// Overrides <see cref="DefaultResultsDirectory"/> when coverage is requested through
  /// <see cref="VariableName"/>.
  /// </summary>
  public const string ResultsVariableName = "JARIBU_COVERAGE_RESULTS";

  /// <summary>
  /// Semicolon-separated module-path regexes that replace <see cref="DefaultModuleExcludes"/>
  /// when coverage is requested through <see cref="VariableName"/>.
  /// </summary>
  public const string ModuleFilterVariableName = "JARIBU_COVERAGE_MODULE_FILTER";

  /// <summary>
  /// Path of the <c>dotnet-coverage</c> executable. When set, PATH, the user tool
  /// directory, and a local manifest are not searched.
  /// </summary>
  public const string ToolVariableName = "JARIBU_COVERAGE_TOOL";

  /// <summary>
  /// Directory searched for a user-level <c>dotnet-coverage</c> executable before a
  /// local tool manifest. When unset, the directory is <c>~/.dotnet/tools</c>.
  /// </summary>
  public const string UserToolsVariableName = "JARIBU_COVERAGE_USER_TOOLS";

  /// <summary>
  /// Set to <c>1</c> on the relaunched child. A process that already has this value
  /// runs tests and does not relaunch.
  /// </summary>
  public const string ActiveVariableName = "JARIBU_COVERAGE_ACTIVE";

  /// <summary>
  /// Default directory for <see cref="CoberturaFileName"/>.
  /// </summary>
  public const string DefaultResultsDirectory = "artifacts/coverage";

  /// <summary>
  /// Cobertura file name written inside <see cref="ResultsDirectory"/>.
  /// </summary>
  public const string CoberturaFileName = "coverage.cobertura.xml";

  /// <summary>
  /// Settings file written beside the Cobertura report.
  /// </summary>
  public const string SettingsFileName = "coverage.settings.xml";

  /// <summary>
  /// Assembly file names that contain <c>tests</c>.
  /// A directory named tests does not match, so a product dll next to a runfile stays included.
  /// </summary>
  public const string TestsModuleExclude = @"(?i)[/\\][^/\\]*tests[^/\\]*\.(dll|exe)$";

  /// <summary>
  /// Assembly file names that contain <c>jaribu</c>.
  /// </summary>
  public const string JaribuModuleExclude = @"(?i)[/\\][^/\\]*jaribu[^/\\]*\.(dll|exe)$";

  /// <summary>
  /// Assembly file names that contain <c>msbuild</c>.
  /// </summary>
  public const string MsBuildModuleExclude = @"(?i)[/\\][^/\\]*msbuild[^/\\]*\.(dll|exe)$";

  /// <summary>
  /// Assembly file names that contain <c>microsoft.build</c>.
  /// </summary>
  public const string MicrosoftBuildModuleExclude = @"(?i)[/\\][^/\\]*microsoft\.build[^/\\]*\.(dll|exe)$";

  /// <summary>
  /// Module-path regexes excluded unless <see cref="ModuleFilter"/> replaces them.
  /// Each pattern matches the assembly file name, not a parent directory.
  /// The patterns drop test assemblies, Jaribu, and MSBuild nodes.
  /// The settings file also limits modules to this repository's projects and
  /// sources to that repository or the deterministic <c>/_/</c> prefix.
  /// <see cref="ModuleFilter"/> does not remove that rule.
  /// </summary>
  public static IReadOnlyList<string> DefaultModuleExcludes { get; } =
  [
    TestsModuleExclude,
    JaribuModuleExclude,
    MsBuildModuleExclude,
    MicrosoftBuildModuleExclude
  ];

  /// <summary>
  /// Directory that receives <see cref="CoberturaFileName"/>.
  /// The default is <see cref="DefaultResultsDirectory"/>.
  /// </summary>
  public string ResultsDirectory { get; init; } = DefaultResultsDirectory;

  /// <summary>
  /// Semicolon-separated module-path regexes that replace <see cref="DefaultModuleExcludes"/>.
  /// Null keeps that default.
  /// </summary>
  public string? ModuleFilter { get; init; }
}
