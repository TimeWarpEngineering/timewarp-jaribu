#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Packs TimeWarp.Jaribu.TestingPlatform and checks a class library that only
// references a test project using that package stays a library.
#endregion

#region Design
// The direct props set OutputType=Exe, IsTestProject, IsTestingPlatformApplication,
// and _JaribuTestingPlatformDirectReference. NuGet imports build/ for a direct
// PackageReference and buildTransitive/ for that project and every project that
// references it. The transitive targets force IsTestingPlatformApplication false
// unless the marker is set or EnableJaribuRunner is true. Microsoft.Testing.Platform
// defaults an empty flag to true, so the targets assign false rather than leaving
// the property unset.
// Packing the adapter does not emit the TimeWarp.Jaribu nupkg for its project
// reference into the -o directory. GeneratePackageOnBuild writes that package
// to the repo package output. The test packs the core project into the feed so
// sandbox restore resolves TimeWarp.Jaribu from the feed.
// dotnet pack does not compile when GeneratePackageOnBuild is true and the
// Release assembly is missing (NU5026). The test builds Release first.
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
  using System.Reflection;
  using System.Runtime.Versioning;
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
        string project = Path.Combine(
          root,
          "source",
          "timewarp-jaribu-testing-platform",
          "timewarp-jaribu-testing-platform.csproj");
        CommandOutput releaseBuild = await RunDotNetAsync(
          root,
          "build",
          project,
          "-c",
          "Release",
          "--nologo");
        releaseBuild.ExitCode.ShouldBe(0, releaseBuild.Combined);

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
        string optInDirectory = Path.Combine(app, "opt-in-library");
        Directory.CreateDirectory(consumerDirectory);
        Directory.CreateDirectory(libraryDirectory);
        Directory.CreateDirectory(optInDirectory);
        WriteSandboxProps(app);
        WriteNuGetConfig(sandbox, feed);
        File.WriteAllText(
          Path.Combine(consumerDirectory, "adapter-consumer.csproj"),
          ConsumerProject(version));
        File.WriteAllText(
          Path.Combine(libraryDirectory, "transitive-library.csproj"),
          LibraryProject());
        File.WriteAllText(
          Path.Combine(optInDirectory, "opt-in-library.csproj"),
          OptInLibraryProject());

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
        string libraryTestingPlatform =
          libraryProperties.GetValueOrDefault("IsTestingPlatformApplication") ?? "";
        (libraryTestingPlatform is "" or "false").ShouldBeTrue(
          $"IsTestingPlatformApplication was '{libraryTestingPlatform}'.");

        string optInProject = Path.Combine(optInDirectory, "opt-in-library.csproj");
        CommandOutput optInBuild = await RunDotNetAsync(
          root,
          "build",
          optInProject,
          "-c",
          "Debug",
          "--configfile",
          config,
          "--packages",
          packages,
          "--nologo");
        optInBuild.ExitCode.ShouldBe(0, optInBuild.Combined);

        Dictionary<string, string> optInProperties = await EvaluateAsync(optInProject);
        optInProperties["OutputType"].ShouldBe("Library");
        optInProperties["IsTestProject"].ShouldNotBe("true");
        optInProperties["IsTestingPlatformApplication"].ShouldBe("true");

        Dictionary<string, string> consumerProperties = await EvaluateAsync(consumerProject);
        consumerProperties["OutputType"].ShouldBe("Exe");
        consumerProperties["IsTestProject"].ShouldBe("true");
        consumerProperties["IsTestingPlatformApplication"].ShouldBe("true");

        string targetFramework = TargetFrameworkMoniker();
        string libraryOutput = Path.Combine(libraryDirectory, "bin", "Debug", targetFramework);
        File.Exists(Path.Combine(libraryOutput, "transitive-library.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(libraryOutput, ApphostName("transitive-library"))).ShouldBeFalse();
        File.Exists(EntryPointSource(libraryDirectory)).ShouldBeFalse();

        string optInOutput = Path.Combine(optInDirectory, "bin", "Debug", targetFramework);
        File.Exists(Path.Combine(optInOutput, "opt-in-library.dll")).ShouldBeTrue();
        File.Exists(EntryPointSource(optInDirectory)).ShouldBeTrue();

        string consumerOutput = Path.Combine(consumerDirectory, "bin", "Debug", targetFramework);
        File.Exists(Path.Combine(consumerOutput, "adapter-consumer.dll")).ShouldBeTrue();
        File.Exists(Path.Combine(consumerOutput, ApphostName("adapter-consumer"))).ShouldBeTrue();
        File.Exists(EntryPointSource(consumerDirectory)).ShouldBeTrue();
      }
      finally
      {
        await DeleteSandboxAsync(root, sandbox);
      }
    }

    // Windows: build servers can hold package dlls and the NuGet cache marks files read-only,
    // so a plain recursive delete throws UnauthorizedAccessException.
    private static async Task DeleteSandboxAsync(string root, string sandbox)
    {
      if (!Directory.Exists(sandbox))
      {
        return;
      }

      await RunDotNetAsync(root, "build-server", "shutdown");

      for (int attempt = 1; ; attempt++)
      {
        try
        {
          foreach (string file in Directory.EnumerateFiles(sandbox, "*", SearchOption.AllDirectories))
          {
            File.SetAttributes(file, FileAttributes.Normal);
          }

          Directory.Delete(sandbox, recursive: true);
          return;
        }
        catch (Exception exception) when
          (attempt < 5 && exception is UnauthorizedAccessException or IOException)
        {
          await Task.Delay(TimeSpan.FromSeconds(attempt));
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
        entry.FullName.Equals(
          "buildTransitive/timewarp.jaribu.testingplatform.props",
          StringComparison.OrdinalIgnoreCase));
      bool hasTransitiveTargets = archive.Entries.Any(entry =>
        entry.FullName.Equals(
          "buildTransitive/timewarp.jaribu.testingplatform.targets",
          StringComparison.OrdinalIgnoreCase));
      string entryNames = string.Join(Environment.NewLine, archive.Entries.Select(entry => entry.FullName));
      hasDirectProps.ShouldBeTrue(packagePath);
      hasTransitiveProps.ShouldBeFalse(entryNames);
      hasTransitiveTargets.ShouldBeTrue(entryNames);
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
      string targetFramework = TargetFrameworkMoniker();
      return $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>{targetFramework}</TargetFramework>
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
      string targetFramework = TargetFrameworkMoniker();
      return $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>{targetFramework}</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
          </PropertyGroup>
          <ItemGroup>
            <ProjectReference Include="..\adapter-consumer\adapter-consumer.csproj" />
          </ItemGroup>
        </Project>
        """;
    }

    private static string OptInLibraryProject()
    {
      string targetFramework = TargetFrameworkMoniker();
      return $"""
        <Project Sdk="Microsoft.NET.Sdk">
          <PropertyGroup>
            <TargetFramework>{targetFramework}</TargetFramework>
            <ImplicitUsings>enable</ImplicitUsings>
            <Nullable>enable</Nullable>
            <EnableJaribuRunner>true</EnableJaribuRunner>
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

    // The MTP runner runs under dotnet-coverage, which exports profiler variables. Child dotnet
    // processes inherit them and restore hangs on Windows, so the children run without a profiler.
    // Each call also has a hard timeout so a hang fails fast with the captured output.
    private static readonly TimeSpan DotNetTimeout = TimeSpan.FromMinutes(5);

    private static async Task<CommandOutput> RunDotNetAsync(string workingDirectory, params string[] arguments)
    {
      using CancellationTokenSource timeout = new(DotNetTimeout);
      ShellBuilder builder = Shell.Builder("dotnet")
        .WithArguments(arguments)
        .WithWorkingDirectory(workingDirectory)
        .WithEnvironmentVariable("CORECLR_ENABLE_PROFILING", "0")
        .WithEnvironmentVariable("DOTNET_CLI_USE_MSBUILD_SERVER", "0")
        .WithEnvironmentVariable("UseSharedCompilation", "false")
        .WithEnvironmentVariable("MSBUILDDISABLENODEREUSE", "1")
        .WithNoValidation();
      foreach (string name in new[]
      {
        "CORECLR_PROFILER",
        "CORECLR_PROFILER_PATH",
        "CORECLR_PROFILER_PATH_32",
        "CORECLR_PROFILER_PATH_64",
        "CORECLR_PROFILER_PATH_ARM64",
        "COR_ENABLE_PROFILING",
        "COR_PROFILER",
        "COR_PROFILER_PATH",
      })
      {
        builder = builder.WithEnvironmentVariable(name, string.Empty);
      }

      try
      {
        return await builder.RunAndCaptureAsync(timeout.Token);
      }
      catch (OperationCanceledException) when (timeout.IsCancellationRequested)
      {
        throw new TimeoutException(
          $"dotnet {string.Join(' ', arguments)} did not finish within {DotNetTimeout.TotalMinutes} minutes.");
      }
    }

    private static string ApphostName(string name) =>
      OperatingSystem.IsWindows() ? name + ".exe" : name;

    private static string EntryPointSource(string projectDirectory) =>
      Path.Combine(
        projectDirectory,
        "obj",
        "Debug",
        TargetFrameworkMoniker(),
        "MicrosoftTestingPlatformEntryPoint.cs");

    private static string TargetFrameworkMoniker()
    {
      TargetFrameworkAttribute attribute =
        Assembly.GetExecutingAssembly().GetCustomAttribute<TargetFrameworkAttribute>()
        ?? throw new InvalidOperationException("The test assembly declares no target framework.");

      const string VersionMarker = ",Version=v";
      int marker = attribute.FrameworkName.IndexOf(VersionMarker, StringComparison.Ordinal);
      if (marker < 0 || marker + VersionMarker.Length >= attribute.FrameworkName.Length)
      {
        throw new InvalidOperationException(
          $"Cannot read a TFM from '{attribute.FrameworkName}'.");
      }

      return "net" + attribute.FrameworkName[(marker + VersionMarker.Length)..];
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
