namespace TimeWarp.Jaribu.TestingPlatform;

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;

#region Purpose
// MTP command-line options for Jaribu selection filters and run policy:
// --filter-tag, --filter-class, --filter-method, --strict-signatures, --allow-empty.
#endregion

#region Design
// Implements ICommandLineOptionsProvider (extends IExtension) wrapping the
// shared JaribuExtension identity. Filter options use ArgumentArity.ExactlyOne.
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

  private readonly IExtension Extension;

  public JaribuCommandLineOptionsProvider(IExtension extension)
  {
    ArgumentNullException.ThrowIfNull(extension);
    Extension = extension;
  }

  public string Uid => Extension.Uid;
  public string Version => Extension.Version;
  public string DisplayName => Extension.DisplayName;
  public string Description => Extension.Description;

  public Task<bool> IsEnabledAsync() => Extension.IsEnabledAsync();

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
        isHidden: false)
    ];
  }

  public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
  {
    ArgumentNullException.ThrowIfNull(commandOption);
    ArgumentNullException.ThrowIfNull(arguments);

    if (commandOption.Name is StrictSignaturesOption or AllowEmptyOption)
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
