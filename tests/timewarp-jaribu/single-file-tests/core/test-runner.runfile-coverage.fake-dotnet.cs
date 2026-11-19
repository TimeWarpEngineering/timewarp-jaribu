#region Purpose
// Places a fake dotnet next to the coverage-tool fake so a relaunch can find it.
#endregion

#region Design
// Windows copies the fake and aliases coverage-tool-fake.* to dotnet.*, because an
// apphost loads its dll from beside the executable. Other systems use a symlink.
// why: task 075, split out of launch.cs to stay under the 500-line limit
#endregion

namespace TestRunner_;

public partial class RunfileCoverage_Given_
{
    private static string LinkFakeDotnet(string toolPath)
    {
      // A `dotnet` name next to the fake would steal the relaunch's `dotnet` lookup.
      string directory = Path.Combine(Path.GetTempPath(), "jaribu-fake-dotnet");
      System.IO.Directory.CreateDirectory(directory);
      string linkName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
      string link = Path.Combine(directory, linkName);
      if (File.Exists(link))
      {
        File.Delete(link);
      }

      try
      {
        if (OperatingSystem.IsWindows())
        {
          // An apphost finds its dll beside the link, not beside the target, so copy.
          CopyFakeBesideDotnet(toolPath, directory, link);
        }
        else
        {
          File.CreateSymbolicLink(link, toolPath);
        }
      }
      catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
      {
        CopyFakeBesideDotnet(toolPath, directory, link);
      }

      return directory;
    }

    private static void CopyFakeBesideDotnet(string toolPath, string directory, string link)
    {
      string? sourceDirectory = Path.GetDirectoryName(toolPath);
      if (string.IsNullOrWhiteSpace(sourceDirectory))
      {
        throw new InvalidOperationException($"Coverage fake has no directory: '{toolPath}'.");
      }

      foreach (string file in System.IO.Directory.GetFiles(sourceDirectory))
      {
        File.Copy(file, Path.Combine(directory, Path.GetFileName(file)), overwrite: true);
      }

      if (!File.Exists(link))
      {
        File.Copy(toolPath, link, overwrite: true);
      }

      // Some apphosts load <exe-name>.* from beside the executable. The built
      // names are coverage-tool-fake.*; the relaunch executable is dotnet.exe.
      CopyAlias(sourceDirectory, directory, ".dll");
      CopyAlias(sourceDirectory, directory, ".deps.json");
      CopyAlias(sourceDirectory, directory, ".runtimeconfig.json");
    }

    private static void CopyAlias(string sourceDirectory, string directory, string extension)
    {
      if (!OperatingSystem.IsWindows())
      {
        return;
      }

      string source = Path.Combine(sourceDirectory, "coverage-tool-fake" + extension);
      if (!File.Exists(source))
      {
        return;
      }

      File.Copy(source, Path.Combine(directory, "dotnet" + extension), overwrite: true);
    }

    private static string FindDotnet()
    {
      string? path = Environment.GetEnvironmentVariable("PATH");
      if (string.IsNullOrEmpty(path))
      {
        throw new InvalidOperationException("PATH is empty.");
      }

      string fileName = OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet";
      foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
      {
        string candidate = Path.Combine(directory, fileName);
        if (File.Exists(candidate))
        {
          return candidate;
        }
      }

      throw new InvalidOperationException("dotnet was not found on PATH.");
    }
}
