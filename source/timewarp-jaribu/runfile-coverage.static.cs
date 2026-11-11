namespace TimeWarp.Jaribu;

using System.Runtime.InteropServices;
using TimeWarp.Amuru;
using TimeWarp.Terminal;

#region Purpose
// Builds a runner and instruments that output when the dynamic profiler cannot attach.
#endregion

#region Design
// macOS arm64 and Linux arm do not support dynamic instrumentation. The collect
// then reports "Profiler was not initialized" and the Cobertura file has no
// classes. A file-based runner is built into a temp artifacts directory, and
// --no-build runs that output. A compiled host has already loaded its output,
// so that directory is copied and the copy is what gets instrumented and
// relaunched. JARIBU_COVERAGE_STATIC forces either path. The owned directory
// is deleted after the child exits. A later child `dotnet run` is not in that build.
#endregion

internal static partial class RunfileCoverage
{
  private readonly record struct StaticLaunch(
    string? OwnedArtifactsPath,
    IReadOnlyList<string> IncludeFiles,
    IReadOnlyList<string> RunnerOptions,
    string? RunnerFile = null,
    string? RunnerArgument = null);

  private static bool RequiresStaticInstrumentation()
  {
    string? forced = Environment.GetEnvironmentVariable(RunfileCoverageOptions.StaticVariableName);
    if (string.Equals(forced, "1", StringComparison.Ordinal)
      || string.Equals(forced, "true", StringComparison.OrdinalIgnoreCase))
    {
      return true;
    }

    if (string.Equals(forced, "0", StringComparison.Ordinal)
      || string.Equals(forced, "false", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }

    // Windows arm64 has a dynamic profiler. macOS arm64 and Linux arm do not.
    if (OperatingSystem.IsWindows())
    {
      return false;
    }

    return RuntimeInformation.OSArchitecture is Architecture.Arm64 or Architecture.Arm;
  }

  private static Task<StaticLaunch?> PrepareStaticLaunch(
    string runnerFile,
    string? runnerArgument,
    string sourceRoot,
    string? entryPointFile,
    string workingDirectory,
    ITerminal? terminal,
    CancellationToken cancellationToken)
  {
    string target = string.IsNullOrWhiteSpace(runnerArgument) ? runnerFile : runnerArgument;
    if (target.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    {
      return BuildFileBasedLaunchAsync(
        target,
        sourceRoot,
        entryPointFile,
        workingDirectory,
        terminal,
        cancellationToken);
    }

    return CopyLoadedHostLaunchAsync(
      runnerFile,
      runnerArgument,
      target,
      sourceRoot,
      entryPointFile,
      terminal,
      cancellationToken);
  }

  private static async Task<StaticLaunch?> CopyLoadedHostLaunchAsync(
    string runnerFile,
    string? runnerArgument,
    string target,
    string sourceRoot,
    string? entryPointFile,
    ITerminal? terminal,
    CancellationToken cancellationToken)
  {
    string fullTarget = Path.GetFullPath(target);
    string? outputDirectory = Path.GetDirectoryName(fullTarget);
    if (string.IsNullOrWhiteSpace(outputDirectory) || !Directory.Exists(outputDirectory))
    {
      await WriteErrorAsync(
        terminal,
        "Runfile coverage cannot statically instrument '" + target + "' because it has no output directory.",
        cancellationToken).ConfigureAwait(false);
      return null;
    }

    // The parent has already loaded this output. Instrumentation rewrites files
    // in place, which aborts that process after the child exits.
    string ownedDirectory = NewOwnedDirectoryPath();
    try
    {
      try
      {
        CopyDirectory(outputDirectory, ownedDirectory);
      }
      catch (IOException exception)
      {
        return await FailOwnedLaunchAsync(
          ownedDirectory,
          terminal,
          "Runfile coverage could not copy '" + outputDirectory + "' for static instrumentation."
            + Environment.NewLine
            + exception.Message,
          cancellationToken).ConfigureAwait(false);
      }
      catch (UnauthorizedAccessException exception)
      {
        return await FailOwnedLaunchAsync(
          ownedDirectory,
          terminal,
          "Runfile coverage could not copy '" + outputDirectory + "' for static instrumentation."
            + Environment.NewLine
            + exception.Message,
          cancellationToken).ConfigureAwait(false);
      }

      string copiedTarget = Path.GetFullPath(Path.Combine(ownedDirectory, Path.GetFileName(fullTarget)));
      if (!File.Exists(copiedTarget))
      {
        return await FailOwnedLaunchAsync(
          ownedDirectory,
          terminal,
          "Runfile coverage copied '" + outputDirectory + "' but '" + copiedTarget + "' is missing.",
          cancellationToken).ConfigureAwait(false);
      }

      List<string> includeFiles = FindBuiltAssemblies(
        ownedDirectory,
        IncludedAssemblyNames(sourceRoot, entryPointFile));
      if (includeFiles.Count == 0)
      {
        return await FailOwnedLaunchAsync(
          ownedDirectory,
          terminal,
          "Runfile coverage found no product assemblies to instrument in '" + ownedDirectory + "'.",
          cancellationToken).ConfigureAwait(false);
      }

      bool apphost = string.IsNullOrWhiteSpace(runnerArgument);
      return new StaticLaunch(
        ownedDirectory,
        includeFiles,
        [],
        apphost ? copiedTarget : runnerFile,
        apphost ? null : copiedTarget);
    }
    catch
    {
      DeleteOwnedArtifacts(ownedDirectory);
      throw;
    }
  }

  // dotnet-coverage restricts instrumentation to the exact --include-files paths
  // and matches them against the paths the runtime loads modules from. macOS temp
  // directories sit behind symlinks (/var -> /private/var), so the loaded path can
  // differ from the spelled one and a module is silently left out of the report.
  private static string NewOwnedDirectoryPath()
  {
    string root = ResolveSymbolicLinks(Path.GetFullPath(Path.GetTempPath()));
    return Path.Combine(root, "jaribu-runfile-coverage", Guid.NewGuid().ToString("N"));
  }

  private static string ResolveSymbolicLinks(string path)
  {
    try
    {
      string full = Path.GetFullPath(path);
      string? root = Path.GetPathRoot(full);
      if (string.IsNullOrEmpty(root))
      {
        return full;
      }

      string current = root;
      string[] segments = full[root.Length..].Split(
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
        StringSplitOptions.RemoveEmptyEntries);
      foreach (string segment in segments)
      {
        current = Path.Combine(current, segment);
        FileSystemInfo? target = new DirectoryInfo(current).ResolveLinkTarget(returnFinalTarget: true);
        if (target is not null)
        {
          current = Path.GetFullPath(target.FullName);
        }
      }

      return current;
    }
    catch (IOException)
    {
      return Path.GetFullPath(path);
    }
    catch (UnauthorizedAccessException)
    {
      return Path.GetFullPath(path);
    }
  }

  private static async Task<StaticLaunch?> FailOwnedLaunchAsync(
    string ownedDirectory,
    ITerminal? terminal,
    string message,
    CancellationToken cancellationToken)
  {
    DeleteOwnedArtifacts(ownedDirectory);
    await WriteErrorAsync(terminal, message, cancellationToken).ConfigureAwait(false);
    return null;
  }

  private static void CopyDirectory(string source, string destination)
  {
    Directory.CreateDirectory(destination);
    foreach (string file in Directory.EnumerateFiles(source))
    {
      File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: false);
    }

    foreach (string directory in Directory.EnumerateDirectories(source))
    {
      CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
  }

  private static async Task<StaticLaunch?> BuildFileBasedLaunchAsync(
    string entryPoint,
    string sourceRoot,
    string? entryPointFile,
    string workingDirectory,
    ITerminal? terminal,
    CancellationToken cancellationToken)
  {
    string artifactsPath = NewOwnedDirectoryPath();
    Directory.CreateDirectory(artifactsPath);
    try
    {
      CommandOutput build = await Shell.Builder("dotnet")
        .WithArguments(
          "build",
          entryPoint,
          "--artifacts-path",
          artifactsPath,
          "--nologo",
          "--verbosity",
          "minimal")
        .WithWorkingDirectory(workingDirectory)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      if (build.ExitCode != 0)
      {
        DeleteOwnedArtifacts(artifactsPath);
        string detail = string.IsNullOrWhiteSpace(build.Combined)
          ? "dotnet build failed."
          : build.Combined.Trim();
        await WriteErrorAsync(
          terminal,
          "Runfile coverage could not build '" + entryPoint + "' for static instrumentation."
            + Environment.NewLine
            + detail,
          cancellationToken).ConfigureAwait(false);
        return null;
      }

      string? outputDirectory = FindRunnerOutputDirectory(artifactsPath, entryPoint);
      if (outputDirectory is null)
      {
        DeleteOwnedArtifacts(artifactsPath);
        await WriteErrorAsync(
          terminal,
          "Runfile coverage built '" + entryPoint + "' but did not find its assembly under '"
            + artifactsPath
            + "'.",
          cancellationToken).ConfigureAwait(false);
        return null;
      }

      List<string> includeFiles = FindBuiltAssemblies(
        outputDirectory,
        IncludedAssemblyNames(sourceRoot, entryPointFile));
      if (includeFiles.Count == 0)
      {
        DeleteOwnedArtifacts(artifactsPath);
        await WriteErrorAsync(
          terminal,
          "Runfile coverage found no product assemblies to instrument in '" + outputDirectory + "'.",
          cancellationToken).ConfigureAwait(false);
        return null;
      }

      return new StaticLaunch(
        artifactsPath,
        includeFiles,
        ["--artifacts-path", artifactsPath, "--no-build"]);
    }
    catch
    {
      DeleteOwnedArtifacts(artifactsPath);
      throw;
    }
  }

  private static string? FindRunnerOutputDirectory(string artifactsPath, string entryPoint)
  {
    string assemblyFileName = Path.GetFileNameWithoutExtension(entryPoint) + ".dll";
    foreach (string candidate in Directory.EnumerateFiles(
      artifactsPath,
      assemblyFileName,
      SearchOption.AllDirectories))
    {
      string full = Path.GetFullPath(candidate);
      if (IsIgnoredBuildOutput(full))
      {
        continue;
      }

      return Path.GetDirectoryName(full);
    }

    return null;
  }

  private static bool IsIgnoredBuildOutput(string path)
  {
    string normalized = path.Replace('\\', '/');
    return normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
      || normalized.Contains("/ref/", StringComparison.OrdinalIgnoreCase)
      || normalized.Contains("/refint/", StringComparison.OrdinalIgnoreCase);
  }

  private static List<string> FindBuiltAssemblies(
    string outputDirectory,
    IReadOnlyList<string> assemblyNames)
  {
    List<string> files = [];
    foreach (string name in assemblyNames)
    {
      string candidate = Path.Combine(outputDirectory, name + ".dll");
      if (File.Exists(candidate))
      {
        files.Add(Path.GetFullPath(candidate));
      }
    }

    return files;
  }

  private static void DeleteOwnedArtifacts(string? artifactsPath)
  {
    if (string.IsNullOrWhiteSpace(artifactsPath))
    {
      return;
    }

    try
    {
      if (Directory.Exists(artifactsPath))
      {
        Directory.Delete(artifactsPath, recursive: true);
      }
    }
    catch (IOException)
    {
      // The report is already written. A leftover temp build is not a coverage failure.
    }
    catch (UnauthorizedAccessException)
    {
      // Same as IOException: the collect already finished.
    }
  }
}
