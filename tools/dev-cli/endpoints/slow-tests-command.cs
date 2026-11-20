#region Purpose
// dev slow-tests prints the slowest recorded cases from a local file or from SigNoz.
#endregion

#region Design
// Local mode exits 0. A missing file is still a report. Option values this command
// does not understand exit 2, and so does a SigNoz call that cannot return rows.
// Missing SIGNOZ_URL or SIGNOZ_API_KEY prints one line and exits 2.
// An empty durations file prints one line and exits 0.
// The newest durations file is the latest write under the repo's artifacts/jaribu.
// The writer's default path is relative to the test process working directory.
// dev test points JARIBU_DURATIONS_PATH at that repo directory. --file covers
// a run that wrote somewhere else. --outcome limits SigNoz duration groups.
// The failure-rate column is blank when it is set, because that count is no
// longer every run.
#endregion

namespace DevCli.Commands;

using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

[NuruRoute("slow-tests", Description = "Rank tests by recorded duration")]
[NuruRouteExample("slow-tests", Description = "Top 20 from the newest durations file")]
[NuruRouteExample("slow-tests --top 10 --format markdown --outcome passed")]
[NuruRouteExample("slow-tests --signoz --since 7d")]
internal sealed class SlowTestsCommand : ICommand<Unit>
{
  [Option("top", Description = "Rows to print. Default is 20.")]
  public string? Top { get; set; }

  [Option("file", Description = "Durations JSON. Default is the newest artifacts/jaribu/durations-*.json.")]
  public string? File { get; set; }

  [Option("format", Description = "text or markdown. Default is text.")]
  public string? Format { get; set; }

  [Option("outcome", Description = "passed, failed, skipped, timed_out, error, cancelled, or all.")]
  public string? Outcome { get; set; }

  [Option("signoz", Description = "Rank jaribu.test spans from SigNoz instead of a local file.")]
  public bool Signoz { get; set; }

  [Option("since", Description = "SigNoz window, such as 7d, 24h, or 30m. Default is 7d.")]
  public string? Since { get; set; }

  internal sealed class Handler : IRequestHandler<SlowTestsCommand, Unit>
  {
    private readonly ITerminal Terminal;

    public Handler(ITerminal terminal)
    {
      Terminal = terminal;
    }

