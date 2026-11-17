#region Purpose
// Fixtures and capture types for the telemetry tests. These classes are not registered.
#endregion

#region Design
// The meta-test file includes this sibling. The MTP project lists it because a
// normal project ignores #:include.
#endregion

namespace TestRunner_;

using System.Net;
using System.Net.Sockets;

  /// <summary>Pass, fail, skip, timeout, and two input rows.</summary>
  public class TelemetrySpanFixture
  {
    public static Task SetupOnce() => Task.CompletedTask;

    public static Task CleanUpOnce() => Task.CompletedTask;

    public static Task Setup() => Task.CompletedTask;

    public static Task CleanUp() => Task.CompletedTask;

    public static async Task Should_Pass()
    {
      await Task.CompletedTask;
    }

    public static Task Should_Fail()
    {
      throw new InvalidOperationException("telemetry failure");
    }

    [Skip("not this run")]
    public static async Task Should_Skip()
    {
      await Task.CompletedTask;
    }

    [Timeout(200)]
    public static async Task Should_TimeOut()
    {
      await Task.Delay(TimeSpan.FromSeconds(3));
    }

    [Input("alpha")]
    [Input("beta")]
    public static async Task Should_EchoInput(string value)
    {
      value.ShouldNotBeNullOrEmpty();
      await Task.CompletedTask;
    }
  }

  /// <summary>Async SetupOnce that faults after it yields.</summary>
  public class TelemetryFaultingSetupOnceFixture
  {
    public static async Task SetupOnce()
    {
      await Task.Yield();
      throw new InvalidOperationException("setup once failed");
    }

    public static async Task Should_Pass()
    {
      await Task.CompletedTask;
    }
  }

  /// <summary>One passing test used to read the parent trace.</summary>
  public class TelemetryParentFixture
  {
    public static async Task Should_Pass()
    {
      await Task.CompletedTask;
    }
  }

  /// <summary>Blocks until the meta-test cancels the run.</summary>
  public class TelemetryCancelFixture
  {
    public static TaskCompletionSource<bool> Entered { get; private set; } = NewSource();

    public static TaskCompletionSource<bool> Release { get; private set; } = NewSource();

    public static void Reset()
    {
      Entered = NewSource();
      Release = NewSource();
    }

    public static async Task Should_Wait()
    {
      Entered.TrySetResult(true);
      await Release.Task;
    }

    private static TaskCompletionSource<bool> NewSource()
    {
      return new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
  }

  internal sealed class TelemetryCapture : IDisposable
  {
    private readonly ActivityListener Listener;
    private readonly object Gate = new();

    public TelemetryCapture()
    {
      Listener = new ActivityListener
      {
        ShouldListenTo = static source => source.Name == JaribuTelemetry.ActivitySourceName,
        Sample = static (ref ActivityCreationOptions<ActivityContext> _) =>
          ActivitySamplingResult.AllDataAndRecorded,
        ActivityStopped = OnStopped
      };
      ActivitySource.AddActivityListener(Listener);
    }

    public List<CapturedSpan> Spans { get; } = [];

    public void Dispose() => Listener.Dispose();

    public List<CapturedSpan> ForClass(string className)
    {
      return
      [
        .. Spans.Where(span =>
          span.Tags.TryGetValue(JaribuTelemetry.ClassAttribute, out string? value) &&
          value == className)
      ];
    }

    public List<CapturedSpan> TestsFor(string className)
    {
      return
      [
        .. ForClass(className).Where(span =>
          span.Name.StartsWith(JaribuTelemetry.TestSpanPrefix, StringComparison.Ordinal))
      ];
    }

    public List<CapturedSpan> Children(string parentSpanId, string namePrefix)
    {
      return
      [
        .. Spans.Where(span =>
          span.ParentSpanId == parentSpanId &&
          span.Name.StartsWith(namePrefix, StringComparison.Ordinal))
      ];
    }

    private void OnStopped(Activity activity)
    {
      Dictionary<string, string> tags = [];
      foreach (KeyValuePair<string, object?> tag in activity.TagObjects)
      {
        if (tag.Value is not null)
        {
          tags[tag.Key] = Convert.ToString(tag.Value, CultureInfo.InvariantCulture) ?? string.Empty;
        }
      }

      CapturedSpan span = new(
        activity.DisplayName,
        activity.SpanId.ToHexString(),
        activity.ParentSpanId.ToHexString(),
        activity.TraceId.ToHexString(),
        activity.Status,
        tags);
      lock (Gate)
      {
        Spans.Add(span);
      }
    }
  }

  internal readonly record struct CapturedSpan(
    string Name,
    string SpanId,
    string ParentSpanId,
    string TraceId,
    ActivityStatusCode Status,
    Dictionary<string, string> Tags);

  internal sealed class TelemetryListener : IDisposable
  {
    private readonly HttpListener Listener;
    private readonly List<byte> Body = [];
    private readonly object Gate = new();

    private TelemetryListener(HttpListener listener, int port)
    {
      Listener = listener;
      Endpoint = $"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}";
      _ = AcceptAsync();
    }

    public string Endpoint { get; }

    public static TelemetryListener Start()
    {
      using TcpListener tcp = new(IPAddress.Loopback, 0);
      tcp.Start();
      int port = ((IPEndPoint)tcp.LocalEndpoint).Port;
      tcp.Stop();
      HttpListener? listener = new();
      try
      {
        listener.Prefixes.Add($"http://127.0.0.1:{port.ToString(CultureInfo.InvariantCulture)}/");
        listener.Start();
        TelemetryListener started = new(listener, port);
        listener = null;
        return started;
      }
      finally
      {
        listener?.Close();
      }
    }

    public byte[] TakeBody()
    {
      lock (Gate)
      {
        return [.. Body];
      }
    }

    private async Task AcceptAsync()
    {
      while (Listener.IsListening)
      {
        try
        {
          HttpListenerContext context = await Listener.GetContextAsync();
          using MemoryStream copy = new();
          await context.Request.InputStream.CopyToAsync(copy);
          context.Response.StatusCode = 200;
          context.Response.Close();
          lock (Gate)
          {
            Body.AddRange(copy.ToArray());
          }
        }
        catch (Exception) when (!Listener.IsListening)
        {
          break;
        }
      }
    }

    public void Dispose()
    {
      Listener.Stop();
      Listener.Close();
    }
  }
