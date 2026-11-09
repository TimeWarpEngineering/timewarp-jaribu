#region Purpose
// Shared repository-root lookup for dev-cli commands.
#endregion

#region Design
// Git.FindRoot walks from the working directory. Commands that cannot continue
// without a root report that once, through the injected terminal, and set the
// process exit code. Workflow keeps a binary-relative fallback when Find returns
// a directory that is not this repository.
#endregion

namespace DevCli;

internal static class RepoRoot
{
  public static string? Find() => Git.FindRoot();

  public static bool TryFind(ITerminal terminal, out string root)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    string? found = Find();
    if (found is null)
    {
      terminal.WriteErrorLine("Error: could not find repository root.");
      Environment.ExitCode = 1;
      root = string.Empty;
      return false;
    }

    root = found;
    return true;
  }
}
