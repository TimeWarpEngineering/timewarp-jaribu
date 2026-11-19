#region Purpose
// Runs the two test targets shared by dev test and the CI workflow.
#endregion

#region Design
// The ci-runner runfile and the CI-safe MTP project are one set.
// dev test and dev workflow both call RunAsync, so a local pass cannot skip MTP.
// JARIBU_FILTER_TAG is inherited. The workflow leaves it blank on ubuntu, sets
// macos on the macOS leg, and sets windows on the Windows leg. A blank value
// is not a filter. Both targets read the same variable.
// The validation project is not a third target. Its intentional failures stay
// inside the out-of-process harness that the CI-safe suite executes.
// The CI runfile opts into RunAllTests coverage and writes
// artifacts/coverage/runfile/coverage.cobertura.xml. Its module filter keeps
// TimeWarp.Jaribu, which the default filter excludes, and still drops tests
// and MSBuild nodes. The project include drops dependency assemblies.
// The MTP dotnet test passes
// coverage flags and writes artifacts/coverage/coverage.cobertura.xml.
// A relative --coverage-output is resolved from --results-directory.
// The runner project references Microsoft.Testing.Extensions.CodeCoverage so
// --coverage is a known option. DotNet.Test sets the project, configuration, and
// results directory, and WithArguments appends the coverage extension tokens after
// them, so the command line is unchanged:
// `test <project> --configuration Release --results-directory artifacts/coverage --coverage ...`.
// Amuru (3.0.0-beta.9+) sets TRACEPARENT on each child from Activity.Current, the
// current command span. The test process parents jaribu.class and jaribu.test
// spans to that span.
// Both legs set JARIBU_DURATIONS_PATH to the repository artifacts/jaribu directory
// with a trailing separator. The writer puts durations-<utc>.json there, including
// when the test host's working directory is not the repo root. dev slow-tests
// reads that same directory.
#endregion

namespace DevCli.Commands;

internal static class CiTestSuite
{
  public static async Task RunAsync(string repoRoot, CancellationToken cancellationToken)
  {
    string durationsDirectory = DurationsDirectory(repoRoot);
    await RunCiRunnerAsync(repoRoot, durationsDirectory, cancellationToken).ConfigureAwait(false);
    await RunMtpRunnerAsync(repoRoot, durationsDirectory, cancellationToken).ConfigureAwait(false);
  }

  private static string DurationsDirectory(string repoRoot)
  {
    string directory = Path.Combine(repoRoot, "artifacts", "jaribu");
    Directory.CreateDirectory(directory);
    return directory + Path.DirectorySeparatorChar;
  }

  private static async Task RunCiRunnerAsync(
    string repoRoot,
    string durationsDirectory,
    CancellationToken cancellationToken)
  {
    string ciTestRunner = Path.Combine(
      repoRoot,
      "tests",
      "timewarp-jaribu",
      "multi-file-runners",
      "ci-runner",
      "run-ci-tests.cs");
    int exitCode = await DotNet.Run()
      .WithFile(ciTestRunner)
      .WithProperty("ExperimentalFileBasedProgramEnableTransitiveDirectives", "true")
      .WithWorkingDirectory(repoRoot)
      .WithEnvironmentVariable("JARIBU_DURATIONS_PATH", durationsDirectory)
      .RunAsync(cancellationToken);
    if (exitCode != 0)
    {
      throw new InvalidOperationException("Tests failed!");
    }
  }

  private static async Task RunMtpRunnerAsync(
    string repoRoot,
    string durationsDirectory,
    CancellationToken cancellationToken)
  {
    string mtpProject = Path.Combine(
      repoRoot,
      "tests",
      "timewarp-jaribu",
      "multi-file-runners",
      "mtp-runner",
      "timewarp-jaribu-tests-mtp.csproj");
    int exitCode = await DotNet.Test()
      .WithProject(mtpProject)
      .WithConfiguration("Release")
      .WithResultsDirectory("artifacts/coverage")
      .WithArguments
      (
        "--coverage",
        "--coverage-output-format",
        "cobertura",
        "--coverage-output",
        "coverage.cobertura.xml"
      )
      .WithWorkingDirectory(repoRoot)
      .WithEnvironmentVariable("JARIBU_DURATIONS_PATH", durationsDirectory)
      .RunAsync(cancellationToken);
    if (exitCode != 0)
    {
      throw new InvalidOperationException("MTP tests failed!");
    }
  }
}
