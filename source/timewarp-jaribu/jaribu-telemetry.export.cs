namespace TimeWarp.Jaribu;

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using TimeWarp.Amuru;

#region Purpose
// OTLP export for the test process, and the trace context handed to a relaunched child.
#endregion

#region Design
// The exporter exists only when OTEL_EXPORTER_OTLP_ENDPOINT is set. AddOtlpExporter
// reads that variable and OTEL_EXPORTER_OTLP_PROTOCOL. OTEL_SERVICE_NAME overrides
// the default service name TimeWarp.Jaribu. A failed builder does not fail the run.
// EnterRun / ExitRun nest. The outermost exit ForceFlush-es, including a failing run.
// BeginRun is that scope for a host. The MTP adapter holds one across its class loop.
// Process exit flushes again so a host that skips the return path still exports.
// The outermost enter also opens the durations file. The matching exit writes it, then flushes.
// ApplyTraceContext copies the ambient span, or the current TRACEPARENT, onto a
// coverage relaunch so the grandchild stays on the same trace.
#endregion

public static partial class JaribuTelemetry
{
  private const int FlushTimeoutMilliseconds = 10000;

  private static readonly object Gate = new();
  private static readonly AsyncLocal<int> RunDepth = new();
  private static int ProviderState;
  private static TracerProvider? Provider;

  /// <summary>
  /// Opens one export scope. Nested class runs flush once when the returned scope is disposed.
  /// </summary>
  /// <returns>A scope whose dispose flushes the exporter, including after a failed run.</returns>
  public static IDisposable BeginRun()
  {
    EnterRun();
    return new RunScope();
  }

  internal static void EnterRun()
  {
    if (RunDepth.Value == 0)
    {
      JaribuDurations.BeginOuter();
    }

    EnsureStarted();
    RunDepth.Value++;
  }

  internal static void ExitRun()
  {
    int depth = RunDepth.Value - 1;
    if (depth < 0)
    {
      depth = 0;
    }

    RunDepth.Value = depth;
    if (depth == 0)
    {
      JaribuDurations.EndOuter();
      Flush();
    }
  }

  internal static ShellBuilder ApplyTraceContext(ShellBuilder builder)
  {
    ArgumentNullException.ThrowIfNull(builder);
    string? traceparent = CurrentTraceParent();
    if (traceparent is null)
    {
      return builder;
    }

    builder = builder.WithEnvironmentVariable("TRACEPARENT", traceparent);
    string? tracestate = Activity.Current?.TraceStateString;
    if (string.IsNullOrEmpty(tracestate))
    {
      tracestate = Environment.GetEnvironmentVariable("TRACESTATE");
    }

    if (!string.IsNullOrEmpty(tracestate))
    {
      builder = builder.WithEnvironmentVariable("TRACESTATE", tracestate);
    }

    return builder;
  }

  private static void EnsureStarted()
  {
    if (Volatile.Read(ref ProviderState) == 1)
    {
      return;
    }

    lock (Gate)
    {
      if (ProviderState == 1)
      {
        return;
      }

      RunExporter(
        () =>
        {
          string? endpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
          if (!string.IsNullOrWhiteSpace(endpoint))
          {
            Provider = BuildProvider();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
          }
        },
        "did not start the OTLP exporter");

      ProviderState = 1;
    }
  }

  private static TracerProvider BuildProvider()
  {
    ResourceBuilder resource = ResourceBuilder.CreateDefault();
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME")))
    {
      resource = resource.AddService(ActivitySourceName);
    }

    return Sdk.CreateTracerProviderBuilder()
      .AddSource(ActivitySourceName)
      .SetResourceBuilder(resource)
      .AddOtlpExporter()
      .Build();
  }

  private static void Flush()
  {
    TracerProvider? provider = Provider;
    if (provider is null)
    {
      return;
    }

    lock (Gate)
    {
      RunExporter(
        () => provider.ForceFlush(FlushTimeoutMilliseconds),
        "did not flush spans");
    }
  }

  private static void RunExporter(Action action, string failure)
  {
    try
    {
      action();
    }
    catch (ArgumentException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (FormatException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (IOException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (InvalidOperationException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (NotSupportedException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (System.Net.Http.HttpRequestException exception)
    {
      ReportExporter(failure, exception);
    }
    catch (TimeoutException exception)
    {
      ReportExporter(failure, exception);
    }
  }

  private static void ReportExporter(string failure, Exception exception)
  {
    Trace.TraceError("TimeWarp.Jaribu {0}: {1}", failure, exception.Message);
  }

  private static void OnProcessExit(object? sender, EventArgs args)
  {
    Flush();
    lock (Gate)
    {
      Provider?.Dispose();
      Provider = null;
    }
  }

  private sealed class RunScope : IDisposable
  {
    private int Disposed;

    public void Dispose()
    {
      if (Interlocked.Exchange(ref Disposed, 1) == 0)
      {
        ExitRun();
      }
    }
  }

  private static string? CurrentTraceParent()
  {
    Activity? current = Activity.Current;
    if (current is not null && current.TraceId != default && current.SpanId != default)
    {
      string flags = current.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
      return string.Concat(
        "00-",
        current.TraceId.ToHexString(),
        "-",
        current.SpanId.ToHexString(),
        "-",
        flags);
    }

    string? traceparent = Environment.GetEnvironmentVariable("TRACEPARENT");
    if (string.IsNullOrWhiteSpace(traceparent))
    {
      return null;
    }

    if (!ActivityContext.TryParse(traceparent, null, isRemote: true, out ActivityContext parsed))
    {
      return null;
    }

    if (parsed.TraceId == default || parsed.SpanId == default)
    {
      return null;
    }

    return traceparent;
  }
}
