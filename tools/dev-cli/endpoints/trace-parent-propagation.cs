#region Purpose
// Copies the current span onto a child process so test spans join the dev command.
#endregion

#region Design
// TelemetryBehavior leaves the command span as Activity.Current for the handler.
// The test process reads TRACEPARENT and does not see that in-memory activity.
// A missing or empty current span leaves the child's environment alone, so an
// inherited TRACEPARENT from ganda or CI still applies.
// The header is not logged.
#endregion

namespace DevCli;

internal static class TraceParentPropagation
{
  internal static ShellBuilder Apply(ShellBuilder builder)
  {
    return Apply(builder, static (item, key, value) => item.WithEnvironmentVariable(key, value));
  }

  internal static DotNetRunBuilder Apply(DotNetRunBuilder builder)
  {
    return Apply(builder, static (item, key, value) => item.WithEnvironmentVariable(key, value));
  }

  private static T Apply<T>(T builder, Func<T, string, string, T> setVariable)
  {
    Activity? current = Activity.Current;
    if (current is null || current.TraceId == default || current.SpanId == default)
    {
      return builder;
    }

    string flags = current.ActivityTraceFlags.HasFlag(ActivityTraceFlags.Recorded) ? "01" : "00";
    builder = setVariable(
      builder,
      "TRACEPARENT",
      string.Concat(
        "00-",
        current.TraceId.ToHexString(),
        "-",
        current.SpanId.ToHexString(),
        "-",
        flags));
    if (!string.IsNullOrEmpty(current.TraceStateString))
    {
      builder = setVariable(builder, "TRACESTATE", current.TraceStateString);
    }

    return builder;
  }
}
