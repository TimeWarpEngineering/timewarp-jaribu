#region Purpose
// Runs the Jaribu BenchmarkDotNet project and reads mean time and allocated bytes.
#endregion

#region Design
// Statistics.Mean in the full JSON export is nanoseconds. One unfiltered run covers
// every workload. A subset runs once per workload so the class filter stays a single pattern.
// The JSON file is parsed before the next run overwrites it.
// Artifacts stay under the project so the report is not a repo-root file from another tool.
// dotnet run forwards --nologo into BenchmarkDotNet, which rejects that option, so this launch passes none.
#endregion

using System.Globalization;
using System.Text.Json;

namespace DevCli.Commands;

internal static class BenchmarkInProcess
{
  internal static async Task<IReadOnlyList<InProcessRow>> RunAsync(
    string repo,
    IReadOnlyList<string> workloadIds,
    CancellationToken cancellationToken)
  {
    string projectDirectory = Path.Combine(repo, "benchmarks", "jaribu", "in-process");
    string project = Path.Combine(projectDirectory, "jaribu-in-process.csproj");
    bool all = workloadIds.Count == BenchHosts.InProcessFilters.Count;
    List<InProcessRow> rows = [];
    if (all)
    {
      rows.AddRange(await RunFilterAsync(repo, project, projectDirectory, filter: null, cancellationToken));
      return rows;
    }

    foreach (string workload in workloadIds)
    {
      rows.AddRange(await RunFilterAsync(
        repo,
        project,
        projectDirectory,
        BenchHosts.InProcessFilters[workload],
        cancellationToken));
    }

    return rows;
  }

  private static async Task<IReadOnlyList<InProcessRow>> RunFilterAsync(
    string repo,
    string project,
    string projectDirectory,
    string? filter,
    CancellationToken cancellationToken)
  {
    List<string> arguments =
    [
      "run",
      "--project", project,
      "-c", "Release",
      "--",
      "--artifacts",
      Path.Combine(projectDirectory, "BenchmarkDotNet.Artifacts")
    ];
    if (filter is not null)
    {
      arguments.Add("--filter");
      arguments.Add(filter);
    }

    CommandOutput output = await Shell.Builder("dotnet")
      .WithArguments([.. arguments])
      .WithWorkingDirectory(repo)
      .WithNoValidation()
      .WithTimeout(TimeSpan.FromMinutes(45))
      .WithEnvironmentVariable("DOTNET_NOLOGO", "1")
      .WithEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1")
      .CaptureAsync(cancellationToken);
    if (!output.Success)
    {
      string[] lines = output.GetLines();
      int start = Math.Max(0, lines.Length - 40);
      throw new InvalidOperationException(
        "In-process benchmark failed." + Environment.NewLine + string.Join(Environment.NewLine, lines[start..]));
    }

    string? json = NewestReport(Path.Combine(projectDirectory, "BenchmarkDotNet.Artifacts"), marker: null)
      ?? NewestReport(Path.Combine(repo, "BenchmarkDotNet.Artifacts"), "jaribu-in-process");

    if (json is null)
    {
      throw new InvalidOperationException("BenchmarkDotNet did not write a report-full JSON file.");
    }

    return Parse(json);
  }

  private static string? NewestReport(string directory, string? marker)
  {
    if (!Directory.Exists(directory))
    {
      return null;
    }

    string? newest = null;
    DateTime newestTime = DateTime.MinValue;
    foreach (string path in Directory.GetFiles(directory, "*-report-full.json", SearchOption.AllDirectories))
    {
      if (marker is not null && !path.Contains(marker, StringComparison.Ordinal))
      {
        continue;
      }

      DateTime written = File.GetLastWriteTimeUtc(path);
      if (newest is null || written > newestTime)
      {
        newest = path;
        newestTime = written;
      }
    }

    return newest;
  }

  private static List<InProcessRow> Parse(string path)
  {
    using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
    if (!document.RootElement.TryGetProperty("Benchmarks", out JsonElement benchmarks))
    {
      throw new InvalidOperationException("BenchmarkDotNet JSON has no Benchmarks array: " + path);
    }

    List<InProcessRow> rows = [];
    foreach (JsonElement item in benchmarks.EnumerateArray())
    {
      string name = ReadName(item);
      if (!item.TryGetProperty("Statistics", out JsonElement statistics)
        || !statistics.TryGetProperty("Mean", out JsonElement mean))
      {
        continue;
      }

      long? allocated = null;
      if (item.TryGetProperty("Memory", out JsonElement memory)
        && memory.TryGetProperty("BytesAllocatedPerOperation", out JsonElement bytes)
        && bytes.TryGetInt64(out long value))
      {
        allocated = value;
      }

      rows.Add(new InProcessRow
      {
        Name = name,
        MeanMs = mean.GetDouble() / 1_000_000d,
        AllocatedBytes = allocated
      });
    }

    if (rows.Count == 0)
    {
      throw new InvalidOperationException("BenchmarkDotNet JSON contained no measured rows: " + path);
    }

    return rows;
  }

  private static string ReadName(JsonElement item)
  {
    if (item.TryGetProperty("DisplayInfo", out JsonElement display) && display.GetString() is string displayName)
    {
      return displayName;
    }

    if (item.TryGetProperty("FullName", out JsonElement full) && full.GetString() is string fullName)
    {
      return fullName;
    }

    return "unknown";
  }
}
