namespace TimeWarp.Jaribu;

using System.Text.Json;

#region Purpose
// Finds dotnet-coverage on PATH, in the user tools directory, or in a local manifest.
#endregion

#region Design
// JARIBU_COVERAGE_TOOL wins. Otherwise PATH, then JARIBU_COVERAGE_USER_TOOLS
// (default ~/.dotnet/tools), then a manifest. Both dotnet-tools.json and
// .config/dotnet-tools.json count. A manifest that does not list the tool stops
// the walk only when isRoot is true. A manifest found from the working directory
// keeps that directory. The entry-point fallback uses the manifest directory
// so `dotnet tool run` can see a manifest outside the working directory.
#endregion

internal static partial class RunfileCoverage
{
  private const string CoverageToolPackageName = "dotnet-coverage";

  private static readonly string[] ManifestRelativePaths =
  [
    "dotnet-tools.json",
    Path.Combine(".config", "dotnet-tools.json")
  ];

  private static CoverageTool? LocateTool()
  {
    string? configured = Environment.GetEnvironmentVariable(RunfileCoverageOptions.ToolVariableName);
    if (!string.IsNullOrWhiteSpace(configured))
    {
      string? resolved = ResolveConfiguredTool(configured);
      return resolved is null ? null : new CoverageTool(resolved, [], null);
    }

    string? onPath = FindOnPath(CoverageToolPackageName);
    if (onPath is not null)
    {
      return new CoverageTool(onPath, [], null);
    }

    string userTools = UserToolsDirectory();
    string[] userCandidates = OperatingSystem.IsWindows()
      ? [Path.Combine(userTools, "dotnet-coverage.exe"), Path.Combine(userTools, "dotnet-coverage")]
      : [Path.Combine(userTools, "dotnet-coverage")];
    foreach (string candidate in userCandidates)
    {
      if (File.Exists(candidate))
      {
        return new CoverageTool(candidate, [], null);
      }
    }

    string? entryPoint = AppContext.GetData("EntryPointFilePath") as string;
    if (TryFindLocalTool(entryPoint, out string? manifestWorkingDirectory))
    {
      return new CoverageTool(
        "dotnet",
        ["tool", "run", CoverageToolPackageName, "--"],
        manifestWorkingDirectory);
    }

    return null;
  }

  private static string UserToolsDirectory()
  {
    string? configured = Environment.GetEnvironmentVariable(RunfileCoverageOptions.UserToolsVariableName);
    if (!string.IsNullOrWhiteSpace(configured))
    {
      return configured;
    }

    return Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".dotnet",
      "tools");
  }

  private static string? MuxerEntryAssembly(string processPath)
  {
    if (!IsDotnetMuxer(processPath))
    {
      return null;
    }

    string[] argv = Environment.GetCommandLineArgs();
    if (argv.Length == 0 || string.IsNullOrWhiteSpace(argv[0]))
    {
      return null;
    }

    if (string.Equals(
      Path.GetFileName(argv[0]),
      Path.GetFileName(processPath),
      StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    return argv[0];
  }

  private static bool IsDotnetMuxer(string processPath)
  {
    if (string.IsNullOrWhiteSpace(processPath))
    {
      return false;
    }

    string fileName = Path.GetFileName(processPath);
    return fileName.Equals("dotnet", StringComparison.OrdinalIgnoreCase)
      || fileName.Equals("dotnet.exe", StringComparison.OrdinalIgnoreCase);
  }

  private static string? ResolveConfiguredTool(string configured)
  {
    if (configured.Contains(Path.DirectorySeparatorChar, StringComparison.Ordinal)
      || configured.Contains(Path.AltDirectorySeparatorChar, StringComparison.Ordinal)
      || Path.IsPathRooted(configured))
    {
      return File.Exists(configured) ? configured : null;
    }

    return FindOnPath(configured);
  }

  private static string? FindOnPath(string name)
  {
    string? path = Environment.GetEnvironmentVariable("PATH");
    if (string.IsNullOrEmpty(path))
    {
      return null;
    }

    string[] extensions = OperatingSystem.IsWindows() ? [".exe", ".cmd", ".bat", ""] : [""];
    foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
    {
      foreach (string extension in extensions)
      {
        string candidate = Path.Combine(directory, name + extension);
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }
    }

    return null;
  }

  private static bool TryFindLocalTool(string? entryPointFile, out string? workingDirectory)
  {
    workingDirectory = null;
    ManifestSearch fromWorkingDirectory = SearchManifests(Directory.GetCurrentDirectory());
    if (fromWorkingDirectory.Directory is not null)
    {
      return true;
    }

    if (fromWorkingDirectory.StoppedAtRoot)
    {
      return false;
    }

    if (string.IsNullOrWhiteSpace(entryPointFile))
    {
      return false;
    }

    string? entryDirectory = Path.GetDirectoryName(Path.GetFullPath(entryPointFile));
    if (string.IsNullOrWhiteSpace(entryDirectory))
    {
      return false;
    }

    string? manifestDirectory = SearchManifests(entryDirectory).Directory;
    if (manifestDirectory is null)
    {
      return false;
    }

    workingDirectory = manifestDirectory;
    return true;
  }

  private static ManifestSearch SearchManifests(string startDirectory)
  {
    DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
    while (directory is not null)
    {
      bool sawManifest = false;
      bool isRoot = false;
      foreach (string relativePath in ManifestRelativePaths)
      {
        string manifest = Path.Combine(directory.FullName, relativePath);
        if (!File.Exists(manifest))
        {
          continue;
        }

        sawManifest = true;
        if (ManifestListsCoverage(manifest, out bool manifestIsRoot))
        {
          return new ManifestSearch(directory.FullName, false);
        }

        if (manifestIsRoot)
        {
          isRoot = true;
        }
      }

      if (sawManifest && isRoot)
      {
        return new ManifestSearch(null, true);
      }

      directory = directory.Parent;
    }

    return new ManifestSearch(null, false);
  }

  private static bool ManifestListsCoverage(string manifestPath, out bool isRoot)
  {
    isRoot = false;
    try
    {
      using FileStream stream = File.OpenRead(manifestPath);
      using JsonDocument document = JsonDocument.Parse(stream);
      JsonElement root = document.RootElement;
      if (root.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      if (root.TryGetProperty("isRoot", out JsonElement isRootElement)
        && isRootElement.ValueKind == JsonValueKind.True)
      {
        isRoot = true;
      }

      if (!root.TryGetProperty("tools", out JsonElement tools)
        || tools.ValueKind != JsonValueKind.Object)
      {
        return false;
      }

      foreach (JsonProperty tool in tools.EnumerateObject())
      {
        if (string.Equals(tool.Name, CoverageToolPackageName, StringComparison.Ordinal))
        {
          return true;
        }
      }

      return false;
    }
    catch (JsonException)
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

  private readonly record struct CoverageTool(
    string FileName,
    string[] PrefixArguments,
    string? WorkingDirectory);

  private readonly record struct ManifestSearch(string? Directory, bool StoppedAtRoot);
}
