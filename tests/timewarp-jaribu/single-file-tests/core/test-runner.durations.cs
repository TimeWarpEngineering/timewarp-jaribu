#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Proves the runner writes one durations file per outermost run.
#endregion

#region Design
// The file is written when the outermost run returns, so this test cannot
// read a file produced by its own suite. A child process runs the probe.
// multi covers both classes. single covers one class and the default path.
// JARIBU_DURATIONS=0 writes nothing. A skipped row has outcome skipped and
// a null duration.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Text.Json;
  using TimeWarp.Amuru;

  [TestTag("Jaribu")]
  public class Durations_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Durations_Given_>();

    [KnownTautology("child process asserts the durations file")]
    public static async Task MultiFile_Should_RecordEveryCaseAndLeaveSkippedUntimed()
    {
      string path = Path.Combine(Path.GetTempPath(), "jaribu-durations-" + Guid.NewGuid().ToString("N") + ".json");
      CommandOutput output = await LaunchAsync("multi", path, optOut: false, workingDirectory: FindRepoRoot());
      output.ExitCode.ShouldBe(0, output.Combined);
      File.Exists(path).ShouldBeTrue(output.Combined);

      List<RecordedCase> records = Read(path);
      records.Count.ShouldBe(5);
      RecordedCase passed = One(records, "Should_Pass", caseIndex: null);
      passed.Outcome.ShouldBe("passed");
      passed.DurationMilliseconds.ShouldNotBeNull();
      passed.Tags.ShouldBe(["Suite", "Fast"]);

      RecordedCase skipped = One(records, "Should_Skip", caseIndex: null);
      skipped.Outcome.ShouldBe("skipped");
      skipped.DurationMilliseconds.ShouldBeNull();
      skipped.Tags.ShouldBe(["Suite"]);

      RecordedCase first = One(records, "Should_Echo", caseIndex: 0);
      first.Arguments.ShouldBe("\"one\"");
      first.Outcome.ShouldBe("passed");
      RecordedCase second = One(records, "Should_Echo", caseIndex: 1);
      second.Arguments.ShouldBe("\"two\"");
      One(records, "Should_AlsoPass", caseIndex: null).Tags.ShouldBeEmpty();
    }

    [KnownTautology("child process asserts the durations file")]
    public static async Task SingleFile_Should_WriteTheDefaultPathForThatClassOnly()
    {
      string directory = Path.Combine(Path.GetTempPath(), "jaribu-durations-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(directory);
      CommandOutput output = await LaunchAsync("single", durationsPath: null, optOut: false, directory);
      output.ExitCode.ShouldBe(0, output.Combined);
      string[] files = Directory.GetFiles(Path.Combine(directory, "artifacts", "jaribu"), "durations-*.json");
      files.Length.ShouldBe(1, output.Combined);
      List<RecordedCase> records = Read(files[0]);
      records.Count.ShouldBe(4);
      records.ShouldNotContain(record => record.Method == "Should_AlsoPass");
      One(records, "Should_Skip", caseIndex: null).DurationMilliseconds.ShouldBeNull();
    }

    [KnownTautology("child process asserts the durations file")]
    public static async Task OptOut_Should_WriteNothing()
    {
      string path = Path.Combine(Path.GetTempPath(), "jaribu-durations-" + Guid.NewGuid().ToString("N") + ".json");
      CommandOutput output = await LaunchAsync("multi", path, optOut: true, workingDirectory: FindRepoRoot());
      output.ExitCode.ShouldBe(0, output.Combined);
      File.Exists(path).ShouldBeFalse(output.Combined);
    }

    private static RecordedCase One(List<RecordedCase> records, string method, int? caseIndex)
    {
      return records.Single(record => record.Method == method && record.CaseIndex == caseIndex);
    }

    private static List<RecordedCase> Read(string path)
    {
      using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
      JsonElement records = document.RootElement.GetProperty("records");
      List<RecordedCase> rows = [];
      foreach (JsonElement record in records.EnumerateArray())
      {
        int? caseIndex = record.GetProperty("caseIndex").ValueKind == JsonValueKind.Null
          ? null
          : record.GetProperty("caseIndex").GetInt32();
        double? duration = record.GetProperty("durationMilliseconds").ValueKind == JsonValueKind.Null
          ? null
          : record.GetProperty("durationMilliseconds").GetDouble();
        string? arguments = record.GetProperty("arguments").ValueKind == JsonValueKind.Null
          ? null
          : record.GetProperty("arguments").GetString();
        List<string> tags = [];
        foreach (JsonElement tag in record.GetProperty("tags").EnumerateArray())
        {
          tags.Add(tag.GetString() ?? string.Empty);
        }

        rows.Add(new RecordedCase(
          record.GetProperty("class").GetString() ?? string.Empty,
          record.GetProperty("method").GetString() ?? string.Empty,
          caseIndex,
          arguments,
          record.GetProperty("outcome").GetString() ?? string.Empty,
          duration,
          tags));
      }

      return rows;
    }

    private static Task<CommandOutput> LaunchAsync(
      string mode,
      string? durationsPath,
      bool optOut,
      string workingDirectory)
    {
      string root = FindRepoRoot();
      string probe = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu",
        "single-file-tests",
        "core",
        "fixtures",
        "durations-probe.cs");
      return DotNet.Builder()
        .WithArguments("run", probe, "--", mode)
        .WithWorkingDirectory(workingDirectory)
        .WithEnvironmentVariable("JARIBU_DURATIONS", optOut ? "0" : string.Empty)
        .WithEnvironmentVariable("JARIBU_DURATIONS_PATH", durationsPath ?? string.Empty)
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3))
        .CaptureAsync();
    }

    private static string FindRepoRoot()
    {
      DirectoryInfo? directory = new(Directory.GetCurrentDirectory());
      while (directory is not null)
      {
        if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      throw new DirectoryNotFoundException("timewarp-jaribu.slnx was not found.");
    }

    private sealed record RecordedCase(
      string ClassName,
      string Method,
      int? CaseIndex,
      string? Arguments,
      string Outcome,
      double? DurationMilliseconds,
      List<string> Tags);
  }
}
