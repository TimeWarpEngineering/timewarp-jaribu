#region Purpose
// Verifies that code samples in the repository compile
#endregion
#region Design
// samples/ is optional. A missing directory, or one with nothing to compile, exits non-zero.
// The command reports success only after dotnet build of a samples solution, project, or script exits 0.
#endregion

namespace DevCli.Commands;

[NuruRoute("verify-samples", Description = "Verify code samples compile")]
internal sealed class VerifySamplesCommand : ICommand<Unit>
{
  internal sealed class Handler : ICommandHandler<VerifySamplesCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(VerifySamplesCommand command, CancellationToken ct)
    {
      _ = command;
      ct.ThrowIfCancellationRequested();

      string? repoRoot = Git.FindRoot();
      if (repoRoot is null)
      {
        Terminal.WriteErrorLine("Error: could not find repository root.");
        Environment.ExitCode = 1;
        return Value;
      }

      string samplesDirectory = Path.Combine(repoRoot, "samples");
      if (!Directory.Exists(samplesDirectory))
      {
        Terminal.WriteErrorLine("verify-samples: no samples/ directory exists.");
        Environment.ExitCode = 1;
        return Value;
      }

      string[] targets = FindCompileTargets(samplesDirectory);
      if (targets.Length == 0)
      {
        Terminal.WriteErrorLine("verify-samples: samples/ has nothing to compile.");
        Environment.ExitCode = 1;
        return Value;
      }

      Terminal.WriteLine($"Verifying {targets.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)} sample target(s)...");
      foreach (string target in targets)
      {
        ct.ThrowIfCancellationRequested();
        Terminal.WriteLine($"  Building {Path.GetRelativePath(repoRoot, target)}");
        int exitCode = await DotNet.Build(target)
          .WithConfiguration("Release")
          .WithWorkingDirectory(repoRoot)
          .RunAsync(ct);
        if (exitCode != 0)
        {
          Terminal.WriteErrorLine($"verify-samples: build failed for {target}");
          Environment.ExitCode = 1;
          return Value;
        }
      }

      Terminal.WriteLine("Samples verified successfully!");
      return Value;
    }

    private static string[] FindCompileTargets(string samplesDirectory)
    {
      string[] solutions = Directory.GetFiles(samplesDirectory, "*.slnx", SearchOption.AllDirectories);
      if (solutions.Length > 0)
      {
        return solutions;
      }

      string[] projects = Directory.GetFiles(samplesDirectory, "*.csproj", SearchOption.AllDirectories);
      if (projects.Length > 0)
      {
        return projects;
      }

      return
      [
        .. Directory.GetFiles(samplesDirectory, "*.cs", SearchOption.AllDirectories)
          .Where(path => !IsUnderBuildOutput(path))
      ];
    }

    private static bool IsUnderBuildOutput(string path)
    {
      return path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }
  }
}
