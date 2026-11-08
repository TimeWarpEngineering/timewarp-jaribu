#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Out-of-process check that the validation project reports the expected node states.
#endregion

#region Design
// Each filter is a separate dotnet test of tests/timewarp-jaribu-mtp-validation.
// --output Detailed names every node. A timed-out node is the host line
// "failed (canceled)", and the summary counts it as failed.
// --list-tests prints discovered display names and does not run them.
// A blank JARIBU_FILTER_TAG is set on the child so a developer shell cannot change the table.
// SDK 10 publishes a dropped signature warning as an extra skipped node. That node is ignored.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Mtp_
{
  using System.Text.RegularExpressions;
  using TimeWarp.Amuru;

  [TestTag("Jaribu")]
  public partial class ValidationHarness_Given_
  {
    private const string PassingPath =
      "/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest";

    private static readonly ExpectedNode[] BasicNodes =
    [
      new("passed", "PassingTest"),
      new("passed", "AssertionPassingTest"),
      new("passed", "AsyncWithDelayTest"),
      new("failed", "FailingWithExceptionTest"),
      new("failed", "FailingWithAssertionTest"),
      new("skipped", "SkippedTest"),
      new("skipped", "SkippedFeatureTest"),
      new("failed (canceled)", "TimeoutExceededTest"),
      new("passed", "TimeoutNotExceededTest")
    ];

    private static readonly ExpectedNode[] EdgeCasesTagNodes =
    [
      new("failed", "NullReferenceExceptionTest"),
      new("failed", "ArgumentExceptionTest"),
      new("passed", "WorkThenPassTest"),
      new("passed", "MultipleAssertionsPassTest"),
      new("passed", "QuickOperationWithTimeoutTest"),
      new("skipped", "FailThenPass"),
      new("skipped", "PassingTest"),
      new("skipped", "AssertionPassingTest"),
      new("skipped", "AsyncWithDelayTest"),
      new("skipped", "FailingWithExceptionTest"),
      new("skipped", "FailingWithAssertionTest"),
      new("skipped", "SkippedTest"),
      new("skipped", "SkippedFeatureTest"),
      new("skipped", "TimeoutExceededTest"),
      new("skipped", "TimeoutNotExceededTest"),
      new("skipped", "SetupWasCalledTest"),
      new("skipped", "SetupCalledMultipleTimesTest"),
      new("skipped", "Real")
    ];

    [ModuleInitializer]
    internal static void Register() => RegisterTests<ValidationHarness_Given_>();

    public static async Task FilterTag_Should_MatchTheExpectedTable()
    {
      CommandOutput output = await RunValidationAsync(detailed: true, "--", "--filter-tag", "EdgeCases");
      AssertExit(output, 2);
      AssertSameNodes(ParseRun(output.Combined), EdgeCasesTagNodes, output.Combined);
    }

    public static async Task FilterClass_Should_MatchTheExpectedTable()
    {
      CommandOutput output = await RunValidationAsync(detailed: true, "--", "--filter-class", "Basic");
      AssertExit(output, 2);
      AssertSameNodes(ParseRun(output.Combined), BasicNodes, output.Combined);
    }

    public static async Task FilterMethod_Should_MatchTheExpectedTable()
    {
      ExpectedNode[] expected =
      [
        new("passed", "PassingTest"),
        new("passed", "AssertionPassingTest")
      ];
      CommandOutput output = await RunValidationAsync(detailed: true, "--", "--filter-method", "PassingTest");
      AssertExit(output, 0);
      AssertSameNodes(ParseRun(output.Combined), expected, output.Combined);
    }

    public static async Task TreeNodeFilter_Should_MatchTheExpectedTable()
    {
      ExpectedNode[] expected = [new("passed", "PassingTest")];
      CommandOutput output = await RunValidationAsync(detailed: true, "--", "--treenode-filter", PassingPath);
      AssertExit(output, 0);
      AssertSameNodes(ParseRun(output.Combined), expected, output.Combined);
    }

    public static async Task ListTests_Should_MatchTheExpectedTable()
    {
      CommandOutput basic = await RunValidationAsync(detailed: false, "--list-tests", "--", "--filter-class", "Basic");
      AssertExit(basic, 0);
      string[] expectedNames = [.. BasicNodes.Select(node => node.Name)];
      ListedNames(basic.Combined).ShouldBe(expectedNames, basic.Combined);
      DiscoveredCount(basic.Combined).ShouldBe(BasicNodes.Length);

      CommandOutput tagged = await RunValidationAsync(
        detailed: false,
        "--list-tests",
        "--",
        "--filter-class",
        "InputIdentity",
        "--filter-tag",
        "EdgeCases");
      AssertExit(tagged, 0);
      ListedNames(tagged.Combined).ShouldBe(["FailThenPass"], tagged.Combined);
      DiscoveredCount(tagged.Combined).ShouldBe(1);
    }

    private static void AssertExit(CommandOutput output, int exitCode)
    {
      output.ExitCode.ShouldBe(exitCode, Normalize(output.Combined));
    }

    private static void AssertSameNodes(
      IReadOnlyList<ReportedNode> actual,
      IReadOnlyList<ExpectedNode> expected,
      string output)
    {
      List<string> actualText =
      [
        .. actual
          .Where(node => !IsHostSignatureWarning(node))
          .Select(node => $"{node.State} {node.Name}")
          .Order(StringComparer.Ordinal)
      ];
      List<string> expectedText =
      [
        .. expected
          .Select(node => $"{node.State} {node.Name}")
          .Order(StringComparer.Ordinal)
      ];
      actualText.ShouldBe(expectedText, Normalize(output));
    }

    private static bool IsHostSignatureWarning(ReportedNode node)
    {
      return node.Name.Contains("Near-miss", StringComparison.Ordinal) ||
        node.Name.Contains("ReturnsTaskOfInt", StringComparison.Ordinal);
    }

    private static List<ReportedNode> ParseRun(string output)
    {
      List<ReportedNode> nodes = [];
      foreach (string line in Normalize(output).Split('\n'))
      {
        Match match = RunNode().Match(line);
        if (!match.Success)
        {
          continue;
        }

        nodes.Add(new ReportedNode(match.Groups[1].Value, match.Groups[2].Value));
      }

      return nodes;
    }

    private static string[] ListedNames(string output)
    {
      List<string> names = [];
      foreach (string line in Normalize(output).Split('\n'))
      {
        Match match = ListedName().Match(line);
        if (match.Success)
        {
          names.Add(match.Groups[1].Value);
        }
      }

      return [.. names];
    }

    private static int DiscoveredCount(string output)
    {
      Match match = DiscoveredLine().Match(Normalize(output));
      match.Success.ShouldBeTrue(Normalize(output));
      return int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
    }

    private static string Normalize(string output)
    {
      string withoutCarriageReturns = output.Replace("\r", "", StringComparison.Ordinal);
      return AnsiEscape().Replace(withoutCarriageReturns, "");
    }

    private static Task<CommandOutput> RunValidationAsync(bool detailed, params string[] arguments)
    {
      string root = FindRepoRoot();
      string project = Path.Combine(
        root,
        "tests",
        "timewarp-jaribu-mtp-validation",
        "timewarp-jaribu-mtp-validation.csproj");
      List<string> allArguments = ["test", project, "--nologo", "--no-ansi"];
      if (detailed)
      {
        allArguments.Add("--output");
        allArguments.Add("Detailed");
      }

      allArguments.AddRange(arguments);
      return Shell.Builder("dotnet")
        .WithArguments([.. allArguments])
        .WithWorkingDirectory(root)
        .WithEnvironmentVariable("JARIBU_FILTER_TAG", "")
        .WithNoValidation()
        .RunAndCaptureAsync();
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

    [GeneratedRegex(
      @"^(passed|failed \(canceled\)|failed|skipped|timeout|error|cancelled) ([A-Za-z_][A-Za-z0-9_]*(?:\([^)]*\))?)(?: \(\d+ms\))?$",
      RegexOptions.CultureInvariant)]
    private static partial Regex RunNode();

    [GeneratedRegex(@"^  ([A-Za-z_][A-Za-z0-9_]*(?:\([^)]*\))?)$", RegexOptions.CultureInvariant)]
    private static partial Regex ListedName();

    [GeneratedRegex(@"^Discovered (\d+) tests\.$", RegexOptions.CultureInvariant | RegexOptions.Multiline)]
    private static partial Regex DiscoveredLine();

    [GeneratedRegex(@"\x1b\[[0-9;]*m", RegexOptions.CultureInvariant)]
    private static partial Regex AnsiEscape();

    private readonly record struct ExpectedNode(string State, string Name);

    private readonly record struct ReportedNode(string State, string Name);
  }
}
