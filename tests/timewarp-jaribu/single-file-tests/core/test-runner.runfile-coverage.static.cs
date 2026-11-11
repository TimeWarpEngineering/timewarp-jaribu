#region Purpose
// Proves the static relaunch builds the runner and collects without the profiler.
#endregion

#region Design
// JARIBU_COVERAGE_STATIC=1 is the path macOS arm64 and Linux arm take on their own.
// The build is real. The coverage tool is the fake, which records --include-files
// and --no-build. The dynamic command-shape tests set the variable to 0.
#endregion

namespace TestRunner_;

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
