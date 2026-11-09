#!/usr/bin/env -S dotnet --
#:package TimeWarp.Amuru
#:package TimeWarp.Amuru.Tools
#:property NoWarn=CA2007;CA1849;RS0030
#:property RunAnalyzers=false

// Dispatcher for branch checkouts only ($3 == 1): ganda repo attest.
// A file checkout returns 0. A branch checkout with no repo root, or a failed
// attest, returns 1 so the miss is not silent.
using TimeWarp.Amuru;

// git post-checkout <previous> <new> <branch_flag>
// branch_flag is 1 for branch checkout, 0 for file checkout.
if (args.Length < 3 || args[2] != "1")
{
  return 0;
}

string? root = Git.FindRoot();
if (root is null)
{
#pragma warning disable RS0030, CA1849
  Console.Error.WriteLine("post-checkout: repository root was not found.");
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
