#region Purpose
// Proves the static relaunch builds the runner and collects without the profiler.
#endregion

#region Design
// JARIBU_COVERAGE_STATIC=1 is the path macOS arm64 and Linux arm take on their own.
// The build is real. The coverage tool is the fake, which records --include-files
// and --no-build. The dynamic command-shape tests set the variable to 0.
#endregion

namespace TestRunner_;

using System.Security.Cryptography;
using System.Xml.Linq;

public partial class RunfileCoverage_Given_
{
  [Timeout(ProbeTimeoutMilliseconds)]
  public static async Task StaticInstrumentation_Should_BuildAndRelaunchWithoutTheProfiler()
  {
    using ProbeRun run = ProbeRun.Create();
    string tool = await EnsureFakeToolAsync();
    TimeWarp.Amuru.CommandOutput output = await RunProbeAsync(
      run,
      "api",
      tool,
      probeExit: null,
      coverageVariable: "0",
      staticInstrumentation: true);

    output.ExitCode.ShouldBe(0, output.Combined);
    AssertOneLaunch(run);
    AssertActiveChild(run);
    string settingsPath = Path.Combine(run.Directory, RunfileCoverageOptions.SettingsFileName);
    XDocument document = XDocument.Load(settingsPath);
    ElementValue(document, "EnableDynamicManagedInstrumentation").ShouldBe("False");
    ElementValue(document, "EnableStaticManagedInstrumentation").ShouldBe("True");

    string[] command = ReadCommand(run);
    int separator = Array.IndexOf(command, "--");
    int artifacts = Array.IndexOf(command, "--artifacts-path");
    int noBuild = Array.IndexOf(command, "--no-build");
    separator.ShouldBeGreaterThan(0);
    artifacts.ShouldBeGreaterThan(separator);
    noBuild.ShouldBe(artifacts + 2);
    command[noBuild + 1].ShouldBe("api");
    string artifactsPath = command[artifacts + 1];
    bool sawProbe = false;
    bool sawProduct = false;
    for (int index = 0; index < separator - 1; index++)
    {
      if (command[index] != "--include-files")
      {
        continue;
      }

      string includeFile = command[index + 1];
      includeFile.StartsWith(artifactsPath, StringComparison.Ordinal).ShouldBeTrue(includeFile);
      string fileName = Path.GetFileName(includeFile);
      if (fileName == "runfile-coverage-probe.dll")
      {
        sawProbe = true;
      }

      if (fileName == "timewarp-jaribu.dll")
      {
        sawProduct = true;
      }
    }

    sawProbe.ShouldBeTrue(string.Join(Environment.NewLine, command));
    sawProduct.ShouldBeTrue(string.Join(Environment.NewLine, command));
  }

  [Timeout(ProbeTimeoutMilliseconds)]
  public static async Task StaticMuxerHost_Should_InstrumentACopyInsteadOfTheLoadedOutput()
  {
    using ProbeRun run = ProbeRun.Create();
    string tool = await EnsureFakeToolAsync();
    string host = await EnsureMuxerHostAsync();
    string dotnet = FindDotnet();
    string hostFull = Path.GetFullPath(host);
    TimeWarp.Amuru.CommandOutput output = await IsolateCoverage(
        TimeWarp.Amuru.Shell.Builder(dotnet)
          .WithArguments(host, run.Directory)
          .WithWorkingDirectory(FindRepoRoot()),
        run,
        tool,
        staticInstrumentation: true)
      .CaptureAsync();

    output.ExitCode.ShouldBe(0, output.Combined);
    AssertOneLaunch(run);
    string[] command = ReadCommand(run);
    int separator = Array.IndexOf(command, "--");
    separator.ShouldBeGreaterThan(0);
    command[separator + 1].ShouldBe(dotnet);
    string relaunch = command[separator + 2];
    Path.GetFileName(relaunch).ShouldBe(Path.GetFileName(hostFull));
    string copyDirectory = Path.GetDirectoryName(relaunch) ?? string.Empty;
    string loadedDirectory = Path.GetDirectoryName(hostFull) ?? string.Empty;
    copyDirectory.Contains("jaribu-runfile-coverage", StringComparison.Ordinal).ShouldBeTrue(relaunch);
    string.Equals(copyDirectory, loadedDirectory, StringComparison.Ordinal).ShouldBeFalse(relaunch);
    bool sawHost = false;
    bool sawProduct = false;
    for (int index = 0; index < separator - 1; index++)
    {
      if (command[index] != "--include-files")
      {
        continue;
      }

      string includeFile = command[index + 1];
      string? includeDirectory = Path.GetDirectoryName(includeFile);
      string.Equals(includeDirectory, copyDirectory, StringComparison.Ordinal).ShouldBeTrue(includeFile);
      string fileName = Path.GetFileName(includeFile);
      if (fileName == "coverage-muxer-host.dll")
      {
        sawHost = true;
      }

      if (fileName == "timewarp-jaribu.dll")
      {
        sawProduct = true;
      }
    }

    sawHost.ShouldBeTrue(string.Join(Environment.NewLine, command));
    sawProduct.ShouldBeTrue(string.Join(Environment.NewLine, command));
    foreach (string argument in command)
    {
      string.Equals(argument, hostFull, StringComparison.Ordinal).ShouldBeFalse(argument);
    }
  }

