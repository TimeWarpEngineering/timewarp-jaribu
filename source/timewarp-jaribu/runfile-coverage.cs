namespace TimeWarp.Jaribu;

using System.Text.RegularExpressions;
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
// The settings file includes this repository's project assemblies. Sources match
// the git root or the deterministic /_/ prefix. A local manifest is
// dotnet-tools.json or .config/dotnet-tools.json. The walk stops at isRoot.
// A manifest found from the working directory keeps that directory. The
// entry-point fallback uses the manifest directory. A dotnet muxer host keeps
// argv[0] (the dll) on the command. A report with no classes fails the run.
// macOS arm64 and Linux arm have no dynamic profiler. A file-based runner is
// built and relaunched with --no-build. A compiled host is copied first so
// instrumentation does not rewrite assemblies this process has loaded.
#endregion

/// <summary>
/// Process relaunch that collects Cobertura for a runfile <see cref="TestRunner.RunAllTests"/> call.
/// </summary>
internal static partial class RunfileCoverage
{
  internal const string MissingToolMessage =
    "dotnet-coverage was not found. Install it with `dotnet tool install -g dotnet-coverage`, " +
    "or add it to a local tool manifest with `dotnet new tool-manifest` and `dotnet tool install dotnet-coverage`.";

  internal const string EmptyReportMessage =
    "The coverage report contains no classes. Deterministic builds record this repository's " +
    "sources as /_/ paths, and those paths stay in the report only for this repository's project " +
    "assemblies. An empty report is a failed collection, not a line-rate of 1.";

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

    string resultsDirectory = Path.GetFullPath(request.ResultsDirectory);
    Directory.CreateDirectory(resultsDirectory);
    string outputPath = Path.Combine(resultsDirectory, RunfileCoverageOptions.CoberturaFileName);
    string settingsPath = Path.Combine(resultsDirectory, RunfileCoverageOptions.SettingsFileName);
    string sourceRoot = ResolveSourceRoot(entryPoint);

    string runnerFile;
    string? runnerArgument;
    if (!string.IsNullOrWhiteSpace(entryPoint))
    {
      runnerFile = "dotnet";
      runnerArgument = Path.GetFullPath(entryPoint);
    }
    else
    {
      string processPath = Environment.ProcessPath ?? string.Empty;
      runnerFile = processPath;
      runnerArgument = MuxerEntryAssembly(processPath);
      if (runnerArgument is not null)
      {
        runnerArgument = Path.GetFullPath(runnerArgument);
      }
    }

    string workingDirectory = tool.WorkingDirectory ?? Directory.GetCurrentDirectory();
    string? ownedArtifacts = null;
    try
    {
      bool staticInstrumentation = RequiresStaticInstrumentation();
      IReadOnlyList<string> includeFiles = [];
      IReadOnlyList<string> runnerOptions = [];
      if (staticInstrumentation)
      {
        StaticLaunch? prepared = await PrepareStaticLaunch(
          runnerFile,
          runnerArgument,
          sourceRoot,
          entryPoint,
          workingDirectory,
          terminal,
          cancellationToken).ConfigureAwait(false);
        if (prepared is null)
        {
          return 1;
        }

        ownedArtifacts = prepared.Value.OwnedArtifactsPath;
        includeFiles = prepared.Value.IncludeFiles;
        runnerOptions = prepared.Value.RunnerOptions;
        if (!string.IsNullOrWhiteSpace(prepared.Value.RunnerFile))
        {
          runnerFile = prepared.Value.RunnerFile;
          runnerArgument = prepared.Value.RunnerArgument;
        }
      }

      WriteSettings(settingsPath, request.ModuleExcludes, sourceRoot, entryPoint, staticInstrumentation);
      string[] command = BuildCommand(
        tool,
        outputPath,
        settingsPath,
        runnerFile,
        runnerArgument,
        includeFiles,
        runnerOptions);
      int exitCode = await Shell.Builder(tool.FileName)
        .WithArguments(command)
        .WithWorkingDirectory(workingDirectory)
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "1")
        .WithNoValidation()
        .RunAsync(cancellationToken)
        .ConfigureAwait(false);
      if (exitCode == 0 && !ReportHasClasses(outputPath))
      {
        await WriteErrorAsync(terminal, EmptyReportMessage, cancellationToken).ConfigureAwait(false);
        return 1;
      }

      return exitCode;
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
      return 1;
    }
    finally
    {
      DeleteOwnedArtifacts(ownedArtifacts);
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

  private static string[] BuildCommand(
    CoverageTool tool,
    string outputPath,
    string settingsPath,
    string runnerFile,
    string? runnerArgument,
    IReadOnlyList<string> includeFiles,
    IReadOnlyList<string> runnerOptions)
  {
    List<string> arguments = [.. tool.PrefixArguments];
    arguments.Add("collect");
    arguments.Add("-f");
    arguments.Add("cobertura");
    arguments.Add("-o");
    arguments.Add(outputPath);
    arguments.Add("-s");
    arguments.Add(settingsPath);
    foreach (string includeFile in includeFiles)
    {
      arguments.Add("--include-files");
      arguments.Add(includeFile);
    }

    arguments.Add("--");
    arguments.Add(runnerFile);
    if (!string.IsNullOrWhiteSpace(runnerArgument))
    {
      arguments.Add(runnerArgument);
    }

    arguments.AddRange(runnerOptions);

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
}
