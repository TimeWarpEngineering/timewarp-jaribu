#region Purpose
// Host launched as `dotnet coverage-muxer-host.dll` so runfile coverage keeps argv[0].
#endregion

#region Design
// File-based apps set EntryPointFilePath and relaunch `dotnet <file.cs>`.
// This program has no entry-point file. ProcessPath is the dotnet muxer and
// argv[0] is this dll. RunAllTests must relaunch `dotnet <this dll> …`.
#endregion

if (args.Length == 0 || string.IsNullOrWhiteSpace(args[0]))
{
  return 1;
}

return await RunAllTests(
  allowEmpty: true,
  coverage: new RunfileCoverageOptions
  {
    ResultsDirectory = args[0]
  });
