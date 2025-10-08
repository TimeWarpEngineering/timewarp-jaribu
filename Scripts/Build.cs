#!/usr/bin/dotnet --

// Build.cs - Build the TimeWarp.Jaribu library

// Change to script directory for relative paths
string scriptDir = (AppContext.GetData("EntryPointFileDirectoryPath") as string)!;
Directory.SetCurrentDirectory(scriptDir);

WriteLine("Building TimeWarp.Jaribu library...");
WriteLine($"Working from: {Directory.GetCurrentDirectory()}");

// Build each project individually to avoid framework resolution issues
string[] projectsToBuild = [
  "../Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj"
];

try
{
  foreach (string projectPath in projectsToBuild)
  {
    WriteLine($"Building {projectPath}...");
    CommandResult buildCommandResult = DotNet.Build()
      .WithProject(projectPath)
      .WithConfiguration("Release")
      .WithVerbosity("minimal")
      .Build();

    WriteLine("Running ...");
    WriteLine(buildCommandResult.ToCommandString());

    if (await buildCommandResult.RunAsync() != 0)
    {
      WriteLine($"❌ Failed to build {projectPath}!");
      Environment.Exit(1);
    }
  }
}
catch (Exception ex)
{
  WriteLine("=== Exception Details ===");
  WriteLine($"Exception type: {ex.GetType().Name}");
  WriteLine($"Message: {ex.Message}");

  if (ex.InnerException is not null)
  {
    WriteLine($"Inner exception type: {ex.InnerException.GetType().Name}");
    WriteLine($"Inner exception message: {ex.InnerException.Message}");
  }

  WriteLine($"Stack trace: {ex.StackTrace}");
  WriteLine("❌ Build failed with exception!");
  Environment.Exit(1);
}