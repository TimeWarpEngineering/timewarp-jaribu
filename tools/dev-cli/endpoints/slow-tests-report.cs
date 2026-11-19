#region Purpose
// Ranks a durations file and prints the slowest cases with their share of run time.
#endregion

#region Design
// Duration descending, then class, method, and case index. A null duration sorts
// last and does not add to the total. Cumulative percentage uses every timed row
// that passed the outcome filter, including rows past --top. The top-N share is
// that same total. Markdown and text carry the same numbers.
// Exit 0 belongs to the command: this type only builds the text.
#endregion

namespace DevCli.Commands;

using System.Globalization;
using System.Text;
using System.Text.Json;

internal static class SlowTestReport
{
  internal const int DefaultTop = 20;

  internal static readonly string[] Outcomes =
  [
    "passed",
    "failed",
    "skipped",
    "timed_out",
    "error",
    "cancelled"
  ];

  internal static bool TryParseTop(string? text, out int top, out string? error)
  {
    if (string.IsNullOrWhiteSpace(text))
    {
      top = DefaultTop;
      error = null;
      return true;
    }

    if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out top) || top < 1)
    {
      top = 0;
      error = "slow-tests --top expects a positive integer.";
      return false;
    }

    error = null;
    return true;
  }

  internal static bool TryParseFormat(string? text, out bool markdown, out string? error)
  {
    if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("text", StringComparison.OrdinalIgnoreCase))
    {
      markdown = false;
      error = null;
      return true;
    }

    if (text.Trim().Equals("markdown", StringComparison.OrdinalIgnoreCase))
    {
      markdown = true;
      error = null;
      return true;
    }

    markdown = false;
    error = "slow-tests --format expects text or markdown.";
    return false;
  }

  internal static bool TryParseOutcome(string? text, out string? outcome, out string? error)
  {
    if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
    {
      outcome = null;
      error = null;
      return true;
    }

    string trimmed = text.Trim();
    foreach (string known in Outcomes)
    {
      if (known.Equals(trimmed, StringComparison.OrdinalIgnoreCase))
      {
        outcome = known;
        error = null;
        return true;
      }
    }

    outcome = null;
    error = "slow-tests --outcome expects passed, failed, skipped, timed_out, error, cancelled, or all.";
    return false;
  }

  internal static IReadOnlyList<DurationRow> Read(string json)
  {
    ArgumentException.ThrowIfNullOrEmpty(json);
    using JsonDocument document = JsonDocument.Parse(json);
    if (!document.RootElement.TryGetProperty("records", out JsonElement records) ||
        records.ValueKind != JsonValueKind.Array)
    {
      throw new InvalidOperationException("Durations file has no records array.");
    }

    List<DurationRow> rows = [];
    foreach (JsonElement record in records.EnumerateArray())
    {
      rows.Add(
        new DurationRow(
          RequiredString(record, "class"),
          RequiredString(record, "method"),
          ReadCaseIndex(record),
          ReadOptionalString(record, "arguments"),
          RequiredString(record, "outcome"),
          ReadOptionalDouble(record, "durationMilliseconds"),
          ReadTags(record)));
    }

    return rows;
  }

  internal static string FormatLocal(
    IReadOnlyList<DurationRow> rows,
    int top,
    string? outcome,
    bool markdown,
    string? source)
  {
    ArgumentNullException.ThrowIfNull(rows);
    ArgumentOutOfRangeException.ThrowIfLessThan(top, 1);

    List<DurationRow> filtered = [];
    foreach (DurationRow row in rows)
    {
      if (outcome is null || row.Outcome.Equals(outcome, StringComparison.Ordinal))
      {
        filtered.Add(row);
      }
    }

    filtered.Sort(Compare);
    double total = 0;
    foreach (DurationRow row in filtered)
    {
      if (row.DurationMilliseconds is double milliseconds)
      {
        total += milliseconds;
      }
    }

    int shown = Math.Min(top, filtered.Count);
    double topDuration = 0;
    for (int index = 0; index < shown; index++)
    {
      if (filtered[index].DurationMilliseconds is double milliseconds)
      {
        topDuration += milliseconds;
      }
    }

    StringBuilder builder = new();
    if (!string.IsNullOrEmpty(source))
    {
      builder.Append("Source: ").AppendLine(source);
    }

    builder.AppendLine(ShareLine(shown, filtered.Count, topDuration, total));
    if (markdown)
    {
      AppendMarkdown(builder, filtered, shown, total);
    }
    else
    {
      AppendText(builder, filtered, shown, total);
    }

    return builder.ToString();
  }

  private static int Compare(DurationRow left, DurationRow right)
  {
    int duration = Nullable.Compare(right.DurationMilliseconds, left.DurationMilliseconds);
    if (duration != 0)
    {
      return duration;
    }

    int className = string.CompareOrdinal(left.ClassName, right.ClassName);
    if (className != 0)
    {
      return className;
    }

    int method = string.CompareOrdinal(left.Method, right.Method);
    if (method != 0)
    {
      return method;
    }

    return Nullable.Compare(left.CaseIndex, right.CaseIndex);
  }

  private static string ShareLine(int shown, int count, double topDuration, double total)
  {
    string share = Percent(total == 0 ? 0 : topDuration / total);
    return string.Concat(
      "Top ",
      shown.ToString(CultureInfo.InvariantCulture),
      " of ",
      count.ToString(CultureInfo.InvariantCulture),
      " hold ",
      share,
      " of ",
      Milliseconds(total),
      " ms.");
  }

  private static void AppendMarkdown(StringBuilder builder, List<DurationRow> rows, int shown, double total)
  {
    builder.AppendLine("| Class | Method | Case | Arguments | Outcome | Duration ms | Cumulative | Tags |");
    builder.AppendLine("| --- | --- | --- | --- | --- | ---: | ---: | --- |");
    double running = 0;
    for (int index = 0; index < shown; index++)
    {
      DurationRow row = rows[index];
      string cumulative = Cumulative(row, ref running, total);
      builder
        .Append("| ")
        .Append(Cell(row.ClassName))
        .Append(" | ")
        .Append(Cell(row.Method))
        .Append(" | ")
        .Append(Cell(CaseText(row.CaseIndex)))
        .Append(" | ")
        .Append(Cell(row.Arguments ?? string.Empty))
        .Append(" | ")
        .Append(Cell(row.Outcome))
        .Append(" | ")
        .Append(Cell(DurationText(row.DurationMilliseconds)))
        .Append(" | ")
        .Append(Cell(cumulative))
        .Append(" | ")
        .Append(Cell(string.Join(", ", row.Tags)))
        .AppendLine(" |");
    }
  }

  private static void AppendText(StringBuilder builder, List<DurationRow> rows, int shown, double total)
  {
    builder.AppendLine("Class  Method  Case  Arguments  Outcome  Duration ms  Cumulative  Tags");
    double running = 0;
    for (int index = 0; index < shown; index++)
    {
      DurationRow row = rows[index];
      builder
        .Append(row.ClassName)
        .Append("  ")
        .Append(row.Method)
        .Append("  ")
        .Append(CaseText(row.CaseIndex))
        .Append("  ")
        .Append(row.Arguments ?? "-")
        .Append("  ")
        .Append(row.Outcome)
        .Append("  ")
        .Append(DurationText(row.DurationMilliseconds))
        .Append("  ")
        .Append(Cumulative(row, ref running, total))
        .Append("  ")
        .AppendLine(row.Tags.Count == 0 ? "-" : string.Join(",", row.Tags));
    }
  }

  private static string Cumulative(DurationRow row, ref double running, double total)
  {
    if (row.DurationMilliseconds is not double milliseconds)
    {
      return string.Empty;
    }

    running += milliseconds;
    return Percent(total == 0 ? 0 : running / total);
  }

  private static string DurationText(double? milliseconds)
  {
    return milliseconds is double value ? Milliseconds(value) : string.Empty;
  }

  private static string CaseText(int? caseIndex)
  {
    return caseIndex is int index ? index.ToString(CultureInfo.InvariantCulture) : string.Empty;
  }

  private static string Milliseconds(double value)
  {
    return value.ToString("0.0", CultureInfo.InvariantCulture);
  }

  private static string Percent(double ratio)
  {
    return (ratio * 100).ToString("0.0", CultureInfo.InvariantCulture) + "%";
  }

  private static string Cell(string value)
  {
    return value.Replace("|", "\\|", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
  }

  private static string RequiredString(JsonElement record, string name)
  {
    if (!record.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String)
    {
      throw new InvalidOperationException("Durations record is missing " + name + ".");
    }

    return value.GetString() ?? string.Empty;
  }

  private static string? ReadOptionalString(JsonElement record, string name)
  {
    if (!record.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
    {
      return null;
    }

    if (value.ValueKind != JsonValueKind.String)
    {
      throw new InvalidOperationException("Durations record field " + name + " is not a string.");
    }

    return value.GetString();
  }

  private static int? ReadCaseIndex(JsonElement record)
  {
    if (!record.TryGetProperty("caseIndex", out JsonElement value) || value.ValueKind == JsonValueKind.Null)
    {
      return null;
    }

    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int index))
    {
      throw new InvalidOperationException("Durations record caseIndex is not an integer.");
    }

    return index;
  }

  private static double? ReadOptionalDouble(JsonElement record, string name)
  {
    if (!record.TryGetProperty(name, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
    {
      return null;
    }

    if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number))
    {
      throw new InvalidOperationException("Durations record field " + name + " is not a number.");
    }

    return number;
  }

  private static string[] ReadTags(JsonElement record)
  {
    if (!record.TryGetProperty("tags", out JsonElement value) || value.ValueKind == JsonValueKind.Null)
    {
      return [];
    }

    if (value.ValueKind != JsonValueKind.Array)
    {
      throw new InvalidOperationException("Durations record tags is not an array.");
    }

    List<string> tags = [];
    foreach (JsonElement tag in value.EnumerateArray())
    {
      if (tag.ValueKind != JsonValueKind.String)
      {
        throw new InvalidOperationException("Durations record tags must be strings.");
      }

      tags.Add(tag.GetString() ?? string.Empty);
    }

    return [.. tags];
  }
}

internal readonly record struct DurationRow(
  string ClassName,
  string Method,
  int? CaseIndex,
  string? Arguments,
  string Outcome,
  double? DurationMilliseconds,
  IReadOnlyList<string> Tags);
