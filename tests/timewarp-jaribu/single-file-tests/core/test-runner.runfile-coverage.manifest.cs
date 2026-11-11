#region Purpose
// Proves local dotnet-coverage manifests are found. The relaunch keeps the
// working directory when that walk finds the manifest, and uses the manifest
// directory only for the entry-point fallback.
#endregion

#region Design
// The generated probe lives in a temp repo. PATH contains only the fake dotnet,
// and JARIBU_COVERAGE_USER_TOOLS points at an empty directory, so the global
// tool cannot hide the manifest branch.
#endregion

namespace TestRunner_;

public partial class RunfileCoverage_Given_
{
    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task RootManifestOutsideWorkingDirectory_Should_RelaunchFromTheManifestDirectory()
    {
      using ManifestWorkspace workspace = ManifestWorkspace.Create(configDirectory: false, listTool: true, isRoot: true);
      TimeWarp.Amuru.CommandOutput output = await RunManifestProbeAsync(workspace, workspace.OutsideDirectory);

      output.ExitCode.ShouldBe(0, output.Combined);
      AssertManifestCommand(workspace, workspace.RepoDirectory);
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task ConfigDirectoryManifest_Should_RelaunchFromTheManifestDirectory()
    {
      using ManifestWorkspace workspace = ManifestWorkspace.Create(configDirectory: true, listTool: true, isRoot: true);
      TimeWarp.Amuru.CommandOutput output = await RunManifestProbeAsync(workspace, workspace.RepoDirectory);

      output.ExitCode.ShouldBe(0, output.Combined);
      AssertManifestCommand(workspace, workspace.RepoDirectory);
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task NestedManifest_Should_WalkToTheParentThatListsTheTool()
    {
      using ManifestWorkspace workspace = ManifestWorkspace.Create(
        configDirectory: false,
        listTool: true,
        isRoot: true,
        nestedWithoutTool: true);
      TimeWarp.Amuru.CommandOutput output = await RunManifestProbeAsync(workspace, workspace.NestedDirectory);

      output.ExitCode.ShouldBe(0, output.Combined);
      AssertManifestCommand(workspace, workspace.NestedDirectory);
    }

    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task RootManifestWithoutTheTool_Should_StopAndNameTheInstallCommand()
    {
      using ManifestWorkspace workspace = ManifestWorkspace.Create(
        configDirectory: false,
        listTool: false,
        isRoot: true,
        outerManifestListsTool: true);
      TimeWarp.Amuru.CommandOutput output = await RunManifestProbeAsync(workspace, workspace.RepoDirectory);

      output.ExitCode.ShouldBe(1, output.Combined);
      output.Combined.ShouldContain("dotnet tool install -g dotnet-coverage");
      File.Exists(workspace.LogPath).ShouldBeFalse();
    }

    private static async Task<TimeWarp.Amuru.CommandOutput> RunManifestProbeAsync(ManifestWorkspace workspace, string workingDirectory)
    {
      _ = await EnsureFakeToolAsync();
      string? fakeDirectory = FakeDotnetDirectory;
      if (string.IsNullOrWhiteSpace(fakeDirectory))
      {
        throw new InvalidOperationException("The fake dotnet directory was not created.");
      }

      return await TimeWarp.Amuru.Shell.Builder(FindDotnet())
        .WithArguments(workspace.ProbePath, "--", workspace.ResultsDirectory)
        .WithWorkingDirectory(workingDirectory)
        .WithEnvironmentVariable("PATH", fakeDirectory)
        .WithEnvironmentVariable(RunfileCoverageOptions.UserToolsVariableName, workspace.EmptyToolsDirectory)
        .WithEnvironmentVariable(RunfileCoverageOptions.ToolVariableName, string.Empty)
        .WithEnvironmentVariable("JARIBU_COVERAGE_FAKE_LOG", workspace.LogPath)
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "0")
        .WithEnvironmentVariable(RunfileCoverageOptions.VariableName, "0")
        .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_32", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_64", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_ARM64", "0")
        .WithEnvironmentVariable(RunfileCoverageOptions.StaticVariableName, "0")
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(3))
        .CaptureAsync();
    }

    private static void AssertManifestCommand(ManifestWorkspace workspace, string workingDirectory)
    {
      string[] lines = File.ReadAllLines(workspace.LogPath);
      lines.ShouldContain("ACTIVE=1");
      string pwdLine = lines.Single(line => line.StartsWith("PWD=", StringComparison.Ordinal));
      CanonicalPath(pwdLine["PWD=".Length..]).ShouldBe(CanonicalPath(workingDirectory));
      string settingsPath = Path.Combine(workspace.ResultsDirectory, RunfileCoverageOptions.SettingsFileName);
      string outputPath = Path.Combine(workspace.ResultsDirectory, RunfileCoverageOptions.CoberturaFileName);
      AssertCommand(
        ReadCommand(workspace.LogPath),
        "tool",
        "run",
        "dotnet-coverage",
        "--",
        "collect",
        "-f",
        "cobertura",
        "-o",
        Path.GetFullPath(outputPath),
        "-s",
        Path.GetFullPath(settingsPath),
        "--",
        "dotnet",
        Path.GetFullPath(workspace.ProbePath),
        workspace.ResultsDirectory);
    }

