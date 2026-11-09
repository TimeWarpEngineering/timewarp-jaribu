#region Purpose
// Writes benchmarks/results/<date>-<sha>.md and the latest.md copy.
#endregion

#region Design
// Warm median is the middle sorted sample (the mean of the two middle samples when the
// count is even). Per-test mean divides that median by the case count. Marginal per-test
// subtracts the 1,000-case warm median from the 10,000-case warm median and divides by 9,000.
#endregion

using System.Globalization;
using System.Text;

namespace DevCli.Commands;

internal static class BenchmarkReport
{
  internal static string Write(
    string repo,
    string sha,
    string shortSha,
    string sdk,
    string specId,
    int warmRuns,
    IReadOnlyDictionary<string, string> versions,
    IReadOnlyList<WorkloadResult> results,
    IReadOnlyList<InProcessRow> inProcess)
  {
    string date = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    StringBuilder builder = new();
    builder.AppendLine("# Benchmark results");
    builder.AppendLine();
    builder.AppendLine("Numbers from one serial run. Build time is not included in cold or warm.");
    builder.AppendLine("The measured commit is the commit that was checked out while the processes ran.");
    builder.AppendLine();
    builder.AppendLine("- Measured at: `" + sha + "`");
    builder.AppendLine("- Date (UTC): " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "Z");
    builder.AppendLine("- Spec: " + specId);
    builder.AppendLine("- SDK: " + sdk);
    builder.AppendLine("- OS: " + RuntimeInformation.OSDescription);
    builder.AppendLine("- Processors: " + Environment.ProcessorCount.ToString(CultureInfo.InvariantCulture));
    builder.AppendLine("- Warm launches: " + warmRuns.ToString(CultureInfo.InvariantCulture));
    builder.AppendLine();
    builder.AppendLine("## Versions");
    builder.AppendLine();
    foreach (KeyValuePair<string, string> pair in versions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
    {
      builder.AppendLine("- " + pair.Key + " " + pair.Value);
    }

    AppendWallClock(builder, results);
    AppendResources(builder, results);
    AppendMarginal(builder, results);
    AppendInProcess(builder, inProcess);

    string text = builder.ToString();
    string directory = Path.Combine(repo, "benchmarks", "results");
    Directory.CreateDirectory(directory);
    string path = Path.Combine(directory, date + "-" + shortSha + ".md");
    File.WriteAllText(path, text);
    File.WriteAllText(Path.Combine(directory, "latest.md"), text);
    return path;
  }

  private static void AppendWallClock(StringBuilder builder, IReadOnlyList<WorkloadResult> results)
  {
    builder.AppendLine();
    builder.AppendLine("## Wall-clock");
    builder.AppendLine();
    builder.AppendLine("| Host | Workload | Cases | Build ms | Cold ms | Warm median ms | Warm min ms | Warm max ms | Per-test µs |");
    builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (WorkloadResult result in results)
    {
      double[] walls = Walls(result.Warm);
      double median = Median(walls);
      double perTest = result.Cases == 0 ? 0 : median / result.Cases * 1000d;
      builder.Append("| ").Append(result.Host);
      builder.Append(" | ").Append(result.Workload);
      builder.Append(" | ").Append(result.Cases.ToString(CultureInfo.InvariantCulture));
      builder.Append(" | ").Append(Ms(result.BuildMs));
      builder.Append(" | ").Append(Ms(result.Cold.WallMs));
      builder.Append(" | ").Append(Ms(median));
      builder.Append(" | ").Append(Ms(Min(walls)));
      builder.Append(" | ").Append(Ms(Max(walls)));
      builder.Append(" | ").Append(Us(perTest));
      builder.AppendLine(" |");
    }
  }

  private static void AppendResources(StringBuilder builder, IReadOnlyList<WorkloadResult> results)
  {
    builder.AppendLine();
    builder.AppendLine("## Time-to-first-test, working set, allocations");
    builder.AppendLine();
    builder.AppendLine("Time-to-first-test is the `BENCH first` timestamp minus (capture end − process runtime). A negative delta is 0.");
    builder.AppendLine("Peak bytes are the sampled sum of `VmRSS`. A launch shorter than the 15 ms sample can report 0.");
    builder.AppendLine("Allocated bytes are the `BENCH allocated_bytes` probe delta.");
    builder.AppendLine();
    builder.AppendLine("| Host | Workload | TTFT cold ms | TTFT warm median ms | Cold peak | Warm peak | Cold allocated | Warm allocated median |");
    builder.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |");
    foreach (WorkloadResult result in results)
    {
      builder.Append("| ").Append(result.Host);
      builder.Append(" | ").Append(result.Workload);
      builder.Append(" | ").Append(MsOrBlank(result.Cold.TtftMs));
      builder.Append(" | ").Append(MsOrBlank(MedianNullable(Ttfts(result.Warm))));
      builder.Append(" | ").Append(Bytes(result.Cold.PeakBytes));
      builder.Append(" | ").Append(Bytes(MaxPeak(result.Warm)));
      builder.Append(" | ").Append(BytesOrBlank(result.Cold.AllocatedBytes));
      builder.Append(" | ").Append(BytesOrBlank(MedianNullable(Allocated(result.Warm))));
      builder.AppendLine(" |");
    }
  }

  private static void AppendMarginal(StringBuilder builder, IReadOnlyList<WorkloadResult> results)
  {
    builder.AppendLine();
    builder.AppendLine("## Marginal per-test");
    builder.AppendLine();
    builder.AppendLine("`(warm volume-10k − warm volume-1k) / 9000` microseconds. Shared startup drops out. A host needs both volume rows.");
    builder.AppendLine();
    builder.AppendLine("| Host | Volume-1k warm ms | Volume-10k warm ms | Marginal µs |");
    builder.AppendLine("| --- | ---: | ---: | ---: |");
    foreach (string host in results.Select(result => result.Host).Distinct(StringComparer.Ordinal))
    {
      WorkloadResult? small = Find(results, host, "volume-1k");
      WorkloadResult? large = Find(results, host, "volume-10k");
      if (small is null || large is null)
      {
        continue;
      }

      double smallMedian = Median(Walls(small.Warm));
      double largeMedian = Median(Walls(large.Warm));
      double marginal = (largeMedian - smallMedian) / 9000d * 1000d;
      builder.Append("| ").Append(host);
      builder.Append(" | ").Append(Ms(smallMedian));
      builder.Append(" | ").Append(Ms(largeMedian));
      builder.Append(" | ").Append(Us(marginal));
      builder.AppendLine(" |");
    }
  }

  private static void AppendInProcess(StringBuilder builder, IReadOnlyList<InProcessRow> inProcess)
  {
    builder.AppendLine();
    builder.AppendLine("## Jaribu in-process");
    builder.AppendLine();
    builder.AppendLine("BenchmarkDotNet `ShortRunJob` and `MemoryDiagnoser` call `RunTestsAsync` with `NullSink`. Means are milliseconds.");
    builder.AppendLine();
    if (inProcess.Count == 0)
    {
      builder.AppendLine("Skipped.");
      builder.AppendLine();
      return;
    }

    builder.AppendLine("| Benchmark | Mean ms | Allocated bytes |");
    builder.AppendLine("| --- | ---: | ---: |");
    foreach (InProcessRow row in inProcess)
    {
      builder.Append("| ").Append(row.Name.Replace("|", "\\|", StringComparison.Ordinal));
      builder.Append(" | ").Append(InProcessMs(row.MeanMs));
      builder.Append(" | ").Append(BytesOrBlank(row.AllocatedBytes));
      builder.AppendLine(" |");
    }

    builder.AppendLine();
  }

  private static WorkloadResult? Find(IReadOnlyList<WorkloadResult> results, string host, string workload)
  {
    foreach (WorkloadResult result in results)
    {
      if (string.Equals(result.Host, host, StringComparison.Ordinal)
        && string.Equals(result.Workload, workload, StringComparison.Ordinal))
      {
        return result;
      }
    }

    return null;
  }

  private static double[] Walls(IReadOnlyList<TimedSample> samples)
  {
    double[] values = new double[samples.Count];
    for (int index = 0; index < samples.Count; index++)
    {
      values[index] = samples[index].WallMs;
    }

    return values;
  }

  private static List<double> Ttfts(IReadOnlyList<TimedSample> samples)
  {
    List<double> values = [];
    foreach (TimedSample sample in samples)
    {
      if (sample.TtftMs is double value)
      {
        values.Add(value);
      }
    }

    return values;
  }

  private static List<double> Allocated(IReadOnlyList<TimedSample> samples)
  {
    List<double> values = [];
    foreach (TimedSample sample in samples)
    {
      if (sample.AllocatedBytes is long bytes)
      {
        values.Add(bytes);
      }
    }

    return values;
  }

  private static long MaxPeak(IReadOnlyList<TimedSample> samples)
  {
    long peak = 0;
    foreach (TimedSample sample in samples)
    {
      if (sample.PeakBytes > peak)
      {
        peak = sample.PeakBytes;
      }
    }

    return peak;
  }

  private static double Median(double[] values)
  {
    if (values.Length == 0)
    {
      return 0;
    }

    double[] sorted = [.. values];
    Array.Sort(sorted);
    int mid = sorted.Length / 2;
    if (sorted.Length % 2 == 1)
    {
      return sorted[mid];
    }

    return (sorted[mid - 1] + sorted[mid]) / 2d;
  }

  private static double? MedianNullable(List<double> values)
  {
    if (values.Count == 0)
    {
      return null;
    }

    return Median([.. values]);
  }

  private static double Min(double[] values)
  {
    double min = values[0];
    for (int index = 1; index < values.Length; index++)
    {
      if (values[index] < min)
      {
        min = values[index];
      }
    }

    return min;
  }

  private static double Max(double[] values)
  {
    double max = values[0];
    for (int index = 1; index < values.Length; index++)
    {
      if (values[index] > max)
      {
        max = values[index];
      }
    }

    return max;
  }

  private static string Ms(double value) => value.ToString("F1", CultureInfo.InvariantCulture);

  // Sub-millisecond runner times round to 0.0 at one decimal place.
  private static string InProcessMs(double value) =>
    value.ToString(value < 1d ? "F3" : "F1", CultureInfo.InvariantCulture);

  private static string Us(double value) => value.ToString("F2", CultureInfo.InvariantCulture);

  private static string MsOrBlank(double? value)
    => value is double number ? Ms(number) : "n/a";

  private static string Bytes(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

  private static string BytesOrBlank(long? value)
    => value is long number ? Bytes(number) : "n/a";

  private static string BytesOrBlank(double? value)
    => value is double number ? Bytes((long)number) : "n/a";
}
