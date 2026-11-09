#region Purpose
// Run the same test set the CI workflow runs.
#endregion
#region Design
// CiTestSuite runs the CI-safe runfile and the CI-safe MTP project.
// The validation project is not invoked here. Its intentional failures are
// checked by the out-of-process harness inside that suite.
// Handler stores Ct and RepoRoot as fields so private methods are zero-parameter.
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

    public async Task<Unit> Handle(TestCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!FindRepoRoot()) return Unit.Value;
      if (!await TestAsync()) return Unit.Value;

      Terminal.WriteLine("\nTests completed successfully!".Green());
      return Unit.Value;
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
      try
      {
        await CiTestSuite.RunAsync(RepoRoot, Ct).ConfigureAwait(false);
      }
      catch (InvalidOperationException ex)
      {
        Terminal.WriteErrorLine(ex.Message.Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }
  }
}
