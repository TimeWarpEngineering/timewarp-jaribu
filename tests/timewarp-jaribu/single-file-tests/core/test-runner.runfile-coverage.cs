#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.runfile-coverage.launch.cs
#:include test-runner.runfile-coverage.manifest.cs

#region Purpose
// Proves RunAllTests relaunches under dotnet-coverage and fails clearly when the tool is missing.
#endregion

#region Design
// The probe is a separate runfile so a coverage child marker on this suite cannot
// suppress the relaunch under test. JARIBU_COVERAGE_TOOL points at coverage-tool-fake,
// which records the command and runs the runner after `--`.
// The suite process may already be instrumented; the probe launch clears that marker
// and the coverage profiler variables.
// Manifest tests hide PATH and ~/.dotnet/tools, then point `dotnet` at the same fake.
// coverage-muxer-host is a dll launched by the dotnet muxer.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Globalization;
  using System.Text.RegularExpressions;
  using System.Xml.Linq;
  using TimeWarp.Amuru;

  [TestTag("Jaribu")]
  public partial class RunfileCoverage_Given_
  {
    private const int ProbeTimeoutMilliseconds = 180_000;

    private static readonly SemaphoreSlim BuildGate = new(1, 1);

    private static string? FakeToolPath;

    private static string? FakeDotnetDirectory;

    private static string? MuxerHostPath;

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
      AssertActiveChild(run);
      string settingsPath = Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName);
      string settings = File.ReadAllText(settingsPath);
      foreach (string pattern in RunfileCoverageOptions.DefaultModuleExcludes)
      {
        settings.ShouldContain(pattern);
      }

      settings.ShouldContain("CollectFromChildProcesses");
      AssertRepositorySources(settingsPath);
      string outputPath = Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName);
      AssertCommand(
        ReadCommand(run),
        "collect",
        "-f",
        "cobertura",
        "-o",
        Path.GetFullPath(outputPath),
        "-s",
        Path.GetFullPath(settingsPath),
        "--",
        "dotnet",
        ProbePath(),
        "api",
        run.Directory);
      File.ReadAllText(outputPath).ShouldContain("<coverage");
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
      string settingsPath = Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName);
      string settings = File.ReadAllText(settingsPath);
      settings.ShouldContain(@"MyProduct\.dll");
      settings.ShouldNotContain(RunfileCoverageOptions.TestsModuleExclude);
      AssertRepositorySources(settingsPath);
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
      AssertActiveChild(run);
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
  }
}