  [Timeout(300_000)]
  public static async Task StaticMuxerHost_Should_CollectFromACopyWithTheRealTool()
  {
    using ProbeRun run = ProbeRun.Create();
    string host = await EnsureMuxerHostAsync();
    string hostFull = Path.GetFullPath(host);
    string outputDirectory = Path.GetDirectoryName(hostFull) ?? string.Empty;
    string product = Path.Combine(outputDirectory, "timewarp-jaribu.dll");
    File.Exists(product).ShouldBeTrue(hostFull);
    string hostHash = FileHash(hostFull);
    string productHash = FileHash(product);
    TimeWarp.Amuru.CommandOutput output = await TimeWarp.Amuru.Shell.Builder(FindDotnet())
      .WithArguments(host, run.Directory)
      .WithWorkingDirectory(FindRepoRoot())
      .WithEnvironmentVariable(RunfileCoverageOptions.ToolVariableName, string.Empty)
      .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "0")
      .WithEnvironmentVariable(RunfileCoverageOptions.VariableName, "0")
      .WithEnvironmentVariable(RunfileCoverageOptions.StaticVariableName, "1")
      .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
      .WithEnvironmentVariable("CORECLR_PROFILER", "0")
      .WithEnvironmentVariable("CORECLR_PROFILER_PATH", "0")
      .WithEnvironmentVariable("CORECLR_PROFILER_PATH_32", "0")
      .WithEnvironmentVariable("CORECLR_PROFILER_PATH_64", "0")
      .WithEnvironmentVariable("CORECLR_PROFILER_PATH_ARM64", "0")
      .WithNoValidation()
      .WithTimeout(TimeSpan.FromMinutes(4))
      .CaptureAsync();

    output.ExitCode.ShouldBe(0, output.Combined);
    FileHash(hostFull).ShouldBe(hostHash, output.Combined);
    FileHash(product).ShouldBe(productHash, output.Combined);
    string reportPath = Path.Combine(run.Directory, RunfileCoverageOptions.CoberturaFileName);
    File.Exists(reportPath).ShouldBeTrue(output.Combined);
    XDocument document = XDocument.Load(reportPath);
    bool sawClass = false;
    foreach (XElement element in document.Descendants())
    {
      if (element.Name.LocalName == "class")
      {
        sawClass = true;
        break;
      }
    }

    sawClass.ShouldBeTrue(output.Combined);
  }

  private static string FileHash(string path)
  {
    return Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
  }

  private static string ElementValue(XDocument document, string name)
  {
    foreach (XElement element in document.Descendants())
    {
      if (element.Name.LocalName == name)
      {
        return element.Value;
      }
    }

    throw new InvalidOperationException("Missing settings element " + name + ".");
  }
}