    public async ValueTask<Unit> Handle(SlowTestsCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      if (!SlowTestReport.TryParseTop(command.Top, out int top, out string? topError))
      {
        return Fail(topError);
      }

      if (!SlowTestReport.TryParseFormat(command.Format, out bool markdown, out string? formatError))
      {
        return Fail(formatError);
      }

      if (!SlowTestReport.TryParseOutcome(command.Outcome, out string? outcome, out string? outcomeError))
      {
        return Fail(outcomeError);
      }

      if (command.Signoz)
      {
        await QuerySignozAsync(command.Since, top, outcome, markdown, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
      }

      PrintLocal(command.File, top, outcome, markdown);
      return Unit.Value;
    }

    private void PrintLocal(string? file, int top, string? outcome, bool markdown)
    {
      if (!TryResolveFile(file, out string? path, out string? message))
      {
        Terminal.WriteLine(message ?? "No durations file found.");
        return;
      }

      string json;
      try
      {
        json = System.IO.File.ReadAllText(path);
      }
      catch (IOException exception)
      {
        Terminal.WriteLine("Could not read " + path + ": " + exception.Message);
        return;
      }
      catch (UnauthorizedAccessException exception)
      {
        Terminal.WriteLine("Could not read " + path + ": " + exception.Message);
        return;
      }

      IReadOnlyList<DurationRow> rows;
      try
      {
        rows = SlowTestReport.Read(json);
      }
      catch (JsonException exception)
      {
        Terminal.WriteLine("Could not parse " + path + ": " + exception.Message);
        return;
      }
      catch (InvalidOperationException exception)
      {
        Terminal.WriteLine(exception.Message);
        return;
      }

      Terminal.Write(SlowTestReport.FormatLocal(rows, top, outcome, markdown, path));
    }

    private static bool TryResolveFile(string? file, out string path, out string? message)
    {
      if (!string.IsNullOrWhiteSpace(file))
      {
        path = Path.GetFullPath(file);
        if (!System.IO.File.Exists(path))
        {
          message = "No durations file at " + path + ".";
          path = string.Empty;
          return false;
        }

        message = null;
        return true;
      }

      string root = RepoRoot.Find() ?? Directory.GetCurrentDirectory();
      string directory = Path.Combine(root, "artifacts", "jaribu");
      if (!Directory.Exists(directory))
      {
        path = string.Empty;
        message = "No durations file in " + directory + ". Run tests, or pass --file.";
        return false;
      }

      FileInfo? newest = null;
      foreach (string candidate in Directory.EnumerateFiles(directory, "durations-*.json"))
      {
        FileInfo info = new(candidate);
        if (newest is null ||
            info.LastWriteTimeUtc > newest.LastWriteTimeUtc ||
            (info.LastWriteTimeUtc == newest.LastWriteTimeUtc &&
             string.CompareOrdinal(info.Name, newest.Name) > 0))
        {
          newest = info;
        }
      }

      if (newest is null)
      {
        path = string.Empty;
        message = "No durations file in " + directory + ". Run tests, or pass --file.";
        return false;
      }

      path = newest.FullName;
      message = null;
      return true;
    }

    private async Task QuerySignozAsync(
      string? since,
      int top,
      string? outcome,
      bool markdown,
      CancellationToken cancellationToken)
    {
      if (!SignozSlowTests.TryParseSince(since, out TimeSpan window, out string? sinceError))
      {
        Fail(sinceError);
        return;
      }

      string? url = Environment.GetEnvironmentVariable(SignozSlowTests.UrlVariable);
      string? key = Environment.GetEnvironmentVariable(SignozSlowTests.KeyVariable);
      string? missing = SignozSlowTests.RequirementMessage(url, key);
      if (missing is not null)
      {
        Fail(missing);
        return;
      }

      DateTimeOffset end = DateTimeOffset.UtcNow;
      DateTimeOffset start = end - window;
      string body = SignozSlowTests.BuildRequestJson(start.ToUnixTimeMilliseconds(), end.ToUnixTimeMilliseconds(), outcome);
      string endpoint = SignozSlowTests.Endpoint(url!);
      using HttpRequestMessage request = new(HttpMethod.Post, endpoint);
      request.Headers.TryAddWithoutValidation("SIGNOZ-API-KEY", key);
      request.Content = new StringContent(body, Encoding.UTF8, "application/json");
      using HttpClient client = new()
      {
        Timeout = TimeSpan.FromSeconds(30)
      };

      HttpResponseMessage response;
      try
      {
        response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
      }
      catch (HttpRequestException exception)
      {
        Fail("SigNoz query failed: " + exception.Message);
        return;
      }
      catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
      {
        Fail("SigNoz query timed out.");
        return;
      }

      using (response)
      {
        string payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
          Fail("SigNoz query returned HTTP " + ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture) + ".");
          return;
        }

        IReadOnlyList<SignozGroup> groups;
        try
        {
          groups = SignozSlowTests.ParseResponse(payload);
        }
        catch (JsonException exception)
        {
          Fail("SigNoz response could not be parsed: " + exception.Message);
          return;
        }
        catch (InvalidOperationException exception)
        {
          Fail(exception.Message);
          return;
        }

        string label = string.IsNullOrWhiteSpace(since) ? "7d" : since.Trim();
        Terminal.Write(SignozSlowTests.Format(groups, top, markdown, label, showFailureRate: outcome is null));
      }
    }

    private Unit Fail(string? message)
    {
      Terminal.WriteErrorLine(message ?? "slow-tests could not run.");
      Environment.ExitCode = 2;
      return Unit.Value;
    }
  }
}
