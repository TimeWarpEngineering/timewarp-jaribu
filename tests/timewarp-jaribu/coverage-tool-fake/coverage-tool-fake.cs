#region Purpose
// Stands in for dotnet-coverage so the runfile coverage tests can see the relaunch.
#endregion

#region Design
// JARIBU_COVERAGE_FAKE_LOG receives the arguments and the settings file.
// The command after `--` is the runner. A stub Cobertura file is written when
// that runner does not write one, which is this stand-in rather than dotnet-coverage.
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

File.AppendAllText(
  logPath,
  "ARGS" + Environment.NewLine
  + string.Join(Environment.NewLine, args)
  + Environment.NewLine
  + "SETTINGS"
  + Environment.NewLine
  + settings
  + Environment.NewLine
  + "END"
  + Environment.NewLine);

if (separator < 0 || separator + 1 >= args.Length)
{
  return 1;
}

string fileName = args[separator + 1];
string[] rest = args[(separator + 2)..];
int exitCode = await Shell.Builder(fileName)
  .WithArguments(rest)
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
