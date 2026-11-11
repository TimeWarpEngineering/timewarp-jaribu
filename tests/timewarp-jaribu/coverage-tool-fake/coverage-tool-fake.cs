#region Purpose
// Stands in for dotnet-coverage so the runfile coverage tests can see the relaunch.
#endregion

#region Design
// JARIBU_COVERAGE_FAKE_LOG receives the arguments, the working directory,
// JARIBU_COVERAGE_ACTIVE, and the settings file.
// `dotnet tool run dotnet-coverage -- …` is logged and not executed.
// Otherwise the command after `--` is the runner. A stub Cobertura file is
// written when that runner does not write one.
#endregion

string? logPath = Environment.GetEnvironmentVariable("JARIBU_COVERAGE_FAKE_LOG");
if (string.IsNullOrWhiteSpace(logPath))
{
  return 1;
}

string settings = string.Empty;
string output = string.Empty;
int separator = Array.IndexOf(args, "--");
for (int index = 0; index < args.Length; index++)
{
  if (args[index] == "-s" && index + 1 < args.Length && File.Exists(args[index + 1]))
  {
    settings = File.ReadAllText(args[index + 1]);
  }

  if (args[index] == "-o" && index + 1 < args.Length)
  {
    output = args[index + 1];
  }
}

string active = Environment.GetEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName) ?? string.Empty;
File.AppendAllText(
  logPath,
  "ACTIVE=" + active + Environment.NewLine
  + "PWD=" + Directory.GetCurrentDirectory() + Environment.NewLine
  + "ARGS" + Environment.NewLine
  + string.Join(Environment.NewLine, args)
  + Environment.NewLine
  + "SETTINGS"
  + Environment.NewLine
  + settings
  + Environment.NewLine
  + "END"
  + Environment.NewLine);

bool toolRun = args.Length >= 4
  && args[0] == "tool"
  && args[1] == "run"
  && args[2] == "dotnet-coverage"
  && args[3] == "--";
if (toolRun)
{
  return 0;
}

if (separator < 0 || separator + 1 >= args.Length)
{
  return 1;
}

string fileName = args[separator + 1];
string[] rest = args[(separator + 2)..];
int exitCode = await Shell.Builder(fileName)
  .WithArguments(rest)
  .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, active)
  .WithNoValidation()
  .RunAsync()
  .ConfigureAwait(false);

if (!string.IsNullOrEmpty(output))
{
  string? directory = Path.GetDirectoryName(Path.GetFullPath(output));
  if (!string.IsNullOrEmpty(directory))
  {
    Directory.CreateDirectory(directory);
  }

  if (!File.Exists(output))
  {
    File.WriteAllText(output, "<coverage />");
  }
}

return exitCode;
