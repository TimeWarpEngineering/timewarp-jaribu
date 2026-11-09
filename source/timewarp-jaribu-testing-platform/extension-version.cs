namespace TimeWarp.Jaribu.TestingPlatform;

using System.Reflection;

#region Purpose
// Informational version shared by the Jaribu MTP extensions.
#endregion

#region Design
// Assembly version drops the package prerelease label. MTP shows this string,
// so the informational version is the one both extensions report. A Source Link
// commit suffix after '+' is stripped.
#endregion

/// <summary>
/// Informational version shared by the framework extension and the command-line provider.
/// </summary>
internal static class ExtensionVersion
{
  public static string Current { get; } = Read();

  private static string Read()
  {
    Assembly assembly = typeof(ExtensionVersion).Assembly;
    string? informational = assembly
      .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
      ?.InformationalVersion;
    if (!string.IsNullOrWhiteSpace(informational))
    {
      int plus = informational.IndexOf('+', StringComparison.Ordinal);
      string version = plus > 0 ? informational[..plus] : informational;
      if (!string.IsNullOrWhiteSpace(version))
      {
        return version;
      }
    }

    return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
  }
}
