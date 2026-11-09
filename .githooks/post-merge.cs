#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru
#:package TimeWarp.Amuru.Tools
#:property NoWarn=CA2007;CA1849;RS0030
#:property RunAnalyzers=false

// Dispatcher: ganda repo attest.
// A failed attest is reported. The merge already completed, and the non-zero
// exit is the signal that the attestation is missing.
using TimeWarp.Amuru;

string? root = Git.FindRoot();
if (root is null)
{
#pragma warning disable RS0030, CA1849
  Console.Error.WriteLine("post-merge: repository root was not found.");
#pragma warning restore RS0030, CA1849
  return 1;
}

if (!string.Equals(Environment.GetEnvironmentVariable("GANDA_ATTEST_HOOK"), "0", StringComparison.Ordinal))
{
  CommandOutput result = await Shell.Builder("ganda")
    .WithArguments("repo", "attest")
    .WithWorkingDirectory(root)
    .WithNoValidation()
    .CaptureAsync();
  if (!result.Success)
  {
#pragma warning disable RS0030, CA1849
    Console.Error.WriteLine("ganda repo attest failed.");
    if (!string.IsNullOrWhiteSpace(result.Combined))
    {
      Console.Error.WriteLine(result.Combined);
    }
#pragma warning restore RS0030, CA1849
    return 1;
  }
}

return 0;
