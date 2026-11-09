#region Purpose
// Run the test suite
#endregion
#region Design
// Executes the CI-safe runfile test runner because the MTP validation projects
// intentionally contain failing tests for adapter behavior verification.
// Handler stores Ct and Root as fields so private methods are zero-parameter
// The root comes from RepoRoot.TryFind.
#endregion

namespace DevCli.Commands;

[NuruRoute("test", Description = "Run the test suite")]
internal sealed class TestCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<TestCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private CancellationToken Ct;
    private string Root = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(TestCommand command, CancellationToken ct)
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
      string ciTestRunner = Path.Combine(Root, "tests", "timewarp-jaribu", "multi-file-runners", "ci-runner", "run-ci-tests.cs");

      // DotNetRunBuilder lacks RunAndCaptureAsync; Shell.Builder is needed to stream
      // live output and capture failure details in one call.
      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments
        (
          "run",
          ciTestRunner,
          "/p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true"
        )
        .WithWorkingDirectory(Root)
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
