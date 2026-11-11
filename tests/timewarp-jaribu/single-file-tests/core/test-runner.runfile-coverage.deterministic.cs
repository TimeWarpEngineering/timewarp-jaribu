#region Purpose
// Proves a consumer built with a /_/ path map still reports its own product.
#endregion

#region Design
// The consumer is a temp git repo whose Directory.Build.props maps that repo to
// /_/, the same PathMap ContinuousIntegrationBuild produces. The runfile
// references this repo's Jaribu, so Amuru is loaded. The report must keep
// Calc.cs and omit that dependency.
#endregion

namespace TestRunner_;

using System.Xml.Linq;

public partial class RunfileCoverage_Given_
{
    [Timeout(ProbeTimeoutMilliseconds)]
    public static async Task DeterministicPathMap_Should_KeepTheProductAndOmitDependencies()
    {
      using ConsumerWorkspace workspace = ConsumerWorkspace.Create(FindRepoRoot());
      TimeWarp.Amuru.CommandOutput init = await TimeWarp.Amuru.Shell.Builder("git")
        .WithArguments("init", "-q")
        .WithWorkingDirectory(workspace.RepoDirectory)
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(1))
        .CaptureAsync();
      init.ExitCode.ShouldBe(0, init.Combined);

      TimeWarp.Amuru.CommandOutput output = await TimeWarp.Amuru.Shell.Builder(FindDotnet())
        .WithArguments(workspace.ProbePath)
        .WithWorkingDirectory(workspace.RepoDirectory)
        .WithEnvironmentVariable(RunfileCoverageOptions.ToolVariableName, string.Empty)
        .WithEnvironmentVariable(RunfileCoverageOptions.ActiveVariableName, "0")
        .WithEnvironmentVariable(RunfileCoverageOptions.VariableName, "0")
        .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_32", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_64", "0")
        .WithEnvironmentVariable("CORECLR_PROFILER_PATH_ARM64", "0")
        .WithNoValidation()
        .WithTimeout(TimeSpan.FromMinutes(4))
        .CaptureAsync();

      output.ExitCode.ShouldBe(0, output.Combined);
      string reportPath = Path.Combine(workspace.ResultsDirectory, RunfileCoverageOptions.CoberturaFileName);
      File.Exists(reportPath).ShouldBeTrue(output.Combined);
      XDocument document = XDocument.Load(reportPath);
      List<string> filenames = [];
      foreach (XElement element in document.Descendants("class"))
      {
        XAttribute? filename = element.Attribute("filename");
        if (filename is not null)
        {
          filenames.Add(filename.Value);
        }
      }

      string listed = string.Join(Environment.NewLine, filenames);
      bool sawMappedProduct = false;
      foreach (string filename in filenames)
      {
        if (filename.StartsWith("/_/", StringComparison.Ordinal)
          && filename.Contains("Calc.cs", StringComparison.Ordinal))
        {
          sawMappedProduct = true;
        }

        filename.Contains("timewarp-amuru", StringComparison.OrdinalIgnoreCase)
          .ShouldBeFalse(listed);
        filename.Contains("CliWrap", StringComparison.Ordinal)
          .ShouldBeFalse(listed);
        filename.Contains("runfile-coverage.cs", StringComparison.Ordinal)
          .ShouldBeFalse(listed);
      }

      sawMappedProduct.ShouldBeTrue(listed);
    }

    private sealed class ConsumerWorkspace : IDisposable
    {
      private ConsumerWorkspace(string root, string repoDirectory, string resultsDirectory, string probePath)
      {
        Root = root;
        RepoDirectory = repoDirectory;
        ResultsDirectory = resultsDirectory;
        ProbePath = probePath;
      }

      public string Root { get; }

      public string RepoDirectory { get; }

      public string ResultsDirectory { get; }

      public string ProbePath { get; }

      public static ConsumerWorkspace Create(string jaribuRepo)
      {
        string root = Path.Combine(Path.GetTempPath(), "jaribu-deterministic-" + Guid.NewGuid().ToString("N"));
        string repoDirectory = Path.Combine(root, "repo");
        string resultsDirectory = Path.Combine(root, "results");
        System.IO.Directory.CreateDirectory(Path.Combine(repoDirectory, "lib"));
        System.IO.Directory.CreateDirectory(resultsDirectory);
        File.WriteAllText(
          Path.Combine(repoDirectory, "Directory.Build.props"),
          """
          <Project>
            <PropertyGroup>
              <ImplicitUsings>enable</ImplicitUsings>
              <Nullable>enable</Nullable>
              <Deterministic>true</Deterministic>
              <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
              <DeterministicSourcePaths>true</DeterministicSourcePaths>
              <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
              <PathMap>$(MSBuildThisFileDirectory)=/_/</PathMap>
            </PropertyGroup>
          </Project>
          """);
        File.WriteAllText(
          Path.Combine(repoDirectory, "lib", "Calc.csproj"),
          """
          <Project Sdk="Microsoft.NET.Sdk">
            <PropertyGroup>
              <TargetFramework>net11.0</TargetFramework>
            </PropertyGroup>
          </Project>
          """);
        File.WriteAllText(
          Path.Combine(repoDirectory, "lib", "Calc.cs"),
          """
          public static class Calc
          {
            public static int Add(int left, int right) => left + right;
          }
          """);
        string jaribu = Path.Combine(
          jaribuRepo,
          "source",
          "timewarp-jaribu",
          "timewarp-jaribu.csproj");
        string resultsLiteral = resultsDirectory.Replace("\\", "\\\\", StringComparison.Ordinal);
        string probePath = Path.Combine(repoDirectory, "cover.cs");
        File.WriteAllText(
          probePath,
          "#!/usr/bin/env -S dotnet --" + Environment.NewLine
          + "#:project " + jaribu + Environment.NewLine
          + "#:project lib/Calc.csproj" + Environment.NewLine
          + Environment.NewLine
          + "using TimeWarp.Jaribu;" + Environment.NewLine
          + "using static TimeWarp.Jaribu.TestRunner;" + Environment.NewLine
          + Environment.NewLine
          + "if (Calc.Add(2, 3) != 5)" + Environment.NewLine
          + "{" + Environment.NewLine
          + "  return 3;" + Environment.NewLine
          + "}" + Environment.NewLine
          + Environment.NewLine
          + "return await RunAllTests(" + Environment.NewLine
          + "  allowEmpty: true," + Environment.NewLine
          + "  coverage: new RunfileCoverageOptions" + Environment.NewLine
          + "  {" + Environment.NewLine
          + "    ResultsDirectory = \"" + resultsLiteral + "\"" + Environment.NewLine
          + "  });" + Environment.NewLine);
        return new ConsumerWorkspace(root, repoDirectory, resultsDirectory, probePath);
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
        catch (UnauthorizedAccessException)
        {
          // The assertion already ran. A leftover temp directory is not a test failure.
        }
      }
    }
}
