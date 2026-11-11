#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Proves RunAllTests relaunches under dotnet-coverage and fails clearly when the tool is missing.
#endregion

#region Design
// The probe is a separate runfile so a coverage child marker on this suite cannot
// suppress the relaunch under test. JARIBU_COVERAGE_TOOL points at coverage-tool-fake,
// which records the command and runs the runner after `--`.
// The suite process may already be instrumented; the probe launch clears that marker
// and the coverage profiler variables.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using TimeWarp.Amuru;

  [TestTag("Jaribu")]
  public class RunfileCoverage_Given_
  {
    private const int ProbeTimeoutMilliseconds = 180_000;

    private static readonly SemaphoreSlim BuildGate = new(1, 1);

    private static string? FakeToolPath;

    [ModuleInitializer]
    internal static void Register() => RegisterTests<RunfileCoverage_Given_>();

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task MissingTool_Should_NameTheInstallCommand()
    {
      using ProbeRun run = ProbeRun.Create();
      string missingTool = Path.Combine(run.Directory, "missing", "dotnet-coverage");
      CommandOutput output = await RunProbeAsync(run, "api", missingTool, probeExit: null, coverageVariable: "0");

      output.ExitCode.ShouldBe(1);
      output.Combined.ShouldContain("dotnet tool install -g dotnet-coverage");
      output.Combined.ShouldContain("dotnet tool install dotnet-coverage");
      File.Exists(Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName)).ShouldBeFalse();
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task InvalidModuleFilter_Should_FailBeforeLaunch()
    {
      using ProbeRun run = ProbeRun.Create();
      string missingTool = Path.Combine(run.Directory, "missing", "dotnet-coverage");
      CommandOutput output = await RunProbeAsync(
        run,
        "api",
        missingTool,
        probeExit: null,
        coverageVariable: "0",
        moduleFilter: "[");

      output.ExitCode.ShouldBe(1);
      output.Combined.ShouldContain("invalid regex");
      File.Exists(run.LogPath).ShouldBeFalse();
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task Relaunch_Should_CollectCoberturaOnceWithTheDefaultFilter()
    {
      using ProbeRun run = ProbeRun.Create();
      string tool = await EnsureFakeToolAsync();
      CommandOutput output = await RunProbeAsync(run, "api", tool, probeExit: null, coverageVariable: "0");

      output.ExitCode.ShouldBe(0);
      AssertOneLaunch(run);
      string settings = File.ReadAllText(Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName));
      foreach (string pattern in RunfileCoverageOptions.DefaultModuleExcludes)
      {
        settings.ShouldContain(pattern);
      }

      settings.ShouldContain("CollectFromChildProcesses");
      string log = File.ReadAllText(run.LogPath);
      log.ShouldContain("collect");
      log.ShouldContain("-f");
      log.ShouldContain("cobertura");
      log.ShouldContain("--");
      log.ShouldContain("dotnet");
      log.ShouldContain("runfile-coverage-probe.cs");
      log.ShouldContain(Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName));
      File.ReadAllText(Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName))
        .ShouldContain("<coverage");
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task ApiOptions_Should_OverrideResultsDirectoryAndModuleFilter()
    {
      using ProbeRun run = ProbeRun.Create();
      string tool = await EnsureFakeToolAsync();
      CommandOutput output = await RunProbeAsync(
        run,
        "api",
        tool,
        probeExit: null,
        coverageVariable: "0",
        moduleFilter: @"MyProduct\.dll");

      output.ExitCode.ShouldBe(0);
      string settings = File.ReadAllText(Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName));
      settings.ShouldContain(@"MyProduct\.dll");
      settings.ShouldNotContain(RunfileCoverageOptions.TestsModuleExclude);
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task Environment_Should_OptInAndPropagateTheChildExitCode()
    {
      using ProbeRun run = ProbeRun.Create();
      string tool = await EnsureFakeToolAsync();
      CommandOutput output = await RunProbeAsync(
        run,
        "env",
        tool,
        probeExit: "4",
        coverageVariable: "1",
        moduleFilter: @"Other\.dll",
        passFilterAsArgument: false,
        useResultsVariable: true);

      output.ExitCode.ShouldBe(4);
      AssertOneLaunch(run);
      string settings = File.ReadAllText(Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName));
      settings.ShouldContain(@"Other\.dll");
      File.ReadAllText(run.LogPath).ShouldContain(
        Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName));
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task UnsetVariable_Should_RunInProcess()
    {
      using ProbeRun run = ProbeRun.Create();
      string tool = await EnsureFakeToolAsync();
      CommandOutput output = await RunProbeAsync(run, "off", tool, probeExit: null, coverageVariable: "0");

      output.ExitCode.ShouldBe(0);
      File.Exists(run.LogPath).ShouldBeFalse();
      File.Exists(Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName)).ShouldBeFalse();
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
        CommandOutput build = await Shell.Builder("dotnet")
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
        return built;
      }
      finally
      {
        BuildGate.Release();
      }
    }

    private static Task<CommandOutput> RunProbeAsync(
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
      string probe = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "runfile-coverage-probe.cs");
      List<string> arguments = ["run", probe, "--", mode, run.Directory];
      if (passFilterAsArgument && moduleFilter is not null)
      {
        arguments.Add(moduleFilter);
      }

      ShellBuilder builder = Shell.Builder("dotnet")
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
        .WithEnvironmentVariable("JARIBU_COVERAGE_PROBE_EXIT", probeExit ?? "0")
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
}
