namespace TimeWarp.Jaribu.TestingPlatform;

using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.CommandLine;

#region Purpose
// MTP command-line options for Jaribu selection filters:
// --filter-tag, --filter-class, --filter-method.
#endregion

#region Design
// Implements ICommandLineOptionsProvider (extends IExtension) wrapping the
// shared JaribuExtension identity. Options use ArgumentArity.ExactlyOne and
// are not hidden. ValidateOptionArgumentsAsync rejects empty args.
// Read at execute time via ServiceProviderExtensions.GetCommandLineOptions.
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
        "Run only tests with the given Jaribu TestTag (case-insensitive). CLI wins over JARIBU_FILTER_TAG.",
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
        isHidden: false)
    ];
  }

  public Task<ValidationResult> ValidateOptionArgumentsAsync(CommandLineOption commandOption, string[] arguments)
  {
    ArgumentNullException.ThrowIfNull(commandOption);
    ArgumentNullException.ThrowIfNull(arguments);

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
