#region Purpose
// Runs the two test targets shared by dev test and the CI workflow.
#endregion

#region Design
// The ci-runner runfile and the CI-safe MTP project are one set.
// dev test and dev workflow both call RunAsync, so a local pass cannot skip MTP.
// The validation project is not a third target. Its intentional failures stay
// inside the out-of-process harness that the CI-safe suite executes.
// The CI runfile opts into RunAllTests coverage and writes
// artifacts/coverage/runfile/coverage.cobertura.xml. Its module filter keeps
// TimeWarp.Jaribu, which the default filter excludes, and still drops tests
// and MSBuild nodes. The MTP dotnet test passes
// coverage flags and writes artifacts/coverage/coverage.cobertura.xml.
// A relative --coverage-output is resolved from --results-directory.
// The runner project references Microsoft.Testing.Extensions.CodeCoverage so
// --coverage is a known option. DotNet.Test has no slot for those extension arguments.
#endregion

namespace DevCli.Commands;

internal static class CiTestSuite
{
  public static async Task RunAsync(string repoRoot, CancellationToken cancellationToken)
  {
    await RunCiRunnerAsync(repoRoot, cancellationToken).ConfigureAwait(false);
    await RunMtpRunnerAsync(repoRoot, cancellationToken).ConfigureAwait(false);
  }

  private static async Task RunCiRunnerAsync(string repoRoot, CancellationToken cancellationToken)
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
      .RunAsync(cancellationToken);
    if (exitCode != 0)
    {
      throw new InvalidOperationException("Tests failed!");
    }
  }

  private static async Task RunMtpRunnerAsync(string repoRoot, CancellationToken cancellationToken)
  {
    string mtpProject = Path.Combine(
      repoRoot,
      "tests",
      "timewarp-jaribu",
      "multi-file-runners",
      "mtp-runner",
      "timewarp-jaribu-tests-mtp.csproj");
    int exitCode = await Shell.Builder("dotnet")
      .WithArguments
      (
        "test",
        mtpProject,
        "--configuration",
        "Release",
        "--results-directory",
        "artifacts/coverage",
        "--coverage",
        "--coverage-output-format",
        "cobertura",
        "--coverage-output",
        "coverage.cobertura.xml"
      )
      .WithWorkingDirectory(repoRoot)
      .RunAsync(cancellationToken);
    if (exitCode != 0)
    {
      throw new InvalidOperationException("MTP tests failed!");
    }
  }
}
