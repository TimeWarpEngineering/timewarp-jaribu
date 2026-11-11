namespace TimeWarp.Jaribu;

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

#region Purpose
// Writes the dotnet-coverage settings file for a runfile Cobertura relaunch.
#endregion

#region Design
// Module excludes drop test, Jaribu, and MSBuild assemblies unless the caller
// replaces them. Module includes are this repository's project assemblies and
// the runner assembly, and they stay when the caller replaces the excludes.
// Sources match the git root or the deterministic /_/ prefix CI writes into
// the PDB. A dependency that uses the same prefix stays out because its
// assembly is not one of those projects. A report with no classes is a miss.
#endregion

internal static partial class RunfileCoverage
{
  private const string DeterministicSourcePattern = "^/_/";

  private static readonly string[] SkippedProjectDirectories =
  [
    "bin",
    "obj",
    ".git",
    "node_modules",
    "artifacts"
  ];

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

  private static void WriteSettings(
    string settingsPath,
    IReadOnlyList<string> excludes,
    string sourceRoot,
    string? entryPointFile,
    bool staticInstrumentation)
  {
    List<XElement> moduleChildren = [];
    List<string> includes = ModuleIncludePatterns(sourceRoot, entryPointFile);
    if (includes.Count > 0)
    {
      moduleChildren.Add(
        new XElement(
          "Include",
          includes.Select(pattern => new XElement("ModulePath", pattern))));
    }

    moduleChildren.Add(
      new XElement(
        "Exclude",
        excludes.Select(pattern => new XElement("ModulePath", pattern))));
    XElement sources = new(
      "Sources",
      new XElement(
        "Include",
        new XElement("Source", SourceRootPattern(sourceRoot)),
        new XElement("Source", DeterministicSourcePattern)));
    XDocument document = new(
      new XDeclaration("1.0", "utf-8", "yes"),
      new XElement(
        "Configuration",
        new XElement(
          "CodeCoverage",
          new XElement("CollectFromChildProcesses", "True"),
          staticInstrumentation
            ? new XElement("EnableDynamicManagedInstrumentation", "False")
            : null,
          staticInstrumentation
            ? new XElement("EnableStaticManagedInstrumentation", "True")
            : null,
          new XElement("ModulePaths", moduleChildren),
          sources)));
    document.Save(settingsPath);
  }

  private static List<string> IncludedAssemblyNames(string sourceRoot, string? entryPointFile)
  {
    List<string> names = [];
    AddProjectAssemblyNames(sourceRoot, names);
    AddAssemblyName(names, EntryAssemblyName(entryPointFile));
    names.Sort(StringComparer.OrdinalIgnoreCase);
    return names;
  }

  private static List<string> ModuleIncludePatterns(string sourceRoot, string? entryPointFile)
  {
    List<string> names = IncludedAssemblyNames(sourceRoot, entryPointFile);
    List<string> patterns = [];
    foreach (string name in names)
    {
      patterns.Add(ModuleIncludePattern(name));
    }

    return patterns;
  }

  private static string ModuleIncludePattern(string assemblyName)
  {
    return $@"(?i)[/\\]{Regex.Escape(assemblyName)}\.(dll|exe)$";
  }

  private static void AddProjectAssemblyNames(string sourceRoot, List<string> names)
  {
    if (string.IsNullOrWhiteSpace(sourceRoot) || !Directory.Exists(sourceRoot))
    {
      return;
    }

    Stack<string> pending = new();
    pending.Push(Path.GetFullPath(sourceRoot));
    while (pending.Count > 0)
    {
      string directory = pending.Pop();
      IEnumerable<string> entries;
      try
      {
        entries = Directory.EnumerateFileSystemEntries(directory);
      }
      catch (IOException)
      {
        continue;
      }
      catch (UnauthorizedAccessException)
      {
        continue;
      }

      foreach (string entry in entries)
      {
        if (Directory.Exists(entry))
        {
          if (!IsSkippedProjectDirectory(entry))
          {
            pending.Push(entry);
          }

          continue;
        }

        if (entry.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
        {
          AddAssemblyName(names, AssemblyNameFromProject(entry));
        }
      }
    }
  }

  private static bool IsSkippedProjectDirectory(string path)
  {
    try
    {
      FileAttributes attributes = File.GetAttributes(path);
      if ((attributes & FileAttributes.ReparsePoint) != 0)
      {
        return true;
      }
    }
    catch (IOException)
    {
      return true;
    }
    catch (UnauthorizedAccessException)
    {
      return true;
    }

    string name = Path.GetFileName(path);
    foreach (string skipped in SkippedProjectDirectories)
    {
      if (string.Equals(name, skipped, StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }

    return false;
  }

  private static string AssemblyNameFromProject(string projectPath)
  {
    string fallback = Path.GetFileNameWithoutExtension(projectPath);
    try
    {
      XDocument document = XDocument.Load(projectPath);
      foreach (XElement element in document.Descendants("AssemblyName"))
      {
        if (element.Attribute("Condition") is not null)
        {
          continue;
        }

        string value = element.Value.Trim();
        if (value.Length > 0 && !value.Contains("$(", StringComparison.Ordinal))
        {
          return value;
        }
      }
    }
    catch (XmlException)
    {
      return fallback;
    }
    catch (IOException)
    {
      return fallback;
    }
    catch (UnauthorizedAccessException)
    {
      return fallback;
    }

    return fallback;
  }

  private static void AddAssemblyName(List<string> names, string? assemblyName)
  {
    if (string.IsNullOrWhiteSpace(assemblyName))
    {
      return;
    }

    foreach (string existing in names)
    {
      if (string.Equals(existing, assemblyName, StringComparison.OrdinalIgnoreCase))
      {
        return;
      }
    }

    names.Add(assemblyName);
  }

  private static string? EntryAssemblyName(string? entryPointFile)
  {
    if (!string.IsNullOrWhiteSpace(entryPointFile))
    {
      return Path.GetFileNameWithoutExtension(entryPointFile);
    }

    string? muxerAssembly = MuxerEntryAssembly(Environment.ProcessPath ?? string.Empty);
    if (!string.IsNullOrWhiteSpace(muxerAssembly))
    {
      return Path.GetFileNameWithoutExtension(muxerAssembly);
    }

    string? processPath = Environment.ProcessPath;
    if (string.IsNullOrWhiteSpace(processPath) || IsDotnetMuxer(processPath))
    {
      return null;
    }

    return Path.GetFileNameWithoutExtension(processPath);
  }

  private static bool ReportHasClasses(string outputPath)
  {
    if (!File.Exists(outputPath))
    {
      return false;
    }

    try
    {
      XDocument document = XDocument.Load(outputPath);
      foreach (XElement element in document.Descendants())
      {
        if (string.Equals(element.Name.LocalName, "class", StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }

      return false;
    }
    catch (XmlException)
    {
      return false;
    }
    catch (IOException)
    {
      return false;
    }
    catch (UnauthorizedAccessException)
    {
      return false;
    }
  }
}
