namespace TimeWarp.Jaribu;

using System.Text.RegularExpressions;
using System.Xml.Linq;
using TimeWarp.Amuru;
using TimeWarp.Terminal;

#region Purpose
// Relaunches a runfile under dotnet-coverage when RunAllTests is asked for Cobertura.
#endregion

#region Design
// The parent does not run tests. It starts
// `dotnet-coverage collect -f cobertura -o <results-dir>/coverage.cobertura.xml -- dotnet <runner> …`,
// returns that process's exit code, and sets JARIBU_COVERAGE_ACTIVE=1 on the child.
// A missing tool is an error that names `dotnet tool install -g dotnet-coverage`.
// The coverage profiler GUID skips a second wrap when the process is already instrumented.
// CollectFromChildProcesses stays on, so per-file `dotnet run` children are included.
// The module filter is a coverage settings file of path regexes.
#endregion

/// <summary>
/// Process relaunch that collects Cobertura for a runfile <see cref="TestRunner.RunAllTests"/> call.
/// </summary>
internal static class RunfileCoverage
{
  internal const string MissingToolMessage =
    "dotnet-coverage was not found. Install it with `dotnet tool install -g dotnet-coverage`, " +
    "or add it to a local tool manifest with `dotnet new tool-manifest` and `dotnet tool install dotnet-coverage`.";

  private const string CoverageProfilerId = "324F817A-7420-4E6D-B3C1-143FBED6D855";

