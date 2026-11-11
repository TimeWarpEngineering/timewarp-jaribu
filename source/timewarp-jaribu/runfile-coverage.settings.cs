namespace TimeWarp.Jaribu;

using System.Text.RegularExpressions;
using System.Xml.Linq;

#region Purpose
// Writes the dotnet-coverage settings file for a runfile Cobertura relaunch.
#endregion

#region Design
// Module excludes drop test, Jaribu, and MSBuild assemblies unless the caller
// replaces them. Sources are included only under the consuming repository:
// the git root of the entry-point file, otherwise the working directory.
// That source rule stays when the caller replaces the module excludes.
#endregion

internal static partial class RunfileCoverage
{
  private static string ResolveSourceRoot(string? entryPointFile)
  {
    if (!string.IsNullOrWhiteSpace(entryPointFile))
    {
      string? entryDirectory = Path.GetDirectoryName(Path.GetFullPath(entryPointFile));
      string? fromEntry = FindGitRoot(entryDirectory);
      if (fromEntry is not null)
      {
        return fromEntry;
      }
    }

    string? fromWorkingDirectory = FindGitRoot(Directory.GetCurrentDirectory());
    if (fromWorkingDirectory is not null)
    {
      return fromWorkingDirectory;
    }

    if (!string.IsNullOrWhiteSpace(entryPointFile))
    {
      string? entryDirectory = Path.GetDirectoryName(Path.GetFullPath(entryPointFile));
      if (!string.IsNullOrWhiteSpace(entryDirectory))
      {
        return entryDirectory;
      }
    }

    return Path.GetFullPath(Directory.GetCurrentDirectory());
  }

  private static string? FindGitRoot(string? startDirectory)
  {
    if (string.IsNullOrWhiteSpace(startDirectory))
    {
      return null;
    }

    DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
    while (directory is not null)
    {
      string gitPath = Path.Combine(directory.FullName, ".git");
      if (Directory.Exists(gitPath) || File.Exists(gitPath))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    return null;
  }

  private static string SourceRootPattern(string root)
  {
    string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
    string escaped = Regex.Escape(full).Replace(@"\\", @"[/\\]", StringComparison.Ordinal);
    return "(?i)^" + escaped + @"[/\\]";
  }

  private static void WriteSettings(string settingsPath, IReadOnlyList<string> excludes, string sourceRoot)
  {
    XElement exclude = new(
      "Exclude",
      excludes.Select(pattern => new XElement("ModulePath", pattern)));
    XElement sources = new(
      "Sources",
      new XElement(
        "Include",
        new XElement("Source", SourceRootPattern(sourceRoot))));
    XDocument document = new(
      new XDeclaration("1.0", "utf-8", "yes"),
      new XElement(
        "Configuration",
        new XElement(
          "CodeCoverage",
          new XElement("CollectFromChildProcesses", "True"),
          new XElement("ModulePaths", exclude),
          sources)));
    document.Save(settingsPath);
  }
}
