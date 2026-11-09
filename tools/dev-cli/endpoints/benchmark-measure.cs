#region Purpose
// Builds one workload and times cold and warm process launches.
#endregion

#region Design
// Wall-clock is CommandOutput.RunTime. Time-to-first-test is the BENCH first
// line timestamp minus (capture time − RunTime), so both ends use the process clock.
// A non-zero exit or a missing BENCH first line fails the launch.
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
    double? ttft = null;
    long? allocated = null;
    foreach (OutputLine line in output.OutputLines)
    {
      if (line.Text.Contains("BENCH first", StringComparison.Ordinal))
      {
        double ms = (line.Timestamp - started).TotalMilliseconds;
        if (ms < 0)
        {
          ms = 0;
        }

        if (ttft is null || ms < ttft)
        {
          ttft = ms;
        }
      }

      const string marker = "BENCH allocated_bytes=";
      int index = line.Text.IndexOf(marker, StringComparison.Ordinal);
      if (index >= 0
        && long.TryParse(
          line.Text[(index + marker.Length)..].Trim(),
          System.Globalization.NumberStyles.Integer,
          System.Globalization.CultureInfo.InvariantCulture,
          out long bytes))
      {
        allocated = bytes;
      }
    }

    if (ttft is null && File.Exists(signal))
    {
      double ms = (File.GetLastWriteTimeUtc(signal) - started).TotalMilliseconds;
      ttft = ms < 0 ? 0 : ms;
    }

    if (allocated is null && File.Exists(allocation))
    {
      string text = File.ReadAllText(allocation).Trim();
      const string marker = "BENCH allocated_bytes=";
      if (text.StartsWith(marker, StringComparison.Ordinal)
        && long.TryParse(
          text[marker.Length..],
          System.Globalization.NumberStyles.Integer,
          System.Globalization.CultureInfo.InvariantCulture,
          out long bytes))
      {
        allocated = bytes;
      }
    }

    TryDelete(signal);
    TryDelete(allocation);

    return new TimedSample
    {
      WallMs = output.RunTime.TotalMilliseconds,
      TtftMs = ttft,
      AllocatedBytes = allocated,
      PeakBytes = peak
    };
  }

  private static string[] RunfileArguments(string repo, string workload)
  {
    string runfile = Path.Combine(repo, "benchmarks", "generated", "jaribu-runfile", workload, "run.cs");
    return
    [
      "run",
      runfile,
      "-p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true",
      "--nologo"
    ];
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
