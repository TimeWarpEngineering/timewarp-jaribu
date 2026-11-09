#region Purpose
// Builds one workload and times cold and warm process launches.
#endregion

#region Design
// Wall-clock is CommandOutput.RunTime. Process start is capture end minus that
// runtime. Time-to-first-test is the earlier of the BENCH first line and the
// signal file's last write, measured from process start. A negative delta is 0.
// When only one signal exists, that one is used. A non-zero exit or a missing
// BENCH first signal fails the launch. The runfile host passes -c Release on
// the cache fill and on every timed launch, the same configuration as the
// project hosts. BENCH runtime is the test process FrameworkDescription.
#endregion

using System.Text;

namespace DevCli.Commands;

internal static class BenchmarkMeasure
{
  private static readonly TimeSpan LaunchLimit = TimeSpan.FromMinutes(20);

  internal static async Task<double> BuildProjectAsync(
    string repo,
    BenchHost host,
    string workload,
    CancellationToken cancellationToken)
  {
    string project = Path.Combine(repo, host.ProjectRelative);
    string[] arguments =
    [
      "build",
      project,
      "-c",
      "Release",
      "-p:BenchWorkload=" + workload,
      "--nologo"
    ];
    Stopwatch stopwatch = Stopwatch.StartNew();
    CommandOutput output = await RunAsync(
      repo,
      arguments,
      token: null,
      signal: null,
      allocation: null,
      LaunchLimit,
      cancellationToken);
    stopwatch.Stop();
    if (!output.Success)
    {
      throw new InvalidOperationException(Failure(host.Id, workload, "build", output));
    }

    return stopwatch.Elapsed.TotalMilliseconds;
  }

  internal static async Task<double> WarmRunfileCacheAsync(
    string repo,
    string workload,
    CancellationToken cancellationToken)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    CommandOutput output = await RunAsync(
      repo,
      RunfileArguments(repo, workload),
      token: null,
      signal: null,
      allocation: null,
      LaunchLimit,
      cancellationToken);
    stopwatch.Stop();
    if (!output.Success || !SawFirst(output, signal: null))
    {
      throw new InvalidOperationException(Failure("jaribu-runfile", workload, "cache", output));
    }

