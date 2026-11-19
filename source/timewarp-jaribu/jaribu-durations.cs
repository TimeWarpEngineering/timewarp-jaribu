namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;
using System.Text.Json;

#region Purpose
// Writes one JSON file of executed test-case durations at the end of a run.
#endregion

#region Design
// The file is the local input to dev slow-tests. A span export is not required.
// EnterRun on the outermost scope owns the file. Class depth is an AsyncLocal,
// the same way run depth is, so each flow has its own copy. Only depth 1 is
// recorded. A runner call from inside a test, including one started on another
// thread, does not join the outer file or write one of its own. Those flows
// share the session object, so rows are appended under its lock.
// A skipped case stores outcome skipped and a null duration. JARIBU_DURATIONS=0
// skips the write. JARIBU_DURATIONS_PATH replaces the file. An existing directory,
// or a path that ends with a separator, receives durations-<utc>.json. Otherwise
// the default path is artifacts/jaribu/durations-<utc>.json relative to the process
// working directory. A write failure is traced and does not fail the run.
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
  private static readonly AsyncLocal<int> ClassDepth = new();

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
    if (session is null || OptOut())
    {
      return;
    }

    List<Row> records;
    lock (session.Gate)
    {
      if (!session.SawClass)
      {
        return;
      }

      records = [.. session.Records];
    }

    try
    {
      Write(records);
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
    ClassDepth.Value++;
    lock (session.Gate)
    {
      session.SawClass = true;
    }
  }

  internal static void ExitClass()
  {
    if (ClassDepth.Value == 0)
    {
      return;
    }

    ClassDepth.Value--;
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
    if (session is null || ClassDepth.Value != 1)
    {
      return;
    }

    bool skipped = state == TestNodeState.Skipped;
    Row row = new(
      testClass.FullName ?? testClass.Name,
      method.Name,
      caseIndex,
      JaribuTelemetry.FormatCaseArguments(parameters),
      JaribuTelemetry.OutcomeFor(state),
      skipped ? null : duration?.TotalMilliseconds,
      Tags(testClass, method));
    lock (session.Gate)
    {
      session.Records.Add(row);
    }
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
      if (IsDirectoryOverride(overridePath))
      {
        return UniqueFile(Path.GetFullPath(overridePath));
      }

      return Path.GetFullPath(overridePath);
    }

    return UniqueFile(Path.Combine("artifacts", "jaribu"));
  }

  private static bool IsDirectoryOverride(string overridePath)
  {
    char last = overridePath[^1];
    if (last == Path.DirectorySeparatorChar || last == Path.AltDirectorySeparatorChar)
    {
      return true;
    }

    return Directory.Exists(overridePath);
  }

  private static string UniqueFile(string directory)
  {
    string stamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
    string path = Path.Combine(directory, "durations-" + stamp + ".json");
    int suffix = 2;
    while (File.Exists(path))
    {
      path = Path.Combine(
        directory,
        "durations-" + stamp + "-" + suffix.ToString(CultureInfo.InvariantCulture) + ".json");
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
    public object Gate { get; } = new();

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
