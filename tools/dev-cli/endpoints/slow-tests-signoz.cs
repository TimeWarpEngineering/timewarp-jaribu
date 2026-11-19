#region Purpose
// Builds and reads the SigNoz query behind dev slow-tests --signoz.
#endregion

#region Design
// SIGNOZ_API_KEY is the name ganda already uses. SIGNOZ_URL is the base URL
// because ganda's MCP entry hardcodes the host. The query is POST /api/v5/query_range,
// requestType scalar, two builder queries on jaribu.test spans for service
// TimeWarp.Jaribu. A is count, p50, p95, and max of durationNano, grouped by
// class, method, and case index. B counts outcomes failed, timed_out, error,
// and cancelled for the failure rate. The recorded fixture is that response
// shape. This type does not open a socket.
#endregion

namespace DevCli.Commands;

using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

internal static class SignozSlowTests
{
  internal const string UrlVariable = "SIGNOZ_URL";
  internal const string KeyVariable = "SIGNOZ_API_KEY";
  internal const string ServiceName = "TimeWarp.Jaribu";
  internal const int QueryLimit = 1000;

  internal static string? RequirementMessage(string? url, string? key)
  {
    bool missingUrl = string.IsNullOrWhiteSpace(url);
    bool missingKey = string.IsNullOrWhiteSpace(key);
    if (!missingUrl && !missingKey)
    {
      return null;
    }

    if (missingUrl && missingKey)
    {
      return "Set SIGNOZ_URL and SIGNOZ_API_KEY.";
    }

    return missingUrl ? "Set SIGNOZ_URL." : "Set SIGNOZ_API_KEY.";
  }

  internal static bool TryParseSince(string? text, out TimeSpan window, out string? error)
  {
    string value = string.IsNullOrWhiteSpace(text) ? "7d" : text.Trim();
    if (value.Length < 2 ||
        !int.TryParse(value[..^1], NumberStyles.None, CultureInfo.InvariantCulture, out int count) ||
        count < 1)
    {
      window = default;
      error = "slow-tests --since expects a window such as 7d, 24h, 30m, or 45s.";
      return false;
    }

    window = char.ToLowerInvariant(value[^1]) switch
    {
      'd' => TimeSpan.FromDays(count),
      'h' => TimeSpan.FromHours(count),
      'm' => TimeSpan.FromMinutes(count),
      's' => TimeSpan.FromSeconds(count),
      _ => default
    };
    if (window == default)
    {
      error = "slow-tests --since expects a window such as 7d, 24h, 30m, or 45s.";
      return false;
    }

    error = null;
    return true;
  }

  internal static string Endpoint(string url)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(url);
    string trimmed = url.Trim().TrimEnd('/');
    if (trimmed.EndsWith("/api/v5/query_range", StringComparison.OrdinalIgnoreCase))
    {
      return trimmed;
    }

