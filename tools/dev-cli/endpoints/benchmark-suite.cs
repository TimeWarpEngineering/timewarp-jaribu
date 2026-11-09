#region Purpose
// Generates every workload, times the selected hosts, and writes the report.
#endregion

#region Design
// Builds are not part of cold or warm. A runfile's untimed first launch is the build.
// In-process timing is Jaribu only. An unknown host or workload fails before any launch.
#endregion

using System.Globalization;
using System.Text;

namespace DevCli.Commands;

internal static class BenchmarkSuite
{
  private static readonly string[] PackageNames =
  [
    "BenchmarkDotNet",
    "Fixie",
    "Fixie.TestAdapter",
    "Microsoft.Testing.Platform",
    "TimeWarp.Fixie",
    "TUnit",
    "xunit.v3.mtp-v2"
  ];

  internal static async Task RunAsync(
    string repo,
    ITerminal terminal,
    string runsText,
    string? workloadCsv,
    string? hostCsv,
    bool skipInProcess,
    CancellationToken cancellationToken)
  {
    int warmRuns = ParseRuns(runsText);
    string[] workloadIds = SelectWorkloads(workloadCsv);
    BenchHost[] hosts = SelectHosts(hostCsv);

    terminal.WriteLine("Generating workloads...");
    await GenerateAsync(repo, cancellationToken);
    Dictionary<string, int> cases = ReadCases(repo);
    string specId = ReadSpecId(repo);
    foreach (string workload in workloadIds)
    {
      if (!cases.ContainsKey(workload))
      {
        throw new InvalidOperationException("Manifest has no workload '" + workload + "'.");
      }
    }

    List<WorkloadResult> results = [];
    foreach (BenchHost host in hosts)
    {
      foreach (string workload in workloadIds)
      {
        terminal.WriteLine("Measuring " + host.Id + " " + workload + "...");
        double build = host.Kind == BenchHostKind.Runfile
          ? await BenchmarkMeasure.WarmRunfileCacheAsync(repo, workload, cancellationToken)
          : await BenchmarkMeasure.BuildProjectAsync(repo, host, workload, cancellationToken);
        terminal.WriteLine("  cold");
        TimedSample cold = await BenchmarkMeasure.LaunchAsync(repo, host, workload, cancellationToken);
        List<TimedSample> warm = [];
        for (int index = 0; index < warmRuns; index++)
        {
          terminal.WriteLine("  warm " + (index + 1).ToString(CultureInfo.InvariantCulture));
          warm.Add(await BenchmarkMeasure.LaunchAsync(repo, host, workload, cancellationToken));
        }

        results.Add(new WorkloadResult
        {
          Host = host.Id,
          Workload = workload,
          Cases = cases[workload],
          BuildMs = build,
          Cold = cold,
          Warm = warm
        });
      }
    }

    List<InProcessRow> inProcess = [];
    if (!skipInProcess)
    {
      terminal.WriteLine("Measuring Jaribu in-process...");
      inProcess.AddRange(await BenchmarkInProcess.RunAsync(repo, workloadIds, cancellationToken));
    }

    string sha = await CaptureTextAsync(repo, "git", ["rev-parse", "HEAD"], cancellationToken);
    string shortSha = await CaptureTextAsync(repo, "git", ["rev-parse", "--short=12", "HEAD"], cancellationToken);
    string sdk = await CaptureTextAsync(repo, "dotnet", ["--version"], cancellationToken);
    string path = BenchmarkReport.Write(
      repo,
      sha,
      shortSha,
      sdk,
      JoinRuntimes(results),
      specId,
      warmRuns,
      ReadVersions(repo),
      results,
      inProcess);
    terminal.WriteLine("Wrote " + path);
    terminal.WriteLine("Wrote " + Path.Combine(repo, "benchmarks", "results", "latest.md"));
  }

  private static string? JoinRuntimes(IReadOnlyList<WorkloadResult> results)
  {
    List<string> values = [];
    foreach (WorkloadResult result in results)
    {
      AddRuntime(values, result.Cold.Runtime);
      foreach (TimedSample sample in result.Warm)
      {
        AddRuntime(values, sample.Runtime);
      }
    }

    return values.Count == 0 ? null : string.Join("; ", values);
  }

  private static void AddRuntime(List<string> values, string? runtime)
  {
    if (string.IsNullOrWhiteSpace(runtime) || values.Contains(runtime, StringComparer.Ordinal))
    {
      return;
    }

    values.Add(runtime);
  }

