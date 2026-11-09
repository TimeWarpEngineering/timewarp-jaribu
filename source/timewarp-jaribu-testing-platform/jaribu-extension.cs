namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Supplies the MTP extension identity shared by the framework and the command-line provider.
#endregion

#region Design
// The host treats the framework and the command-line provider as one extension
// when they share this uid. The version string is the assembly version.
// A missing assembly version falls back to 1.0.0.
#endregion

/// <summary>
/// Identity Microsoft.Testing.Platform shows for the Jaribu framework.
/// </summary>
internal sealed class JaribuExtension : IExtension
{
  /// <summary>Stable extension identifier shared with the command-line provider.</summary>
  public string Uid => "TimeWarp.Jaribu";

  /// <summary>Assembly version, or <c>1.0.0</c> when the assembly has none.</summary>
  public string Version => typeof(JaribuExtension).Assembly
    .GetName().Version?.ToString() ?? "1.0.0";

  /// <summary>Name shown by the platform.</summary>
  public string DisplayName => "TimeWarp.Jaribu Test Framework";

  /// <summary>Short description shown by the platform.</summary>
  public string Description => "Lightweight test framework for .NET runfiles and compiled projects";

  /// <summary>Jaribu is always enabled.</summary>
  /// <returns><see langword="true"/>.</returns>
  public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
