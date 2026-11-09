namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Identity of the Jaribu test-framework extension.
#endregion

#region Design
// The command-line provider is a second IExtension and keeps its own uid.
// MTP treats a repeated uid as a duplicate extension. Version is the
// informational version, which carries the package prerelease label.
// The assembly version drops that label.
#endregion

internal sealed class JaribuExtension : IExtension
{
  public string Uid => "TimeWarp.Jaribu";
  public string Version => ExtensionVersion.Current;
  public string DisplayName => "TimeWarp.Jaribu Test Framework";
  public string Description => "Lightweight test framework for .NET runfiles and compiled projects";

  public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
