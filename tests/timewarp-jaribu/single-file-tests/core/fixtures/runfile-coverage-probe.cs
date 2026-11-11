#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Child process for the runfile coverage relaunch tests.
#endregion

#region Design
// JARIBU_COVERAGE_ACTIVE means dotnet-coverage (or the test stand-in) already
// launched this process. JARIBU_COVERAGE_PROBE_EXIT returns that code immediately.
// Otherwise the probe calls RunAllTests so the product code decides whether to relaunch.
#endregion

if (string.Equals(
  Environment.GetEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName),
  "1",
  StringComparison.Ordinal))
{
  string? probeExit = Environment.GetEnvironmentVariable("JARIBU_COVERAGE_PROBE_EXIT");
  if (!string.IsNullOrEmpty(probeExit))
  {
    return int.Parse(probeExit, CultureInfo.InvariantCulture);
  }
}

string mode = args.Length > 0 ? args[0] : "env";
string resultsDirectory = args.Length > 1 ? args[1] : RunfileCoverageOptions.DefaultResultsDirectory;
string? moduleFilter = args.Length > 2 ? args[2] : null;

if (mode == "api")
{
  return await RunAllTests(
    allowEmpty: true,
    coverage: new RunfileCoverageOptions
    {
      ResultsDirectory = resultsDirectory,
      ModuleFilter = moduleFilter
    });
}

return await RunAllTests(allowEmpty: true);
