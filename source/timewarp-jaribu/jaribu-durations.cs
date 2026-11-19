namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;
using System.Text.Json;

#region Purpose
// Writes one JSON file of executed test-case durations at the end of a run.
#endregion

#region Design
// The file is the local input to dev slow-tests. A span export is not required.
// EnterRun on the outermost scope owns the file. Each RunTestsAsync class run
// bumps a depth on that scope. Only depth 1 is recorded, so a runner call made
// from inside a test does not join the outer file or write one of its own.
// A skipped case stores outcome skipped and a null duration. JARIBU_DURATIONS=0
// skips the write. JARIBU_DURATIONS_PATH replaces artifacts/jaribu/durations-<utc>.json.
// A write failure is traced and does not fail the run.
#endregion

/// <summary>
/// Per-run durations file. The runner writes it when the outermost run returns.
/// </summary>
internal static class JaribuDurations
{
  internal const string OptOutVariable = "JARIBU_DURATIONS";
  internal const string PathVariable = "JARIBU_DURATIONS_PATH";
  internal const string DirectoryName = "artifacts/jaribu";

  private static readonly AsyncLocal<Session?> Current = new();

  internal static void BeginOuter()
  {
    if (Current.Value is null)
    {
      Current.Value = new Session();
    }
  }

  internal static void EndOuter()
  {
    Session? session = Current.Value;
    Current.Value = null;
    if (session is null || !session.SawClass || OptOut())
    {
      return;
    }

    try
    {
      Write(session.Records);
    }
    catch (ArgumentException exception)
    {
      TraceWrite(exception);
    }
    catch (IOException exception)
    {
      TraceWrite(exception);
    }
    catch (NotSupportedException exception)
    {
      TraceWrite(exception);
    }
    catch (UnauthorizedAccessException exception)
    {
      TraceWrite(exception);
    }
  }

  internal static void EnterClass()
  {
    Session session = Current.Value ?? new Session();
    Current.Value = session;
    session.ClassDepth++;
    session.SawClass = true;
  }

  internal static void ExitClass()
  {
    Session? session = Current.Value;
    if (session is null || session.ClassDepth == 0)
    {
      return;
    }

    session.ClassDepth--;
  }

  internal static void Record(
    Type testClass,
    MethodInfo method,
    int? caseIndex,
    IReadOnlyList<object?>? parameters,
    TestNodeState state,
    TimeSpan? duration)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    Session? session = Current.Value;
    if (session is null || session.ClassDepth != 1)
    {
      return;
    }

    bool skipped = state == TestNodeState.Skipped;
    session.Records.Add(
      new Row(
        testClass.FullName ?? testClass.Name,
        method.Name,
        caseIndex,
        JaribuTelemetry.FormatCaseArguments(parameters),
        JaribuTelemetry.OutcomeFor(state),
        skipped ? null : duration?.TotalMilliseconds,
        Tags(testClass, method)));
  }

  internal static void Record(
    Type testClass,
    ReportedTestCase testCase,
    TestNodeState state,
    TimeSpan? duration)
  {
    Record(
      testClass,
      testCase.Method,
      testCase.InputOrdinal,
      testCase.Parameters,
      state,
      duration);
  }

  private static bool OptOut()
  {
    string? value = Environment.GetEnvironmentVariable(OptOutVariable);
    return value is not null && value.Trim().Equals("0", StringComparison.Ordinal);
  }

  private static void Write(List<Row> rows)
  {
    string path = ResolvePath();
    string? directory = Path.GetDirectoryName(path);
    if (!string.IsNullOrEmpty(directory))
    {
      Directory.CreateDirectory(directory);
    }

    JsonWriterOptions options = new()
    {
      Indented = true
    };
    using MemoryStream stream = new();
    using (Utf8JsonWriter writer = new(stream, options))
    {
      writer.WriteStartObject();
      writer.WriteStartArray("records");
      foreach (Row row in rows)
      {
        writer.WriteStartObject();
        writer.WriteString("class", row.ClassName);
        writer.WriteString("method", row.Method);
        if (row.CaseIndex is int caseIndex)
        {
          writer.WriteNumber("caseIndex", caseIndex);
        }
        else
        {
          writer.WriteNull("caseIndex");
        }

        if (row.Arguments is null)
        {
          writer.WriteNull("arguments");
        }
        else
        {
          writer.WriteString("arguments", row.Arguments);
        }

        writer.WriteString("outcome", row.Outcome);
        if (row.DurationMilliseconds is double milliseconds)
        {
          writer.WriteNumber("durationMilliseconds", milliseconds);
        }
        else
        {
          writer.WriteNull("durationMilliseconds");
        }

        writer.WriteStartArray("tags");
        foreach (string tag in row.Tags)
        {
          writer.WriteStringValue(tag);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
      }

      writer.WriteEndArray();
      writer.WriteEndObject();
    }

    File.WriteAllBytes(path, stream.ToArray());
  }

  private static string ResolvePath()
  {
    string? overridePath = Environment.GetEnvironmentVariable(PathVariable);
    if (!string.IsNullOrWhiteSpace(overridePath))
    {
      return Path.GetFullPath(overridePath);
    }

    string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
    string directory = Path.Combine("artifacts", "jaribu");
    string path = Path.Combine(directory, "durations-" + stamp + ".json");
    int suffix = 2;
    while (File.Exists(path))
    {
      path = Path.Combine(directory, "durations-" + stamp + "-" + suffix.ToString(CultureInfo.InvariantCulture) + ".json");
      suffix++;
    }

    return path;
  }

  private static string[] Tags(Type testClass, MethodInfo method)
  {
    List<string> tags = [];
    Add(tags, SafeClassTags(testClass));
    Add(tags, SafeMethodTags(method));
    return tags.Count == 0 ? [] : [.. tags];
  }

  private static void Add(List<string> tags, string[] values)
  {
    foreach (string tag in values)
    {
      bool seen = false;
      foreach (string existing in tags)
      {
        if (existing.Equals(tag, StringComparison.Ordinal))
        {
          seen = true;
          break;
        }
      }

      if (!seen)
      {
        tags.Add(tag);
      }
    }
  }

  private static string[] SafeClassTags(Type testClass)
  {
    TestRunner.ClassMetadata metadata = TestRunner.GetClassMetadata(testClass);
    if (metadata.TryReject(out _))
    {
      return [];
    }

    return metadata.Tags();
  }

  private static string[] SafeMethodTags(MethodInfo method)
  {
    TestRunner.MethodMetadata metadata = TestRunner.GetMethodMetadata(method);
    if (metadata.TryReject(out _))
    {
      return [];
    }

    return metadata.Tags();
  }

  private static void TraceWrite(Exception exception)
  {
    System.Diagnostics.Trace.TraceError(
      "TimeWarp.Jaribu did not write the durations file: {0}",
      exception.Message);
  }

  private sealed class Session
  {
    public int ClassDepth { get; set; }

    public bool SawClass { get; set; }

    public List<Row> Records { get; } = [];
  }

  private readonly record struct Row(
    string ClassName,
    string Method,
    int? CaseIndex,
    string? Arguments,
    string Outcome,
    double? DurationMilliseconds,
    string[] Tags);
}