    private static string CanonicalPath(string path)
    {
      // macOS reports a temp child's working directory as /private/var/..., the
      // resolved form of the /var/... symlink Path.GetTempPath returns.
      string full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
      if (OperatingSystem.IsMacOS() && full.StartsWith("/private/", StringComparison.Ordinal))
      {
        return full["/private".Length..];
      }

      return full;
    }

    private sealed class ManifestWorkspace : IDisposable
    {
      private ManifestWorkspace(
        string root,
        string repoDirectory,
        string outsideDirectory,
        string nestedDirectory,
        string emptyToolsDirectory,
        string resultsDirectory,
        string probePath)
      {
        Root = root;
        RepoDirectory = repoDirectory;
        OutsideDirectory = outsideDirectory;
        NestedDirectory = nestedDirectory;
        EmptyToolsDirectory = emptyToolsDirectory;
        ResultsDirectory = resultsDirectory;
        ProbePath = probePath;
        LogPath = Path.Combine(root, "launch.log");
      }

      public string Root { get; }

      public string RepoDirectory { get; }

      public string OutsideDirectory { get; }

      public string NestedDirectory { get; }

      public string EmptyToolsDirectory { get; }

      public string ResultsDirectory { get; }

      public string ProbePath { get; }

      public string LogPath { get; }

      public static ManifestWorkspace Create(
        bool configDirectory,
        bool listTool,
        bool isRoot,
        bool nestedWithoutTool = false,
        bool outerManifestListsTool = false)
      {
        string root = Path.Combine(Path.GetTempPath(), "jaribu-manifest-" + Guid.NewGuid().ToString("N"));
        string repoDirectory = Path.Combine(root, "repo");
        string outsideDirectory = Path.Combine(root, "outside");
        string nestedDirectory = Path.Combine(repoDirectory, "nested");
        string emptyToolsDirectory = Path.Combine(root, "empty-tools");
        string resultsDirectory = Path.Combine(root, "results");
        System.IO.Directory.CreateDirectory(repoDirectory);
        System.IO.Directory.CreateDirectory(outsideDirectory);
        System.IO.Directory.CreateDirectory(nestedDirectory);
        System.IO.Directory.CreateDirectory(emptyToolsDirectory);
        System.IO.Directory.CreateDirectory(resultsDirectory);

        if (outerManifestListsTool)
        {
          WriteManifest(root, listTool: true, isRoot: true, configDirectory: false);
        }

        WriteManifest(repoDirectory, listTool, isRoot, configDirectory);
        if (nestedWithoutTool)
        {
          WriteManifest(nestedDirectory, listTool: false, isRoot: false, configDirectory: false);
        }

        string project = Path.Combine(
          FindRepoRoot(),
          "source",
          "timewarp-jaribu",
          "timewarp-jaribu.csproj");
        string probePath = Path.Combine(repoDirectory, "probe.cs");
        File.WriteAllText(
          probePath,
          "#!/usr/bin/env -S dotnet --" + Environment.NewLine
          + "#:project " + project + Environment.NewLine
          + Environment.NewLine
          + "using TimeWarp.Jaribu;" + Environment.NewLine
          + "using static TimeWarp.Jaribu.TestRunner;" + Environment.NewLine
          + Environment.NewLine
          + "string results = args[0];" + Environment.NewLine
          + "return await RunAllTests(" + Environment.NewLine
          + "  allowEmpty: true," + Environment.NewLine
          + "  coverage: new RunfileCoverageOptions" + Environment.NewLine
          + "  {" + Environment.NewLine
          + "    ResultsDirectory = results," + Environment.NewLine
          + "    ModuleFilter = \"Sentinel\\\\.dll\"" + Environment.NewLine
          + "  });" + Environment.NewLine);
        return new ManifestWorkspace(
          root,
          repoDirectory,
          outsideDirectory,
          nestedDirectory,
          emptyToolsDirectory,
          resultsDirectory,
          probePath);
      }

      public void Dispose()
      {
        try
        {
          System.IO.Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
          // The assertion already ran. A leftover temp directory is not a test failure.
        }
      }

      private static void WriteManifest(string directory, bool listTool, bool isRoot, bool configDirectory)
      {
        string folder = configDirectory ? Path.Combine(directory, ".config") : directory;
        System.IO.Directory.CreateDirectory(folder);
        string tool = listTool
          ? "\"dotnet-coverage\": { \"version\": \"0.0.0\", \"commands\": [ \"dotnet-coverage\" ] }"
          : string.Empty;
        string json = "{" + Environment.NewLine
          + "  \"version\": 1," + Environment.NewLine
          + "  \"isRoot\": " + (isRoot ? "true" : "false") + "," + Environment.NewLine
          + "  \"tools\": { " + tool + " }" + Environment.NewLine
          + "}" + Environment.NewLine;
        File.WriteAllText(Path.Combine(folder, "dotnet-tools.json"), json);
      }
    }
}
