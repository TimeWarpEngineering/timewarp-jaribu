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
// classes. Static instrumentation rewrites the built dlls, and --no-build runs
// those files. JARIBU_COVERAGE_STATIC forces either path. The temp build is
// deleted after the child exits. A later child `dotnet run` is not in that build.
#endregion

internal static partial class RunfileCoverage
{
  private readonly record struct StaticLaunch(
    string? OwnedArtifactsPath,
    IReadOnlyList<string> IncludeFiles,
    IReadOnlyList<string> RunnerOptions);

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

  private static async Task<StaticLaunch?> PrepareStaticLaunchAsync(
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
      return await BuildFileBasedLaunchAsync(
        target,
        sourceRoot,
        entryPointFile,
        workingDirectory,
        terminal,
        cancellationToken).ConfigureAwait(false);
    }

    string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(target));
    if (string.IsNullOrWhiteSpace(outputDirectory))
    {
      await WriteErrorAsync(
        terminal,
        "Runfile coverage cannot statically instrument '" + target + "' because it has no directory.",
        cancellationToken).ConfigureAwait(false);
      return null;
    }

    List<string> includeFiles = FindBuiltAssemblies(
      outputDirectory,
      IncludedAssemblyNames(sourceRoot, entryPointFile));
    if (includeFiles.Count == 0)
    {
      await WriteErrorAsync(
        terminal,
        "Runfile coverage found no product assemblies to instrument in '" + outputDirectory + "'.",
        cancellationToken).ConfigureAwait(false);
      return null;
    }

    return new StaticLaunch(null, includeFiles, []);
  }

  private static async Task<StaticLaunch?> BuildFileBasedLaunchAsync(
    string entryPoint,
    string sourceRoot,
    string? entryPointFile,
    string workingDirectory,
    ITerminal? terminal,
    CancellationToken cancellationToken)
  {
    string artifactsPath = Path.GetFullPath(Path.Combine(
      Path.GetTempPath(),
      "jaribu-runfile-coverage",
      Guid.NewGuid().ToString("N")));
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
