#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Child process that exports one passing and one failing test span.
#endregion

#region Design
// The parent test sets OTEL_EXPORTER_OTLP_ENDPOINT and TRACEPARENT.
// A failing case keeps the process exit at 1 so the parent can see a flush
// that still happened.
#endregion

using TimeWarp.Jaribu;

return await TestRunner.RunTests<TelemetryExportProbe>();

/// <summary>Probe class executed only by the telemetry export test.</summary>
public class TelemetryExportProbe
{
  /// <summary>Per-test hook so the export includes a hook span.</summary>
  public static Task Setup() => Task.CompletedTask;

  /// <summary>Per-test hook so the export includes a hook span.</summary>
  public static Task CleanUp() => Task.CompletedTask;

  /// <summary>Passing case.</summary>
  public static async Task Should_Pass()
  {
    await Task.CompletedTask;
  }

  /// <summary>Failing case. The run exits 1.</summary>
  public static Task Should_Fail()
  {
    throw new InvalidOperationException("telemetry export failure");
  }
}
