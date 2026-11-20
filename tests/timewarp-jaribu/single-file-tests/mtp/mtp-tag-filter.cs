#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Proves multi-tag include and exclude through dotnet test.
#endregion

#region Design
// Each selection is a separate dotnet test of the validation assembly.
// A blank JARIBU_FILTER_TAG and JARIBU_EXCLUDE_TAG are set unless the call
// sets that variable, so a developer shell cannot change the table.
// The skipped-node message is the text dotnet test prints.
// Each child has a four-minute deadline. The method timeout covers four children.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Mtp_
{
  using System.Text.RegularExpressions;
  using TimeWarp.Amuru;

  /// <summary>Include-any and exclude-any on the Microsoft.Testing.Platform path.</summary>
  [TestTag("Jaribu")]
  public partial class TagFilter_Given_
  {
    private const int FourChildTimeoutMilliseconds = 1_200_000;

    private static readonly TimeSpan ChildDeadline = TimeSpan.FromMinutes(4);

    [ModuleInitializer]
    internal static void Register() => RegisterTests<TagFilter_Given_>();

    /// <summary>Repeated flags and environment comma lists skip with the filter's own text.</summary>
    [Timeout(FourChildTimeoutMilliseconds)]
    public static async Task FilterTags_Should_IncludeAnyThenExclude()
    {
      CommandOutput includeAny = await RunValidationAsync(
        ["--", "--filter-tag", "MtpValidation", "--filter-tag", "EdgeCases"]);
      string includeText = Normalize(includeAny.Combined);
      includeText.ShouldContain("failed FailingWithExceptionTest");
      includeText.ShouldContain("failed NullReferenceExceptionTest");
      includeText.ShouldContain("skipped SetupWasCalledTest");
      includeText.ShouldContain("No matching tag 'MtpValidation', 'EdgeCases'");

      CommandOutput excluded = await RunValidationAsync(
        ["--", "--exclude-tag", "EdgeCases", "--filter-class", "EdgeCase"]);
      string excludedText = Normalize(excluded.Combined);
      excludedText.ShouldContain("skipped NullReferenceExceptionTest");
      excludedText.ShouldContain("excluded by tag \"EdgeCases\"");
      excludedText.ShouldNotContain("failed NullReferenceExceptionTest");
      SummaryCount(excludedText, "succeeded").ShouldBe(0);

      CommandOutput both = await RunValidationAsync(
        ["--", "--filter-tag", "MtpValidation", "--exclude-tag", "MtpValidation", "--filter-class", "Basic"]);
      string bothText = Normalize(both.Combined);
      bothText.ShouldContain("skipped PassingTest");
      bothText.ShouldContain("skipped FailingWithExceptionTest");
      bothText.ShouldContain("excluded by tag \"MtpValidation\"");
      bothText.ShouldNotContain("failed FailingWithExceptionTest");

      CommandOutput environment = await RunValidationAsync(
        ["--", "--filter-class", "Tests"],
        ("JARIBU_FILTER_TAG", "MtpValidation,EdgeCases"),
        ("JARIBU_EXCLUDE_TAG", "EdgeCases"));
      string environmentText = Normalize(environment.Combined);
      environmentText.ShouldContain("failed FailingWithExceptionTest");
      environmentText.ShouldContain("skipped NullReferenceExceptionTest");
      environmentText.ShouldContain("excluded by tag \"EdgeCases\"");
      environmentText.ShouldContain("skipped SetupWasCalledTest");
      environmentText.ShouldContain("No matching tag 'MtpValidation', 'EdgeCases'");
      await Task.CompletedTask;
    }

    private static string Normalize(string output)
    {
      string withoutCarriageReturns = output.Replace("\r", "", StringComparison.Ordinal);
      return AnsiEscape().Replace(withoutCarriageReturns, "");
    }

    [GeneratedRegex(@"\x1b\[[0-9;]*m", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiEscape();

    private static int SummaryCount(string output, string label)
    {
      Match match = Regex.Match(output, $@"^\s*{label}: (\d+)\s*$", RegexOptions.Multiline);
      match.Success.ShouldBeTrue(output);
      return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static Task<CommandOutput> RunValidationAsync(params string[] arguments)
    {
      return RunValidationAsync(arguments, environment: []);
    }

    private static async Task<CommandOutput> RunValidationAsync(
      string[] arguments,
      params (string Name, string Value)[] environment)
    {
      string root = FindRepoRoot();
      string project = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu-mtp-validation",
        "timewarp-jaribu-mtp-validation.csproj");

      DotNetTestBuilder builder = DotNet.Test()
        .WithProject(project)
        .WithNoLogo()
        .WithArguments(arguments)
        .WithWorkingDirectory(root)
        .WithNoValidation()
        .WithTimeout(ChildDeadline);

      bool sawFilter = false;
      bool sawExclude = false;
      foreach ((string name, string value) in environment)
      {
        if (name == "JARIBU_FILTER_TAG")
        {
          sawFilter = true;
        }

        if (name == "JARIBU_EXCLUDE_TAG")
        {
          sawExclude = true;
        }

        builder = builder.WithEnvironmentVariable(name, value);
      }

      if (!sawFilter)
      {
        builder = builder.WithEnvironmentVariable("JARIBU_FILTER_TAG", "");
      }

      if (!sawExclude)
      {
        builder = builder.WithEnvironmentVariable("JARIBU_EXCLUDE_TAG", "");
      }

      CommandOutput output = await builder.CaptureAsync();
      if (output.ExitCode == CommandResult.TimeoutExitCode)
      {
        throw new TimeoutException
        (
          $"dotnet test exceeded {ChildDeadline}: {string.Join(' ', arguments)}{Environment.NewLine}{Normalize(output.Combined)}"
        );
      }

      return output;
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

      throw new InvalidOperationException(
        $"Repository root not found from '{Directory.GetCurrentDirectory()}'.");
    }
  }
}
