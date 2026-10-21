#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

// Multi-file test runner
// Compiles the CI-safe set in ci-safe-tests.props (same list as ci-runner and mtp-runner).
// Test classes are auto-registered via [ModuleInitializer].

WriteLine("Running CI-safe Jaribu tests");
WriteLine();

return await RunAllTests();
