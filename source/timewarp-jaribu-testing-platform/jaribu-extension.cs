namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Supplies the MTP extension identity shared by the framework and the command-line provider.
#endregion

#region Design
// The command-line provider is a second IExtension and keeps its own uid.
// MTP treats a repeated uid as a duplicate extension. Version is the
// informational version, which carries the package prerelease label.
// The assembly version drops that label.
#endregion

/// <summary>
/// Identity Microsoft.Testing.Platform shows for the Jaribu framework.
/// </summary>
internal sealed class JaribuExtension : IExtension
{
  /// <summary>Stable extension identifier shared with the command-line provider.</summary>
  public string Uid => "TimeWarp.Jaribu";

  /// <summary>Informational version, which keeps the package prerelease label.</summary>
  public string Version => ExtensionVersion.Current;

  /// <summary>Name shown by the platform.</summary>
  public string DisplayName => "TimeWarp.Jaribu Test Framework";

  /// <summary>Short description shown by the platform.</summary>
  public string Description => "Lightweight test framework for .NET runfiles and compiled projects";

  /// <summary>Jaribu is always enabled.</summary>
  /// <returns><see langword="true"/>.</returns>
  public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
