#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:project $(SourceDirectory)timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj
#:property IsTestingPlatformApplication=false
#:property GenerateTestingPlatformEntryPoint=false
#:property GenerateTestingPlatformConfigurationFile=false
#:property GenerateSelfRegisteredExtensions=false

#region Purpose
// Runs the CI-safe multi-file suite from the shared compile list.
#endregion

#region Design
// mtp-node-identity.cs uses internal MtpSink. The testing platform names
// assembly run-tests (this file) in InternalsVisibleTo. The project reference
// flows Microsoft.Testing.Platform targets, which emit a second Main unless
// this runner opts out. The four properties match ci-runner's Directory.Build.props.
// They live on this file so the shared multi-file props do not disable MTP on mtp-runner.
#endregion

WriteLine("Running CI-safe Jaribu tests");
WriteLine();

return await RunAllTests();
