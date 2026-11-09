#region Purpose
// Run the same test set the CI workflow runs.
#endregion
#region Design
// CiTestSuite runs the CI-safe runfile and the CI-safe MTP project.
// The validation project is not invoked here. Its intentional failures are
// checked by the out-of-process harness inside that suite.
// The root comes from RepoRoot.TryFind, the shared lookup.
// Handler stores Ct and Root as fields so private methods are zero-parameter.
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : IRequestHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string Root = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(TestCommand command, CancellationToken ct)
    {
      Ct = ct;

      if (!RepoRoot.TryFind(Terminal, out string root)) return Unit.Value;
      Root = root;
      if (!await TestAsync()) return Unit.Value;

      Terminal.WriteLine("\nTests completed successfully!".Green());
      return Unit.Value;
    }

    private async Task<bool> TestAsync()
    {
      Terminal.WriteLine("Running test suite...");
      try
      {
        await CiTestSuite.RunAsync(Root, Ct).ConfigureAwait(false);
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
