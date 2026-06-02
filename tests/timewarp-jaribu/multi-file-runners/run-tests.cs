#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

// Multi-file test runner
// Runs all test files compiled together via Directory.Build.props.
// Test classes are auto-registered via [ModuleInitializer].

WriteLine("Running all Jaribu tests");
WriteLine();

return await RunAllTests();