    return trimmed + "/api/v5/query_range";
  }

  internal static string BuildRequestJson(long startMs, long endMs, string? outcome)
  {
    string filter = "service.name = '" + ServiceName + "' AND name CONTAINS 'jaribu.test'";
    if (outcome is not null)
    {
      filter += " AND jaribu.test.outcome = '" + outcome + "'";
    }

    string failures = filter + " AND jaribu.test.outcome IN ('failed', 'timed_out', 'error', 'cancelled')";
    using MemoryStream stream = new();
    JsonWriterOptions options = new()
    {
      Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    using (Utf8JsonWriter writer = new(stream, options))
    {
      writer.WriteStartObject();
      writer.WriteNumber("start", startMs);
      writer.WriteNumber("end", endMs);
      writer.WriteString("requestType", "scalar");
      writer.WriteStartObject("variables");
      writer.WriteEndObject();
      writer.WriteStartObject("compositeQuery");
      writer.WriteStartArray("queries");
      WriteQuery(writer, "A", filter, true);
      WriteQuery(writer, "B", failures, false);
      writer.WriteEndArray();
      writer.WriteEndObject();
      writer.WriteEndObject();
    }

    return Encoding.UTF8.GetString(stream.ToArray());
  }

  internal static IReadOnlyList<SignozGroup> ParseResponse(string json)
  {
    ArgumentException.ThrowIfNullOrEmpty(json);
    using JsonDocument document = JsonDocument.Parse(json);
    JsonElement results = FindResults(document.RootElement);
    Dictionary<string, MutableGroup> groups = [];
    foreach (JsonElement result in results.EnumerateArray())
    {
      string query = result.TryGetProperty("queryName", out JsonElement name) && name.ValueKind == JsonValueKind.String
        ? name.GetString() ?? string.Empty
        : string.Empty;
      if (!result.TryGetProperty("columns", out JsonElement columns) ||
          columns.ValueKind != JsonValueKind.Array ||
          !result.TryGetProperty("data", out JsonElement data) ||
          data.ValueKind != JsonValueKind.Array)
      {
        continue;
      }

      List<string> headers = [];
      foreach (JsonElement column in columns.EnumerateArray())
      {
        headers.Add(column.TryGetProperty("name", out JsonElement columnName) && columnName.ValueKind == JsonValueKind.String
          ? columnName.GetString() ?? string.Empty
          : string.Empty);
      }

      foreach (JsonElement row in data.EnumerateArray())
      {
        if (row.ValueKind != JsonValueKind.Array)
        {
          continue;
        }

        ApplyRow(groups, query, headers, row);
      }
    }

    List<SignozGroup> ranked = [];
    foreach (MutableGroup group in groups.Values)
    {
      if (group.Count <= 0)
      {
        continue;
      }

      double rate = group.Failures / (double)group.Count;
      ranked.Add(
        new SignozGroup(
          group.ClassName,
          group.Method,
          group.CaseIndex,
          group.Count,
          group.P50Milliseconds,
          group.P95Milliseconds,
          group.MaxMilliseconds,
          rate));
    }

    ranked.Sort(static (left, right) =>
    {
      int p95 = right.P95Milliseconds.CompareTo(left.P95Milliseconds);
      if (p95 != 0)
      {
        return p95;
      }

      int p50 = right.P50Milliseconds.CompareTo(left.P50Milliseconds);
      if (p50 != 0)
      {
        return p50;
      }

      int className = string.CompareOrdinal(left.ClassName, right.ClassName);
      if (className != 0)
      {
        return className;
      }

      int method = string.CompareOrdinal(left.Method, right.Method);
      return method != 0 ? method : Nullable.Compare(left.CaseIndex, right.CaseIndex);
    });
    return ranked;
  }

  internal static string Format(
    IReadOnlyList<SignozGroup> groups,
    int top,
    bool markdown,
    string window)
  {
    ArgumentNullException.ThrowIfNull(groups);
    ArgumentOutOfRangeException.ThrowIfLessThan(top, 1);

    int shown = Math.Min(top, groups.Count);
    StringBuilder builder = new();
    builder.Append("Source: SigNoz ").Append(ServiceName).Append(" jaribu.test since ").AppendLine(window);
    builder
      .Append("Showing ")
      .Append(shown.ToString(CultureInfo.InvariantCulture))
      .Append(" of ")
      .Append(groups.Count.ToString(CultureInfo.InvariantCulture))
      .AppendLine(" groups by p95.");
    if (markdown)
    {
      builder.AppendLine("| Class | Method | Case | Count | p50 ms | p95 ms | Max ms | Failure rate |");
      builder.AppendLine("| --- | --- | --- | ---: | ---: | ---: | ---: | ---: |");
    }
    else
    {
      builder.AppendLine("Class  Method  Case  Count  p50 ms  p95 ms  Max ms  Failure rate");
    }

    for (int index = 0; index < shown; index++)
    {
      SignozGroup group = groups[index];
      string rate = (group.FailureRate * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
      string caseText = group.CaseIndex is int caseIndex
        ? caseIndex.ToString(CultureInfo.InvariantCulture)
        : string.Empty;
      if (markdown)
      {
        builder
          .Append("| ")
          .Append(group.ClassName)
          .Append(" | ")
          .Append(group.Method)
          .Append(" | ")
          .Append(caseText)
          .Append(" | ")
          .Append(group.Count.ToString(CultureInfo.InvariantCulture))
          .Append(" | ")
          .Append(group.P50Milliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append(" | ")
          .Append(group.P95Milliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append(" | ")
          .Append(group.MaxMilliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append(" | ")
          .Append(rate)
          .AppendLine(" |");
      }
      else
      {
        builder
          .Append(group.ClassName)
          .Append("  ")
          .Append(group.Method)
          .Append("  ")
          .Append(caseText)
          .Append("  ")
          .Append(group.Count.ToString(CultureInfo.InvariantCulture))
          .Append("  ")
          .Append(group.P50Milliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append("  ")
          .Append(group.P95Milliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append("  ")
          .Append(group.MaxMilliseconds.ToString("0.0", CultureInfo.InvariantCulture))
          .Append("  ")
          .AppendLine(rate);
      }
    }

    return builder.ToString();
  }

  private static void WriteQuery(Utf8JsonWriter writer, string name, string filter, bool durations)
  {
    writer.WriteStartObject();
    writer.WriteString("type", "builder_query");
    writer.WriteStartObject("spec");
    writer.WriteString("name", name);
    writer.WriteString("signal", "traces");
    writer.WriteBoolean("disabled", false);
    writer.WriteStartArray("aggregations");
    if (durations)
    {
      WriteAggregation(writer, "count()", "count");
      WriteAggregation(writer, "p50(durationNano)", "p50");
      WriteAggregation(writer, "p95(durationNano)", "p95");
      WriteAggregation(writer, "max(durationNano)", "max");
    }
    else
    {
      WriteAggregation(writer, "count()", "failures");
    }

    writer.WriteEndArray();
    writer.WriteStartObject("filter");
    writer.WriteString("expression", filter);
    writer.WriteEndObject();
    writer.WriteStartArray("groupBy");
    WriteGroup(writer, "jaribu.test.class", "string");
    WriteGroup(writer, "jaribu.test.method", "string");
    WriteGroup(writer, "jaribu.test.case.index", "int64");
    writer.WriteEndArray();
    writer.WriteStartArray("order");
    writer.WriteStartObject();
    writer.WriteStartObject("key");
    writer.WriteString("name", durations ? "p95" : "failures");
    writer.WriteEndObject();
    writer.WriteString("direction", "desc");
    writer.WriteEndObject();
    writer.WriteEndArray();
    writer.WriteNumber("limit", QueryLimit);
    writer.WriteEndObject();
    writer.WriteEndObject();
  }

  private static void WriteAggregation(Utf8JsonWriter writer, string expression, string alias)
  {
    writer.WriteStartObject();
    writer.WriteString("expression", expression);
    writer.WriteString("alias", alias);
    writer.WriteEndObject();
  }

  private static void WriteGroup(Utf8JsonWriter writer, string name, string dataType)
  {
    writer.WriteStartObject();
    writer.WriteString("name", name);
    writer.WriteString("fieldContext", "attribute");
    writer.WriteString("fieldDataType", dataType);
    writer.WriteEndObject();
  }

  private static JsonElement FindResults(JsonElement root)
  {
    if (root.TryGetProperty("data", out JsonElement data))
    {
      if (data.TryGetProperty("data", out JsonElement inner) &&
          inner.TryGetProperty("results", out JsonElement nested) &&
          nested.ValueKind == JsonValueKind.Array)
      {
        return nested;
      }

      if (data.TryGetProperty("results", out JsonElement direct) && direct.ValueKind == JsonValueKind.Array)
      {
        return direct;
      }
    }

    throw new InvalidOperationException("SigNoz response has no results array.");
  }

  private static void ApplyRow(
    Dictionary<string, MutableGroup> groups,
    string query,
    List<string> headers,
    JsonElement row)
  {
    string className = Cell(row, headers, "jaribu.test.class");
    string method = Cell(row, headers, "jaribu.test.method");
    int? caseIndex = CaseIndex(row, headers);
    string key = className + "\n" + method + "\n" + (caseIndex?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
    if (!groups.TryGetValue(key, out MutableGroup? group))
    {
      group = new MutableGroup(className, method, caseIndex);
      groups.Add(key, group);
    }

    if (query == "B")
    {
      group.Failures = Number(row, headers, "failures", "count()");
      return;
    }

    group.Count = (long)Number(row, headers, "count", "count()");
    group.P50Milliseconds = NanosToMilliseconds(Number(row, headers, "p50", "p50(durationNano)"));
    group.P95Milliseconds = NanosToMilliseconds(Number(row, headers, "p95", "p95(durationNano)"));
    group.MaxMilliseconds = NanosToMilliseconds(Number(row, headers, "max", "max(durationNano)"));
  }

  private static string Cell(JsonElement row, List<string> headers, string name)
  {
    int index = headers.FindIndex(header => header.Equals(name, StringComparison.Ordinal));
    if (index < 0 || index >= row.GetArrayLength())
    {
      return string.Empty;
    }

    JsonElement value = row[index];
    return value.ValueKind switch
    {
      JsonValueKind.String => value.GetString() ?? string.Empty,
      JsonValueKind.Number => value.GetRawText(),
      JsonValueKind.Null => string.Empty,
      _ => string.Empty
    };
  }

  private static int? CaseIndex(JsonElement row, List<string> headers)
  {
    int index = headers.FindIndex(header => header.Equals("jaribu.test.case.index", StringComparison.Ordinal));
    if (index < 0 || index >= row.GetArrayLength())
    {
      return null;
    }

    JsonElement value = row[index];
    if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
    {
      return number;
    }

    if (value.ValueKind == JsonValueKind.String &&
        int.TryParse(value.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out int parsed))
    {
      return parsed;
    }

    return null;
  }

  private static double Number(JsonElement row, List<string> headers, string alias, string expression)
  {
    int index = headers.FindIndex(header =>
      header.Equals(alias, StringComparison.Ordinal) || header.Equals(expression, StringComparison.Ordinal));
    if (index < 0 || index >= row.GetArrayLength())
    {
      return 0;
    }

    JsonElement value = row[index];
    if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out double number))
    {
      return number;
    }

    if (value.ValueKind == JsonValueKind.String &&
        double.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed))
    {
      return parsed;
    }

    return 0;
  }

  private static double NanosToMilliseconds(double nanos)
  {
    return nanos / 1_000_000d;
  }

  private sealed class MutableGroup(string className, string method, int? caseIndex)
  {
    public string ClassName { get; } = className;

    public string Method { get; } = method;

    public int? CaseIndex { get; } = caseIndex;

    public long Count { get; set; }

    public double Failures { get; set; }

    public double P50Milliseconds { get; set; }

    public double P95Milliseconds { get; set; }

    public double MaxMilliseconds { get; set; }
  }
}

internal readonly record struct SignozGroup(
  string ClassName,
  string Method,
  int? CaseIndex,
  long Count,
  double P50Milliseconds,
  double P95Milliseconds,
  double MaxMilliseconds,
  double FailureRate);
