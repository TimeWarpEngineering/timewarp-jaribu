#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Packs TimeWarp.Jaribu.TestingPlatform and checks a class library that only
// references a test project using that package stays a library.
#endregion

#region Design
// The props set OutputType=Exe, IsTestProject, and IsTestingPlatformApplication.
// NuGet imports build/ for a direct PackageReference. It imports buildTransitive/
// for every project that references that consumer, and when both folders contain
// the package props it imports the buildTransitive copy for the direct reference
// as well. The package therefore ships build/ only.
// Packing the adapter does not emit the TimeWarp.Jaribu nupkg for its project
// reference into the -o directory. GeneratePackageOnBuild writes that package
// to the repo package output. The test packs the core project into the feed so
// sandbox restore resolves TimeWarp.Jaribu from the feed.
// The sandbox has its own Directory.Build.props and Directory.Packages.props so
// the repo's central package versions and analyzers do not apply. Packages restore
// into the sandbox, not the global NuGet cache.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestingPlatformProps_
{
  using System.IO.Compression;
  using System.Text.Json;
  using TimeWarp.Amuru;

  [TestTag("Mtp")]
  public class TransitiveConsumer_Given_
  {
    private const string PackagePrefix = "TimeWarp.Jaribu.TestingPlatform.";

    [ModuleInitializer]
    internal static void Register() => RegisterTests<TransitiveConsumer_Given_>();

    public static async Task ClassLibrary_Should_StayALibrary()
    {
      string root = FindRepoRoot();
      string sandbox = Path.Combine(
        root,
        "artifacts",
        "testing-platform-props-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(sandbox);

      try
      {
        string feed = Path.Combine(sandbox, "feed");
        string packages = Path.Combine(sandbox, "packages");
        Directory.CreateDirectory(feed);

        string coreProject = Path.Combine(
          root,
          "source",
          "timewarp-jaribu",
          "timewarp-jaribu.csproj");
        CommandOutput corePack = await RunDotNetAsync(
          root,
          "pack",
          coreProject,
          "-c",
          "Release",
          "-o",
          feed,
          "--nologo");
        corePack.ExitCode.ShouldBe(0, corePack.Combined);

        string project = Path.Combine(
          root,
          "source",
          "timewarp-jaribu-testing-platform",
          "timewarp-jaribu-testing-platform.csproj");
        CommandOutput pack = await RunDotNetAsync(root, "pack", project, "-c", "Release", "-o", feed, "--nologo");
        pack.ExitCode.ShouldBe(0, pack.Combined);

        string[] adapterPackages = Directory.GetFiles(feed, "TimeWarp.Jaribu.TestingPlatform.*.nupkg");
        int corePackageCount = Directory.GetFiles(feed, "TimeWarp.Jaribu.*.nupkg")
          .Count(path => !Path.GetFileName(path).StartsWith(PackagePrefix, StringComparison.Ordinal));
        adapterPackages.Length.ShouldBe(1, string.Join(Environment.NewLine, Directory.GetFiles(feed)));
        corePackageCount.ShouldBe(1, string.Join(Environment.NewLine, Directory.GetFiles(feed)));

        string adapterPackage = adapterPackages[0];
        string version = Path.GetFileNameWithoutExtension(adapterPackage)[PackagePrefix.Length..];
        AssertPackageLayout(adapterPackage);

        string app = Path.Combine(sandbox, "app");
        string consumerDirectory = Path.Combine(app, "adapter-consumer");
        string libraryDirectory = Path.Combine(app, "transitive-library");
        Directory.CreateDirectory(consumerDirectory);
        Directory.CreateDirectory(libraryDirectory);
        WriteSandboxProps(app);
        WriteNuGetConfig(sandbox, feed);
        File.WriteAllText(
          Path.Combine(consumerDirectory, "adapter-consumer.csproj"),
          ConsumerProject(version));
        File.WriteAllText(
          Path.Combine(libraryDirectory, "transitive-library.csproj"),
          LibraryProject());

        string config = Path.Combine(sandbox, "nuget.config");
        string libraryProject = Path.Combine(libraryDirectory, "transitive-library.csproj");
        string consumerProject = Path.Combine(consumerDirectory, "adapter-consumer.csproj");
        CommandOutput build = await RunDotNetAsync(
          root,
          "build",
          libraryProject,
          "-c",
          "Debug",
          "--configfile",
          config,
          "--packages",
          packages,
          "--nologo");
        build.ExitCode.ShouldBe(0, build.Combined);

        Dictionary<string, string> libraryProperties = await EvaluateAsync(libraryProject);
        libraryProperties["OutputType"].ShouldBe("Library");
        libraryProperties["IsTestProject"].ShouldNotBe("true");

        Dictionary<string, string> consumerProperties = await EvaluateAsync(consumerProject);
        consumerProperties["OutputType"].ShouldBe("Exe");
        consumerProperties["IsTestProject"].ShouldBe("true");
        consumerProperties["IsTestingPlatformApplication"].ShouldBe("true");

        string libraryOutput = Path.Combine(libraryDirectory, "bin", "Debug", "net10.0");
        File.Exists(Path.Combine(libraryOutput, "transitive-library.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(libraryOutput, "transitive-library")).ShouldBeFalse();

        string consumerOutput = Path.Combine(consumerDirectory, "bin", "Debug", "net10.0");
        File.Exists(Path.Combine(consumerOutput, "adapter-consumer.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(consumerOutput, "adapter-consumer")).ShouldBeTrue();
      }
      finally
      {
        if (Directory.Exists(sandbox))
        {
          Directory.Delete(sandbox, recursive: true);
        }
      }
    }

    private static void AssertPackageLayout(string packagePath)
    {
      using ZipArchive archive = ZipFile.OpenRead(packagePath);
      bool hasDirectProps = archive.Entries.Any(entry =>
        entry.FullName.Equals(
          "build/timewarp.jaribu.testingplatform.props",
          StringComparison.OrdinalIgnoreCase));
      bool hasTransitiveProps = archive.Entries.Any(entry =>
        entry.FullName.StartsWith("buildTransitive/", StringComparison.OrdinalIgnoreCase));
      hasDirectProps.ShouldBeTrue(packagePath);
      hasTransitiveProps.ShouldBeFalse(string.Join(Environment.NewLine, archive.Entries.Select(entry => entry.FullName)));
    }

    private static void WriteSandboxProps(string app)
    {
      const string EmptyProject = "<Project>\n</Project>\n";
      File.WriteAllText(Path.Combine(app, "Directory.Build.props"), EmptyProject);
      File.WriteAllText(Path.Combine(app, "Directory.Build.targets"), EmptyProject);
      File.WriteAllText(Path.Combine(app, "Directory.Packages.props"), EmptyProject);
    }

    private static void WriteNuGetConfig(string sandbox, string feed)
    {
      string config = $"""
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
          <packageSources>
            <clear />
            <add key="local" value="{feed}" />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
          </packageSources>
        </configuration>
        """;
      File.WriteAllText(Path.Combine(sandbox, "nuget.config"), config.Trim() + Environment.NewLine);
    }

    private static string ConsumerProject(string version)
    {
      return $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <PackageReference Include="TimeWarp.Jaribu.TestingPlatform" Version="{version}" />
          </ItemGroup>
        </Project>
        """;
    }

    private static string LibraryProject()
    {
      return """
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>net10.0</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\adapter-consumer\adapter-consumer.csproj" />
          </ItemGroup>
        </Project>
        """;
    }

    private static async Task<Dictionary<string, string>> EvaluateAsync(string projectPath)
    {
      CommandOutput evaluation = await RunDotNetAsync(
        Path.GetDirectoryName(projectPath)!,
        "msbuild",
        projectPath,
        "-nologo",
        "-getProperty:OutputType,IsTestProject,IsTestingPlatformApplication");
      evaluation.ExitCode.ShouldBe(0, evaluation.Combined);

      using JsonDocument document = JsonDocument.Parse(evaluation.Stdout);
      JsonElement properties = document.RootElement.GetProperty("Properties");
      Dictionary<string, string> values = [];
      foreach (JsonProperty property in properties.EnumerateObject())
      {
        values[property.Name] = property.Value.GetString() ?? "";
      }

      return values;
    }

    private static Task<CommandOutput> RunDotNetAsync(string workingDirectory, params string[] arguments)
    {
      return Shell.Builder("dotnet")
        .WithArguments(arguments)
        .WithWorkingDirectory(workingDirectory)
        .WithNoValidation()
        .RunAndCaptureAsync();
    }

    private static string FindRepoRoot()
    {
      DirectoryInfo? directory = new(AppContext.BaseDirectory);
      while (directory is not null)
      {
        if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      directory = new DirectoryInfo(Directory.GetCurrentDirectory());
      while (directory is not null)
      {
        if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
        {
          return directory.FullName;
        }

        directory = directory.Parent;
      }

      throw new InvalidOperationException(
        $"Repository root not found from '{AppContext.BaseDirectory}' or '{Directory.GetCurrentDirectory()}'.");
    }
  }
}
