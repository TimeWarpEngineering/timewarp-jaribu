#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

// CI Test Runner
// Runs only CI-safe tests (no intentional failures).
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("CI Test Runner - Running CI-safe tests only");
WriteLine();

return await RunAllTests();
