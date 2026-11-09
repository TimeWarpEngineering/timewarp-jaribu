#region Purpose
// Runs the cross-framework benchmark and writes the results markdown.
#endregion

#region Design
// Options filter measurement only. The generator still emits every workload so the
// in-process project, which compiles the whole Jaribu tree, stays consistent.
#endregion

namespace DevCli.Commands;

[NuruRoute("benchmark", Description = "Benchmark Jaribu, TimeWarp.Fixie, TUnit, and xUnit")]
[NuruRouteExample("benchmark", Description = "Every host and workload, three warm launches")]
[NuruRouteExample("benchmark --runs 3 --workloads startup --hosts jaribu-runfile")]
[NuruRouteExample("benchmark --skip-in-process")]
internal sealed class BenchmarkCommand : ICommand<Unit>
{
  [Option("runs", Description = "Warm launches after the cold one. Values below 1 use 3.")]
  public string Runs { get; set; } = "3";

  [Option("workloads", Description = "Comma-separated workload ids. Default is every workload.")]
  public string? Workloads { get; set; }

  [Option("hosts", Description = "Comma-separated host ids. Default is every host.")]
  public string? Hosts { get; set; }

  [Option("skip-in-process", Description = "Skip the BenchmarkDotNet Jaribu run.")]
  public bool SkipInProcess { get; set; }

  internal sealed class Handler : ICommandHandler<BenchmarkCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async Task<Unit> Handle(BenchmarkCommand command, CancellationToken ct)
    {
      string? repo = Git.FindRoot();
      if (repo is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Unit.Value;
      }

      try
      {
        await BenchmarkSuite.RunAsync(
          repo,
          Terminal,
          command.Runs,
          command.Workloads,
          command.Hosts,
          command.SkipInProcess,
          ct);
      }
      catch (InvalidOperationException ex)
      {
        Terminal.WriteErrorLine(ex.Message);
        Environment.ExitCode = 1;
      }

      return Unit.Value;
    }
  }
}
