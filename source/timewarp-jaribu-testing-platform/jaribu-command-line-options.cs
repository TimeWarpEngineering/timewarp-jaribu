namespace TimeWarp.Jaribu.TestingPlatform;

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;

#region Purpose
// MTP command-line options for Jaribu selection filters and run policy:
// --filter-tag, --filter-class, --filter-method, --strict-signatures, --allow-empty,
// --per-test-coverage, --prove, --strict-tautology, --require-claims.
#endregion

#region Design
// Implements ICommandLineOptionsProvider (extends IExtension) with its own uid.
// The framework extension keeps TimeWarp.Jaribu. Sharing that uid trips MTP's
// duplicate-extension check. Filter options use ArgumentArity.ExactlyOne.
// The two policy flags use ArgumentArity.Zero. None are hidden.
// ValidateOptionArgumentsAsync rejects an empty filter argument and rejects
// an argument on a flag. Read at execute time via
// ServiceProviderExtensions.GetCommandLineOptions.
#endregion

/// <summary>
/// Provides Jaribu selection filter options for Microsoft.Testing.Platform.
/// </summary>
internal sealed class JaribuCommandLineOptionsProvider : ICommandLineOptionsProvider
{
  /// <summary>Option name: filter tests by <see cref="TestTagAttribute"/> tag.</summary>
  public const string FilterTagOption = "filter-tag";

  /// <summary>Option name: substring match on test class FullName.</summary>
  public const string FilterClassOption = "filter-class";

  /// <summary>Option name: substring match on test method name.</summary>
  public const string FilterMethodOption = "filter-method";

  /// <summary>Option name: fail the run on a near-miss test signature.</summary>
  public const string StrictSignaturesOption = "strict-signatures";

  /// <summary>Option name: allow a class with no discoverable tests.</summary>
  public const string AllowEmptyOption = "allow-empty";

  /// <summary>Option name: record the production methods each test enters.</summary>
  public const string PerTestCoverageOption = "per-test-coverage";

  /// <summary>Option name: break each claim and require the test to fail, then pass.</summary>
  public const string ProveOption = "prove";

  /// <summary>Option name: fail the run on an unproven or zero-coverage test.</summary>
  public const string StrictTautologyOption = "strict-tautology";

  /// <summary>Option name: require a [Proves] claim on every test.</summary>
  public const string RequireClaimsOption = "require-claims";

  public string Uid => "TimeWarp.Jaribu.CommandLine";
  public string Version => ExtensionVersion.Current;
  public string DisplayName => "TimeWarp.Jaribu Command Line";
  public string Description => "Jaribu selection filters and run policy";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);

  public IReadOnlyCollection<CommandLineOption> GetCommandLineOptions()
  {
    return
    [
      new CommandLineOption(
        FilterTagOption,
        "Run tests with this Jaribu TestTag (case-insensitive). A class or method whose tags exist and none match is Skipped. CLI wins over JARIBU_FILTER_TAG. A blank variable is ignored.",
        ArgumentArity.ExactlyOne,
        isHidden: false),
      new CommandLineOption(
        FilterClassOption,
        "Run only test classes whose FullName contains this substring (ordinal ignore-case).",
        ArgumentArity.ExactlyOne,
        isHidden: false),
      new CommandLineOption(
        FilterMethodOption,
        "Run only test methods whose name contains this substring (ordinal ignore-case).",
        ArgumentArity.ExactlyOne,
        isHidden: false),
      new CommandLineOption(
        StrictSignaturesOption,
        "Fail the run when a public static method has a near-miss test signature.",
        ArgumentArity.Zero,
        isHidden: false),
      new CommandLineOption(
        AllowEmptyOption,
        "Allow a class with no discoverable tests to succeed.",
        ArgumentArity.Zero,
        isHidden: false),
      new CommandLineOption(
        PerTestCoverageOption,
        "Record which production methods each test enters. Zero production methods is a tautology warning.",
        ArgumentArity.Zero,
        isHidden: false),
      new CommandLineOption(
        ProveOption,
        "Break each [Proves] claim, or the implied claim, and require the test to fail and then pass.",
        ArgumentArity.Zero,
        isHidden: false),
      new CommandLineOption(
        StrictTautologyOption,
        "Fail the run when a test is unproven or covers no production method.",
        ArgumentArity.Zero,
        isHidden: false),
      new CommandLineOption(
        RequireClaimsOption,
        "Require a [Proves] claim on every test.",
        ArgumentArity.Zero,
        isHidden: false)
    ];
  }

  public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
  {
    ArgumentNullException.ThrowIfNull(commandOption);
    ArgumentNullException.ThrowIfNull(arguments);

    if (commandOption.Name is StrictSignaturesOption
      or AllowEmptyOption
      or PerTestCoverageOption
      or ProveOption
      or StrictTautologyOption
      or RequireClaimsOption)
    {
      if (arguments.Length == 0)
      {
        return ValidationResult.ValidTask;
      }

      return ValidationResult.InvalidTask(
        $"Option '--{commandOption.Name}' does not take an argument.");
    }

    if (arguments.Length == 0 || string.IsNullOrWhiteSpace(arguments[0]))
    {
      return ValidationResult.InvalidTask(
        $"Option '--{commandOption.Name}' requires a non-empty argument.");
    }

    return ValidationResult.ValidTask;
  }

  public Task<ValidationResult> ValidateCommandLineOptionsAsync(ICommandLineOptions commandLineOptions)
  {
    return ValidationResult.ValidTask;
  }
}
