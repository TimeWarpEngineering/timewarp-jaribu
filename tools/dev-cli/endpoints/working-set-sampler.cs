#region Purpose
// Samples the resident set of the benchmark process tree while a launch is in flight.
#endregion

#region Design
// Amuru does not expose the child pid. The launch sets JARIBU_BENCH_TOKEN and this
// sampler sums VmRSS of every process that inherited it. The interval is 15 ms.
// A launch that exits between samples reports 0.
#endregion

using System.Text;

namespace DevCli.Commands;

internal sealed class WorkingSetSampler : IDisposable
{
  private const int IntervalMs = 15;
  private readonly string Token;
  private long PeakBytes;
  private CancellationTokenSource? Cts;
  private Task? Loop;

  internal WorkingSetSampler(string token)
  {
    Token = token;
  }

  internal void Start()
  {
    Cts = new CancellationTokenSource();
    Loop = SampleAsync(Cts.Token);
  }

  internal async Task<long> StopAsync()
  {
    if (Cts is null || Loop is null)
    {
      return PeakBytes;
    }

    await Cts.CancelAsync();
    try
    {
      await Loop;
    }
    catch (OperationCanceledException)
    {
      // The loop ends on cancel.
    }

    Note(Scan());
    Cts.Dispose();
    Cts = null;
    Loop = null;
    return PeakBytes;
  }

  public void Dispose()
  {
    Cts?.Dispose();
    Cts = null;
  }

  private async Task SampleAsync(CancellationToken cancellationToken)
  {
    while (!cancellationToken.IsCancellationRequested)
    {
      Note(Scan());
      try
      {
        await Task.Delay(IntervalMs, cancellationToken);
      }
      catch (OperationCanceledException)
      {
        return;
      }
    }
  }

  private void Note(long bytes)
  {
    if (bytes > PeakBytes)
    {
      PeakBytes = bytes;
    }
  }

  private long Scan()
  {
    if (!Directory.Exists("/proc"))
    {
      return 0;
    }

    long sum = 0;
    foreach (string directory in Directory.EnumerateDirectories("/proc"))
    {
      string name = Path.GetFileName(directory);
      if (name.Length == 0 || !char.IsAsciiDigit(name[0]))
      {
        continue;
      }

      try
      {
        byte[] environ = File.ReadAllBytes(Path.Combine(directory, "environ"));
        if (Encoding.UTF8.GetString(environ).Contains(Token, StringComparison.Ordinal))
        {
          sum += ReadRssBytes(Path.Combine(directory, "status"));
        }
      }
      catch (IOException)
      {
        // The process exited between the listing and the read.
      }
      catch (UnauthorizedAccessException)
      {
        // Another user's process. It does not carry our token.
      }
    }

    return sum;
  }

  private static long ReadRssBytes(string statusPath)
  {
    foreach (string line in File.ReadLines(statusPath))
    {
      if (!line.StartsWith("VmRSS:", StringComparison.Ordinal))
      {
        continue;
      }

      string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length >= 2 && long.TryParse(parts[1], out long kilobytes))
      {
        return kilobytes * 1024;
      }
    }

    return 0;
  }
}
