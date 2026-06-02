#region Purpose
// Run the test suite
#endregion
#region Design
// Executes the CI-safe runfile test runner because the MTP validation projects
// intentionally contain failing tests for adapter behavior verification.
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string RepoRoot = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Value;
      if (!await TestAsync()) return Value;

      Terminal.WriteLine("\nTests completed successfully!".Green());
      return Value;
    }

    private bool FindRepoRoot()
    {
      string? root = Git.FindRoot();
      if (root is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return false;
      }
      RepoRoot = root;
      return true;
    }

    private async Task<bool> TestAsync()
    {
      Terminal.WriteLine("Running test suite...");
      string ciTestRunner = Path.Combine(RepoRoot, "tests", "timewarp-jaribu", "multi-file-runners", "ci-runner", "run-ci-tests.cs");

      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments
        (
          "run",
          ciTestRunner,
          "/p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true"
        )
        .WithWorkingDirectory(RepoRoot)
        .WithNoValidation()
        .RunAndCaptureAsync(Ct);

      if (!result.Success)
      {
        Terminal.WriteErrorLine(result.Combined);
        Terminal.WriteErrorLine("Tests failed!".Red());
        Environment.ExitCode = 1;
        return false;
      }
      return true;
    }
  }
}
