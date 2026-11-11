#region Purpose
// Runfile coverage relaunch assertions, the dotnet muxer host, and the coverage-tool fake.
#endregion

#region Design
// The fake records the command. A second directory named dotnet points at that
// fake so manifest tests can hide the real dotnet-coverage without hiding the
// SDK that compiles the probe.
#endregion

namespace TestRunner_;

public partial class RunfileCoverage_Given_
{
    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task DotnetMuxer_Should_KeepTheEntryAssemblyOnTheRelaunch()
    {
      using ProbeRun run = ProbeRun.Create();
      string tool = await EnsureFakeToolAsync();
      string host = await EnsureMuxerHostAsync();
      string dotnet = FindDotnet();
      TimeWarp.Amuru.CommandOutput output = await IsolateCoverage(
          TimeWarp.Amuru.Shell.Builder(dotnet)
            .WithArguments(host, run.Directory)
            .WithWorkingDirectory(FindRepoRoot()),
          run,
          tool)
        .CaptureAsync();

      output.ExitCode.ShouldBe(0, output.Combined);
      AssertOneLaunch(run);
      AssertActiveChild(run);
      string settingsPath = Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName);
      AssertCommand(
        ReadCommand(run),
        "collect",
        "-f",
        "cobertura",
        "-o",
        Path.GetFullPath(Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName)),
        "-s",
        Path.GetFullPath(settingsPath),
        "--",
        dotnet,
        Path.GetFullPath(host),
        run.Directory);
    }

    private static void AssertOneLaunch(ProbeRun run)
    {
      string log = File.ReadAllText(run.LogPath);
      int launches = 0;
      foreach (string line in log.Split('\n'))
      {
        if (line.TrimEnd('\r') == "ARGS")
        {
          launches++;
        }
      }

      launches.ShouldBe(1);
    }

    private static async Task<string> EnsureFakeToolAsync()
    {
      await BuildGate.WaitAsync();
      try
      {
        if (FakeToolPath is not null && File.Exists(FakeToolPath))
        {
          return FakeToolPath;
        }

        string root = FindRepoRoot();
        string project = Path.Combine(root, "tests", "timewarp-jaribu", "coverage-tool-fake", "coverage-tool-fake.csproj");
        TimeWarp.Amuru.CommandOutput build = await TimeWarp.Amuru.Shell.Builder("dotnet")
          .WithArguments("build", project, "-c", "Release", "--nologo")
          .WithWorkingDirectory(root)
          .WithNoValidation()
          .WithTimeout(TimeSpan.FromMinutes(3))
          .CaptureAsync();
        build.ExitCode.ShouldBe(0, build.Combined);

        string built = Path.Combine(
          root,
          "tests",
          "timewarp-jaribu",
          "coverage-tool-fake",
          "bin",
          "Release",
          "net11.0",
          "coverage-tool-fake");
        if (OperatingSystem.IsWindows())
        {
          built += ".exe";
        }

        File.Exists(built).ShouldBeTrue(build.Combined);
        FakeToolPath = built;
        FakeDotnetDirectory = LinkFakeDotnet(built);
        return built;
      }
      finally
      {
        BuildGate.Release();
      }
    }

    private static Task<TimeWarp.Amuru.CommandOutput> RunProbeAsync(
      ProbeRun run,
      string mode,
      string toolPath,
      string? probeExit,
      string coverageVariable,
      string? moduleFilter = null,
      bool passFilterAsArgument = true,
      bool useResultsVariable = false)
    {
      string root = FindRepoRoot();
      string probe = ProbePath();
      List<string> arguments = ["run", probe, "--", mode, run.Directory];
      if (passFilterAsArgument && moduleFilter is not null)
      {
        arguments.Add(moduleFilter);
      }

      TimeWarp.Amuru.ShellBuilder builder = TimeWarp.Amuru.Shell.Builder("dotnet")
        .WithArguments([.. arguments])
        .WithWorkingDirectory(root)
        .WithEnvironmentVariable(RunfileCoverageOptions.ToolVariableName, toolPath)
        .WithEnvironmentVariable("JARIBU_COVERAGE_FAKE_LOG", run.LogPath)
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "0")
        .WithEnvironmentVariable(RunfileCoverageOptions.VariableName, coverageVariable)
        .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_32", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_64", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_ARM64", "0")
        .WithEnvironmentVariable(
          RunfileCoverageOptions.ResultsVariableName,
          useResultsVariable ? run.Directory : "0")
        .WithEnvironmentVariable(
          RunfileCoverageOptions.ModuleFilterVariableName,
          useResultsVariable ? moduleFilter ?? "0" : "0")
        .WithEnvironmentVariable("JARIBU_COVERAGE_PROBE_EXIT", probeExit ?? string.Empty)
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3));

      return builder.CaptureAsync();
    }

    private static string FindRepoRoot()
    {
      DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
      while (directory is not null)
      {
        if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      throw new InvalidOperationException(
        $"Repository root not found from '{Directory.GetCurrentDirectory()}'.");
    }

    private static string ProbePath()
    {
      return Path.Combine(
        FindRepoRoot(),
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "runfile-coverage-probe.cs");
    }

    private static void AssertActiveChild(ProbeRun run)
    {
      File.ReadAllLines(run.LogPath).ShouldContain("ACTIVE=1");
    }

    private static void AssertRepositorySources(string settingsPath)
    {
      System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Load(settingsPath);
      string pattern = document.Descendants("Source").Single().Value;
      System.Text.RegularExpressions.Regex regex = new(
        pattern,
        System.Text.RegularExpressions.RegexOptions.None,
        TimeSpan.FromSeconds(1));
      string product = Path.Combine(FindRepoRoot(), "source", "timewarp-jaribu", "runfile-coverage.cs");
      regex.IsMatch(product).ShouldBeTrue(pattern);
      regex.IsMatch("/_/CliWrap/Cli.cs").ShouldBeFalse(pattern);
      regex.IsMatch("/_/source/timewarp-amuru/src/Command.cs").ShouldBeFalse(pattern);
      regex.IsMatch("/_/TimeWarp.Builder/Builder.cs").ShouldBeFalse(pattern);
      regex.IsMatch(
          "/home/runner/work/timewarp-builder/timewarp-builder/source/timewarp-builder/builder.cs")
        .ShouldBeFalse(pattern);
    }

    private static string[] ReadCommand(ProbeRun run)
    {
      return ReadCommand(run.LogPath);
    }

    private static string[] ReadCommand(string logPath)
    {
      string[] lines = File.ReadAllLines(logPath);
      int argsIndex = Array.IndexOf(lines, "ARGS");
      int settingsIndex = Array.IndexOf(lines, "SETTINGS", argsIndex + 1);
      argsIndex.ShouldBeGreaterThanOrEqualTo(0);
      settingsIndex.ShouldBeGreaterThan(argsIndex);
      return lines[(argsIndex + 1)..settingsIndex];
    }

    private static void AssertCommand(string[] actual, params string[] expected)
    {
      actual.Length.ShouldBe(expected.Length, string.Join(Environment.NewLine, actual));
      for (int index = 0; index < expected.Length; index++)
      {
        actual[index].ShouldBe(expected[index], "argument " + index.ToString(CultureInfo.InvariantCulture));
      }
    }

    private static async Task<string> EnsureMuxerHostAsync()
    {
      await BuildGate.WaitAsync();
      try
      {
        if (MuxerHostPath is not null && File.Exists(MuxerHostPath))
        {
          return MuxerHostPath;
        }

        string root = FindRepoRoot();
        string project = Path.Combine(
          root,
          "tests",
          "timewarp-jaribu",
          "coverage-muxer-host",
          "coverage-muxer-host.csproj");
        TimeWarp.Amuru.CommandOutput build = await TimeWarp.Amuru.Shell.Builder("dotnet")
          .WithArguments("build", project, "-c", "Release", "--nologo")
          .WithWorkingDirectory(root)
          .WithNoValidation()
          .WithTimeout(TimeSpan.FromMinutes(3))
          .CaptureAsync();
        build.ExitCode.ShouldBe(0, build.Combined);

        string built = Path.Combine(
          root,
          "tests",
          "timewarp-jaribu",
          "coverage-muxer-host",
          "bin",
          "Release",
          "net11.0",
          "coverage-muxer-host.dll");
        File.Exists(built).ShouldBeTrue(build.Combined);
        MuxerHostPath = built;
        return built;
      }
      finally
      {
        BuildGate.Release();
      }
    }

    private static string LinkFakeDotnet(string toolPath)
    {
      // A `dotnet` name next to the fake would steal the relaunch's `dotnet` lookup.
      string directory = Path.Combine(Path.GetTempPath(), "jaribu-fake-dotnet");
      System.IO.Directory.CreateDirectory(directory);
      string linkName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
      string link = Path.Combine(directory, linkName);
      if (File.Exists(link))
      {
        File.Delete(link);
      }

      try
      {
        File.CreateSymbolicLink(link, toolPath);
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        CopyFakeBesideDotnet(toolPath, directory, link);
      }

      return directory;
    }

    private static void CopyFakeBesideDotnet(string toolPath, string directory, string link)
    {
      string? sourceDirectory = Path.GetDirectoryName(toolPath);
      if (string.IsNullOrWhiteSpace(sourceDirectory))
      {
        throw new InvalidOperationException($"Coverage fake has no directory: '{toolPath}'.");
      }

      foreach (string file in System.IO.Directory.GetFiles(sourceDirectory))
      {
        File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), overwrite: true);
      }

      if (!File.Exists(link))
      {
        File.Copy(toolPath, link, overwrite: true);
      }
    }

    private static string FindDotnet()
    {
      string? path = Environment.GetEnvironmentVariable("PATH");
      if (string.IsNullOrEmpty(path))
      {
        throw new InvalidOperationException("PATH is empty.");
      }

      string fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
      foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
      {
        string candidate = Path.Combine(directory, fileName);
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }

      throw new InvalidOperationException("dotnet was not found on PATH.");
    }

    private static TimeWarp.Amuru.ShellBuilder IsolateCoverage(TimeWarp.Amuru.ShellBuilder builder, ProbeRun run, string? toolPath)
    {
      return builder
        .WithEnvironmentVariable(
          RunfileCoverageOptions.ToolVariableName,
          toolPath ?? string.Empty)
        .WithEnvironmentVariable("JARIBU_COVERAGE_FAKE_LOG", run.LogPath)
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "0")
        .WithEnvironmentVariable(RunfileCoverageOptions.VariableName, "0")
        .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_32", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_64", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_ARM64", "0")
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3));
    }

    private sealed class ProbeRun : IDisposable
    {
      private ProbeRun(string directory)
      {
        Directory = directory;
        LogPath = Path.Combine(directory, "launch.log");
      }

      public string Directory { get; }

      public string LogPath { get; }

      public static ProbeRun Create()
      {
        string directory = Path.Combine(Path.GetTempPath(), "jaribu-coverage-" + Guid.NewGuid().ToString("N"));
        System.IO.Directory.CreateDirectory(directory);
        return new ProbeRun(directory);
      }

      public void Dispose()
      {
        try
        {
          System.IO.Directory.Delete(Directory, recursive: true);
        }
        catch (IOException)
        {
          // The assertion already ran. A leftover temp directory is not a test failure.
        }
      }
    }
}
