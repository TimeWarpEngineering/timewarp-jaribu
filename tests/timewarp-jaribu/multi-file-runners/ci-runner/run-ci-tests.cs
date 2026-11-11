#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

// CI Test Runner
// Runs only CI-safe tests (no intentional failures).
// Test classes are auto-registered via [ModuleInitializer] when compiled with JARIBU_MULTI.

WriteLine("CI Test Runner - Running CI-safe tests only");
WriteLine();

// The report lives under artifacts/coverage/runfile so the MTP Cobertura file
// at artifacts/coverage/coverage.cobertura.xml stays in place. The workflow
// uploads artifacts/coverage, which includes this subdirectory.
// The default module filter excludes Jaribu by assembly file name. This repo's
// product assembly is Jaribu, so the CI report keeps tests and MSBuild nodes
// out and leaves timewarp-jaribu.dll in the report. The repository source rule
// still drops dependency assemblies such as CliWrap, Amuru, and Builder,
// including when a deterministic build records their sources as /_/.
return await RunAllTests(
  coverage: new RunfileCoverageOptions
  {
    ResultsDirectory = "artifacts/coverage/runfile",
    ModuleFilter = string.Join(
      ';',
      RunfileCoverageOptions.TestsModuleExclude,
      RunfileCoverageOptions.MsBuildModuleExclude,
      RunfileCoverageOptions.MicrosoftBuildModuleExclude)
  });