    return stopwatch.Elapsed.TotalMilliseconds;
  }

  internal static async Task<TimedSample> LaunchAsync(
    string repo,
    BenchHost host,
    string workload,
    CancellationToken cancellationToken)
  {
    string token = "jaribu-bench-" + Guid.NewGuid().ToString("N");
    string signal = Path.Combine(Path.GetTempPath(), token + ".first");
    string allocation = Path.Combine(Path.GetTempPath(), token + ".alloc");
    string[] arguments = host.Kind switch
    {
      BenchHostKind.Runfile => RunfileArguments(repo, workload),
      BenchHostKind.DotnetTest =>
      [
        "test",
        Path.Combine(repo, host.ProjectRelative),
        "-c", "Release",
        "--no-build",
        "--no-restore",
        "-p:BenchWorkload=" + workload,
        "--nologo"
      ],
      // No --nologo here. TimeWarp.Fixie treats extra arguments as tag filters.
      _ =>
      [
        "run",
        "--project", Path.Combine(repo, host.ProjectRelative),
        "-c", "Release",
        "--no-build",
        "--no-restore",
        "-p:BenchWorkload=" + workload
      ]
    };

    using WorkingSetSampler sampler = new(token);
    sampler.Start();
    CommandOutput output;
    long peak;
    try
    {
      output = await RunAsync(repo, arguments, token, signal, allocation, LaunchLimit, cancellationToken);
    }
    finally
    {
      // Stop immediately so a failed launch does not leave the sampler running.
      peak = await sampler.StopAsync();
    }

    if (!output.Success || !SawFirst(output, signal))
    {
      TryDelete(signal);
      TryDelete(allocation);
      throw new InvalidOperationException(Failure(host.Id, workload, "launch", output));
    }

    DateTimeOffset started = DateTimeOffset.UtcNow - output.RunTime;
    double? lineTtft = null;
    long? allocated = null;
    string? runtime = null;
    foreach (OutputLine line in output.OutputLines)
    {
      if (line.Text.Contains("BENCH first", StringComparison.Ordinal))
      {
        double ms = (line.Timestamp - started).TotalMilliseconds;
        if (ms < 0)
        {
          ms = 0;
        }

        if (lineTtft is null || ms < lineTtft)
        {
          lineTtft = ms;
        }
      }

      ReadProbeMarkers(line.Text, ref allocated, ref runtime);
    }

    double? fileTtft = null;
    if (File.Exists(signal))
    {
      double ms = (File.GetLastWriteTimeUtc(signal) - started).TotalMilliseconds;
      fileTtft = ms < 0 ? 0 : ms;
    }

    if (File.Exists(allocation))
    {
      foreach (string raw in File.ReadAllLines(allocation))
      {
        ReadProbeMarkers(raw, ref allocated, ref runtime);
      }
    }

    TryDelete(signal);
    TryDelete(allocation);

    return new TimedSample
    {
      WallMs = output.RunTime.TotalMilliseconds,
      TtftMs = Earlier(lineTtft, fileTtft),
      AllocatedBytes = allocated,
      PeakBytes = peak,
      Runtime = runtime
    };
  }

  private static string[] RunfileArguments(string repo, string workload)
  {
    string runfile = Path.Combine(repo, "benchmarks", "generated", "jaribu-runfile", workload, "run.cs");
    return
    [
      "run",
      runfile,
      "-c",
      "Release",
      "-p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true",
      "--nologo"
    ];
  }

  private static void ReadProbeMarkers(string text, ref long? allocated, ref string? runtime)
  {
    const string allocatedMarker = "BENCH allocated_bytes=";
    int allocatedIndex = text.IndexOf(allocatedMarker, StringComparison.Ordinal);
    if (allocatedIndex >= 0
      && long.TryParse(
        text[(allocatedIndex + allocatedMarker.Length)..].Trim(),
        System.Globalization.NumberStyles.Integer,
        System.Globalization.CultureInfo.InvariantCulture,
        out long bytes))
    {
      allocated = bytes;
    }

    const string runtimeMarker = "BENCH runtime=";
    int runtimeIndex = text.IndexOf(runtimeMarker, StringComparison.Ordinal);
    if (runtimeIndex >= 0)
    {
      string value = text[(runtimeIndex + runtimeMarker.Length)..].Trim();
      if (value.Length > 0)
      {
        runtime = value;
      }
    }
  }

  private static double? Earlier(double? left, double? right)
  {
    if (left is null)
    {
      return right;
    }

    if (right is null)
    {
      return left;
    }

    return left.Value <= right.Value ? left : right;
  }

  private static void TryDelete(string path)
  {
    if (File.Exists(path))
    {
      File.Delete(path);
    }
  }

  private static bool SawFirst(CommandOutput output, string? signal)
  {
    if (signal is not null && File.Exists(signal))
    {
      return true;
    }

    foreach (OutputLine line in output.OutputLines)
    {
      if (line.Text.Contains("BENCH first", StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }

  private static Task<CommandOutput> RunAsync(
    string repo,
    IReadOnlyList<string> arguments,
    string? token,
    string? signal,
    string? allocation,
    TimeSpan limit,
    CancellationToken cancellationToken)
  {
    ShellBuilder builder = Shell.Builder("dotnet")
      .WithArguments([.. arguments])
      .WithWorkingDirectory(repo)
      .WithNoValidation()
      .WithTimeout(limit)
      .WithEnvironmentVariable("DOTNET_NOLOGO", "1")
      .WithEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", "1");
    if (token is not null)
    {
      builder = builder.WithEnvironmentVariable("JARIBU_BENCH_TOKEN", token);
    }

    if (signal is not null)
    {
      builder = builder.WithEnvironmentVariable("JARIBU_BENCH_SIGNAL", signal);
    }

    if (allocation is not null)
    {
      builder = builder.WithEnvironmentVariable("JARIBU_BENCH_ALLOC", allocation);
    }

    return builder.CaptureAsync(cancellationToken);
  }

  private static string Failure(string host, string workload, string phase, CommandOutput output)
  {
    string[] lines = output.GetLines();
    int start = Math.Max(0, lines.Length - 40);
    StringBuilder builder = new();
    builder.Append(host);
    builder.Append(' ');
    builder.Append(workload);
    builder.Append(' ');
    builder.Append(phase);
    builder.Append(" failed (exit ");
    builder.Append(output.ExitCode);
    builder.AppendLine(").");
    for (int index = start; index < lines.Length; index++)
    {
      builder.AppendLine(lines[index]);
    }

    return builder.ToString();
  }
}