  /// <summary>
  /// Relaunches under dotnet-coverage when coverage was requested and this process is not
  /// already the child or the coverage profiler. Returns null when this process should run tests.
  /// </summary>
  internal static async Task<int?> TryRelaunchAsync(
    RunfileCoverageOptions? coverage,
    ITerminal? terminal,
    CancellationToken cancellationToken)
  {
    if (!IsRequested(coverage) || IsAlreadyInstrumented())
    {
      return null;
    }

    CoverageRequest request = ResolveRequest(coverage);
    string? invalidPattern = FirstInvalidPattern(request.ModuleExcludes);
    if (invalidPattern is not null)
    {
      await WriteErrorAsync(
        terminal,
        $"The coverage module filter contains an invalid regex: {invalidPattern}",
        cancellationToken).ConfigureAwait(false);
      return 1;
    }

    CoverageTool? located = LocateTool();
    if (located is not CoverageTool tool)
    {
      await WriteErrorAsync(terminal, MissingToolMessage, cancellationToken).ConfigureAwait(false);
      return 1;
    }

    string? entryPoint = AppContext.GetData("EntryPointFilePath") as string;
    if (string.IsNullOrWhiteSpace(entryPoint) && string.IsNullOrWhiteSpace(Environment.ProcessPath))
    {
      await WriteErrorAsync(
        terminal,
        "Runfile coverage cannot relaunch this process because it has no entry-point file and no process path.",
        cancellationToken).ConfigureAwait(false);
      return 1;
    }

    Directory.CreateDirectory(request.ResultsDirectory);
    string outputPath = Path.Combine(request.ResultsDirectory, RunfileCoverageOptions.CoberturaFileName);
    string settingsPath = Path.Combine(request.ResultsDirectory, RunfileCoverageOptions.SettingsFileName);
    WriteSettings(settingsPath, request.ModuleExcludes);

    string runnerFile;
    string? runnerArgument;
    if (!string.IsNullOrWhiteSpace(entryPoint))
    {
      runnerFile = "dotnet";
      runnerArgument = entryPoint;
    }
    else
    {
      runnerFile = Environment.ProcessPath ?? string.Empty;
      runnerArgument = null;
    }

    string[] command = BuildCommand(tool, outputPath, settingsPath, runnerFile, runnerArgument);
    try
    {
      int exitCode = await Shell.Builder(tool.FileName)
        .WithArguments(command)
        .WithWorkingDirectory(Directory.GetCurrentDirectory())
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "1")
        .WithNoValidation()
        .RunAsync(cancellationToken)
        .ConfigureAwait(false);
      return exitCode;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      return 1;
    }
  }

  private static bool IsRequested(RunfileCoverageOptions? coverage)
  {
    if (coverage is not null)
    {
      return true;
    }

    string? value = Environment.GetEnvironmentVariable(RunfileCoverageOptions.VariableName);
    return string.Equals(value, "1", StringComparison.Ordinal)
      || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
  }

  private static bool IsAlreadyInstrumented()
  {
    if (string.Equals(
      Environment.GetEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName),
      "1",
      StringComparison.Ordinal))
    {
      return true;
    }

    if (!string.Equals(Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING"), "1", StringComparison.Ordinal))
    {
      return false;
    }

    string? profiler = Environment.GetEnvironmentVariable("CORECLR_PROFILER");
    return profiler is not null
      && profiler.Contains(CoverageProfilerId, StringComparison.OrdinalIgnoreCase);
  }

  private static CoverageRequest ResolveRequest(RunfileCoverageOptions? coverage)
  {
    string resultsDirectory;
    string? moduleFilter;
    if (coverage is not null)
    {
      resultsDirectory = string.IsNullOrWhiteSpace(coverage.ResultsDirectory)
        ? RunfileCoverageOptions.DefaultResultsDirectory
        : coverage.ResultsDirectory;
      moduleFilter = coverage.ModuleFilter;
    }
    else
    {
      string? fromEnvironment = Environment.GetEnvironmentVariable(RunfileCoverageOptions.ResultsVariableName);
      resultsDirectory = string.IsNullOrWhiteSpace(fromEnvironment)
        ? RunfileCoverageOptions.DefaultResultsDirectory
        : fromEnvironment;
      moduleFilter = Environment.GetEnvironmentVariable(RunfileCoverageOptions.ModuleFilterVariableName);
    }

    return new CoverageRequest(resultsDirectory, ResolveExcludes(moduleFilter));
  }

  private static IReadOnlyList<string> ResolveExcludes(string? moduleFilter)
  {
    if (string.IsNullOrWhiteSpace(moduleFilter))
    {
      return RunfileCoverageOptions.DefaultModuleExcludes;
    }

    return moduleFilter.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  }

  private static string? FirstInvalidPattern(IReadOnlyList<string> patterns)
  {
    foreach (string pattern in patterns)
    {
      try
      {
        _ = Regex.Match(string.Empty, pattern, RegexOptions.None, TimeSpan.FromSeconds(1));
      }
      catch (ArgumentException)
      {
        return pattern;
      }
    }

    return null;
  }

  private static CoverageTool? LocateTool()
  {
    string? configured = Environment.GetEnvironmentVariable(RunfileCoverageOptions.ToolVariableName);
    if (!string.IsNullOrWhiteSpace(configured))
    {
      string? resolved = ResolveConfiguredTool(configured);
      return resolved is null ? null : new CoverageTool(resolved, []);
    }

    string? onPath = FindOnPath("dotnet-coverage");
    if (onPath is not null)
    {
      return new CoverageTool(onPath, []);
    }

    string userTools = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".dotnet",
      "tools");
    string[] userCandidates = OperatingSystem.IsWindows()
      ? [Path.Combine(userTools, "dotnet-coverage.exe"), Path.Combine(userTools, "dotnet-coverage")]
      : [Path.Combine(userTools, "dotnet-coverage")];
    foreach (string candidate in userCandidates)
    {
      if (File.Exists(candidate))
      {
        return new CoverageTool(candidate, []);
      }
    }

    string? entryPoint = AppContext.GetData("EntryPointFilePath") as string;
    if (HasLocalCoverageManifest(entryPoint))
    {
      return new CoverageTool("dotnet", ["tool", "run", "dotnet-coverage", "--"]);
    }

    return null;
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

  private static bool HasLocalCoverageManifest(string? entryPointFile)
  {
    if (ManifestListsCoverage(Directory.GetCurrentDirectory()))
    {
      return true;
    }

    if (string.IsNullOrWhiteSpace(entryPointFile))
    {
      return false;
    }

    string? directory = Path.GetDirectoryName(entryPointFile);
    if (string.IsNullOrWhiteSpace(directory))
    {
      return false;
    }

    return ManifestListsCoverage(directory);
  }

  private static bool ManifestListsCoverage(string startDirectory)
  {
    DirectoryInfo? directory = new(startDirectory);
    while (directory is not null)
    {
      string manifest = Path.Combine(directory.FullName, ".config", "dotnet-tools.json");
      if (File.Exists(manifest))
      {
        string text = File.ReadAllText(manifest);
        return text.Contains("\"dotnet-coverage\"", StringComparison.Ordinal);
      }

      directory = directory.Parent;
    }

    return false;
  }

  private static void WriteSettings(string settingsPath, IReadOnlyList<string> excludes)
  {
    XElement exclude = new(
      "Exclude",
      excludes.Select(pattern => new XElement("ModulePath", pattern)));
    XDocument document = new(
      new XDeclaration("1.0", "utf-8", "yes"),
      new XElement(
        "Configuration",
        new XElement(
          "CodeCoverage",
          new XElement("CollectFromChildProcesses", "True"),
          new XElement("ModulePaths", exclude))));
    document.Save(settingsPath);
  }

  private static string[] BuildCommand(
    CoverageTool tool,
    string outputPath,
    string settingsPath,
    string runnerFile,
    string? runnerArgument)
  {
    List<string> arguments = [.. tool.PrefixArguments];
    arguments.Add("collect");
    arguments.Add("-f");
    arguments.Add("cobertura");
    arguments.Add("-o");
    arguments.Add(outputPath);
    arguments.Add("-s");
    arguments.Add(settingsPath);
    arguments.Add("--");
    arguments.Add(runnerFile);
    if (!string.IsNullOrWhiteSpace(runnerArgument))
    {
      arguments.Add(runnerArgument);
    }

    string[] argv = Environment.GetCommandLineArgs();
    if (argv.Length > 1)
    {
      arguments.AddRange(argv[1..]);
    }

    return [.. arguments];
  }

  private static async Task WriteErrorAsync(
    ITerminal? terminal,
    string message,
    CancellationToken cancellationToken)
  {
    if (cancellationToken.IsCancellationRequested)
    {
      return;
    }

    if (terminal is not null)
    {
      await terminal.WriteErrorLineAsync(message).ConfigureAwait(false);
      return;
    }

    await Terminal.WriteErrorLineAsync(message).ConfigureAwait(false);
  }

  private readonly record struct CoverageRequest(string ResultsDirectory, IReadOnlyList<string> ModuleExcludes);

  private readonly record struct CoverageTool(string FileName, string[] PrefixArguments);
}