  private static int ParseRuns(string text)
  {
    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int runs))
    {
      throw new InvalidOperationException("--runs must be an integer. Received '" + text + "'.");
    }

    return runs <= 0 ? 3 : runs;
  }

  private static string[] SelectWorkloads(string? csv)
  {
    if (string.IsNullOrWhiteSpace(csv))
    {
      return BenchHosts.WorkloadOrder;
    }

    string[] requested = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    List<string> selected = [];
    foreach (string id in requested)
    {
      if (!BenchHosts.WorkloadOrder.Contains(id, StringComparer.Ordinal))
      {
        throw new InvalidOperationException(
          "Unknown workload '" + id + "'. Expected " + string.Join(", ", BenchHosts.WorkloadOrder) + ".");
      }

      if (selected.Contains(id, StringComparer.Ordinal))
      {
        throw new InvalidOperationException("Workload '" + id + "' was listed twice.");
      }

      selected.Add(id);
    }

    if (selected.Count == 0)
    {
      throw new InvalidOperationException("--workloads did not name a workload.");
    }

    return [.. selected];
  }

  private static BenchHost[] SelectHosts(string? csv)
  {
    if (string.IsNullOrWhiteSpace(csv))
    {
      return BenchHosts.All;
    }

    string[] requested = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    List<BenchHost> selected = [];
    foreach (string id in requested)
    {
      BenchHost? host = null;
      foreach (BenchHost candidate in BenchHosts.All)
      {
        if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
        {
          host = candidate;
          break;
        }
      }

      if (host is null)
      {
        throw new InvalidOperationException(
          "Unknown host '" + id + "'. Expected "
          + string.Join(", ", BenchHosts.All.Select(candidate => candidate.Id)) + ".");
      }

      foreach (BenchHost already in selected)
      {
        if (string.Equals(already.Id, id, StringComparison.Ordinal))
        {
          throw new InvalidOperationException("Host '" + id + "' was listed twice.");
        }
      }

      selected.Add(host);
    }

    if (selected.Count == 0)
    {
      throw new InvalidOperationException("--hosts did not name a host.");
    }

    return [.. selected];
  }

  private static async Task GenerateAsync(string repo, CancellationToken cancellationToken)
  {
    string generator = Path.Combine(repo, "benchmarks", "generator", "generate-workloads.cs");
    CommandOutput output = await Shell.Builder("dotnet")
      .WithArguments("run", generator, "--nologo", "--", "--repo", repo)
      .WithWorkingDirectory(repo)
      .WithNoValidation()
      .WithTimeout(TimeSpan.FromMinutes(10))
      .WithEnvironmentVariable("DOTNET_NOLOGO", "1")
      .WithEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
      .CaptureAsync(cancellationToken);
    if (!output.Success)
    {
      throw new InvalidOperationException(Tail("Workload generator failed", output));
    }
  }

  private static Dictionary<string, int> ReadCases(string repo)
  {
    Dictionary<string, int> cases = new(StringComparer.Ordinal);
    foreach (string line in File.ReadAllLines(ManifestPath(repo)))
    {
      if (!line.StartsWith("workload=", StringComparison.Ordinal))
      {
        continue;
      }

      string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      string id = parts[0]["workload=".Length..];
      foreach (string part in parts)
      {
        if (part.StartsWith("cases=", StringComparison.Ordinal)
          && int.TryParse(
            part["cases=".Length..],
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int count))
        {
          cases[id] = count;
        }
      }
    }

    return cases;
  }

  private static string ReadSpecId(string repo)
  {
    foreach (string line in File.ReadAllLines(ManifestPath(repo)))
    {
      if (line.StartsWith("spec=", StringComparison.Ordinal))
      {
        return line["spec=".Length..].Trim();
      }
    }

    return "unknown";
  }

  private static string ManifestPath(string repo)
    => Path.Combine(repo, "benchmarks", "generated", "manifest.txt");

  private static Dictionary<string, string> ReadVersions(string repo)
  {
    string packages = File.ReadAllText(Path.Combine(repo, "Directory.Packages.props"));
    Dictionary<string, string> versions = new(StringComparer.Ordinal);
    foreach (string name in PackageNames)
    {
      versions[name] = ReadAttribute(packages, "Include=\"" + name + "\"", "Version");
    }

    string product = File.ReadAllText(Path.Combine(repo, "source", "Directory.Build.props"));
    versions["Jaribu"] = ReadElement(product, "Version");
    return versions;
  }

  private static string ReadAttribute(string text, string marker, string attribute)
  {
    int index = text.IndexOf(marker, StringComparison.Ordinal);
    if (index < 0)
    {
      throw new InvalidOperationException("Package pin not found for " + marker + ".");
    }

    string token = attribute + "=\"";
    int start = text.IndexOf(token, index, StringComparison.Ordinal);
    if (start < 0)
    {
      throw new InvalidOperationException("Attribute " + attribute + " not found near " + marker + ".");
    }

    start += token.Length;
    int end = text.IndexOf('"', start, StringComparison.Ordinal);
    return text[start..end];
  }

  private static string ReadElement(string text, string name)
  {
    string open = "<" + name + ">";
    string close = "</" + name + ">";
    int start = text.IndexOf(open, StringComparison.Ordinal);
    int end = text.IndexOf(close, StringComparison.Ordinal);
    if (start < 0 || end < 0 || end < start)
    {
      throw new InvalidOperationException("Element " + name + " was not found.");
    }

    return text[(start + open.Length)..end].Trim();
  }

  private static async Task<string> CaptureTextAsync(
    string repo,
    string file,
    string[] arguments,
    CancellationToken cancellationToken)
  {
    CommandOutput output = await Shell.Builder(file)
      .WithArguments(arguments)
      .WithWorkingDirectory(repo)
      .WithNoValidation()
      .WithTimeout(TimeSpan.FromMinutes(2))
      .CaptureAsync(cancellationToken);
    if (!output.Success)
    {
      throw new InvalidOperationException(Tail(file + " failed", output));
    }

    return output.Stdout.Trim();
  }

  private static string Tail(string heading, CommandOutput output)
  {
    string[] lines = output.GetLines();
    int start = Math.Max(0, lines.Length - 40);
    StringBuilder builder = new();
    builder.Append(heading);
    builder.Append(" (exit ");
    builder.Append(output.ExitCode);
    builder.AppendLine(").");
    for (int index = start; index < lines.Length; index++)
    {
      builder.AppendLine(lines[index]);
    }

    return builder.ToString();
  }
}
