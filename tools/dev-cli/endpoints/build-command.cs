#region Purpose
// Build all projects in the repository
#endregion
#region Design
// Discovers the repository root through RepoRoot.TryFind.
// Handler stores Command and Ct as fields so private methods are zero-parameter
// ReportResult handles verbose/error output consistently across steps
#endregion

namespace DevCli.Commands;

[NuruRoute("build", Description = "Build all projects in the repository")]
internal sealed class BuildCommand : ICommand<Unit>
{
  [Option("clean", "c", Description = "Clean before building")]
  public bool Clean { get; set; }

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler : IRequestHandler<BuildCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private BuildCommand Command = null!;
    private CancellationToken Ct;
    private string Root = null!;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(BuildCommand command, CancellationToken ct)
    {
      Command = command;
      Ct = ct;

      if (!RepoRoot.TryFind(Terminal, out string root)) return Unit.Value;
      Root = root;
      Terminal.WriteLine($"Building repository at {Root}...");
      if (!await CleanAsync()) return Unit.Value;
      if (!await BuildAsync()) return Unit.Value;

      Terminal.WriteLine("\nBuild completed successfully!".Green());
      return Unit.Value;
    }

    private async Task<bool> CleanAsync()
    {
      if (!Command.Clean) return true;

      Terminal.WriteLine("\nCleaning before build...");
      CommandOutput result = await DotNet.Clean()
        .WithWorkingDirectory(Root)
        .WithNoValidation()
        .CaptureAsync(Ct);

      return ReportResult(result, "Clean failed!");
    }

    private async Task<bool> BuildAsync()
    {
      Terminal.WriteLine("\nBuilding...");
      CommandOutput result = await DotNet.Build()
        .WithConfiguration("Release")
        .WithWorkingDirectory(Root)
        .WithNoValidation()
        .CaptureAsync(Ct);

      return ReportResult(result, "Build failed!");
    }

    private bool ReportResult(CommandOutput result, string failureMessage)
    {
      if (Command.Verbose)
        Terminal.WriteLine(result.Combined);

      if (!result.Success)
      {
        if (!Command.Verbose)
          Terminal.WriteErrorLine(result.Combined);
        Terminal.WriteErrorLine(failureMessage.Red());
        Environment.ExitCode = 1;
        return false;
      }

      return true;
    }
  }
}
