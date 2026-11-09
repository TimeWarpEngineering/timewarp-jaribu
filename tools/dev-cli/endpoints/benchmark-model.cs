#region Purpose
// Records one host launch and the workload case counts the report divides by.
#endregion

namespace DevCli.Commands;

internal enum BenchHostKind
{
  Runfile,
  DotnetTest,
  DotnetRun
}

internal sealed record WorkloadInfo(string Id, string Kind, int Cases);

internal sealed record BenchHost(string Id, BenchHostKind Kind, string ProjectRelative);

internal sealed class TimedSample
{
  public required double WallMs { get; init; }
  public double? TtftMs { get; init; }
  public long? AllocatedBytes { get; init; }
  public long PeakBytes { get; init; }
  public string? Runtime { get; init; }
}

internal sealed class WorkloadResult
{
  public required string Host { get; init; }
  public required string Workload { get; init; }
  public required int Cases { get; init; }
  public required double BuildMs { get; init; }
  public required TimedSample Cold { get; init; }
  public required IReadOnlyList<TimedSample> Warm { get; init; }
}

internal sealed class InProcessRow
{
  public required string Name { get; init; }
  public required double MeanMs { get; init; }
  public long? AllocatedBytes { get; init; }
}

internal static class BenchHosts
{
  internal static readonly string[] WorkloadOrder =
  [
    "startup",
    "volume-1k",
    "volume-10k",
    "parameterized",
    "async-1k",
    "fixtures"
  ];

  internal static readonly BenchHost[] All =
  [
    new("jaribu-runfile", BenchHostKind.Runfile, ""),
    new("jaribu-mtp", BenchHostKind.DotnetTest, "benchmarks/jaribu/mtp/jaribu-mtp.csproj"),
    new("fixie", BenchHostKind.DotnetRun, "benchmarks/fixie/fixie-bench.csproj"),
    new("tunit", BenchHostKind.DotnetTest, "benchmarks/tunit/tunit-bench.csproj"),
    new("xunit", BenchHostKind.DotnetTest, "benchmarks/xunit/xunit-bench.csproj")
  ];

  internal static readonly Dictionary<string, string> InProcessFilters = new(StringComparer.Ordinal)
  {
    ["startup"] = "*Startup*",
    ["volume-1k"] = "*Volume1K*",
    ["volume-10k"] = "*Volume10K*",
    ["parameterized"] = "*Parameterized*",
    ["async-1k"] = "*Async1K*",
    ["fixtures"] = "*Fixtures*"
  };
}
