#region Purpose
// Orchestrates the full CI/CD pipeline
#endregion

#region Design
// PR/merge: clean -> build -> test
// Release: clean -> build -> test -> tag guard -> NuGet index -> check-version -> pack -> push
// Test runs ci-runner and the CI-safe MTP project on both pipelines.
// The root is RepoRoot.Find when that directory contains timewarp-jaribu.slnx.
// Otherwise the binary-relative walk, then the working directory.
// The release tag must be "v" plus the version `dotnet msbuild -getProperty:Version`
// evaluates for each packable project. A props string search is not used.
// A NuGet flat-container index is known only on HTTP 200. Any other status, and a
// failed request, is unknown and the release stops. On 200, the shared check-version
// handler decides whether that evaluated version is already published.
// artifacts/packages is deleted before pack. The push list is the two nupkgs just
// packed, not a glob of whatever the directory still holds, and uses --skip-duplicate.
// Pack passes JaribuExposeInternals=false so the testing platform omits its test
// InternalsVisibleTo grants. The release gate then reads each packed lib assembly
// and refuses the release when any grant remains.
// Auto-detects mode from GITHUB_EVENT_NAME. A non-empty API key also selects release.
// The key is NUGET_API_KEY or --api-key. Push forwards it only as NUGET_API_KEY.
// NuGet 7.6 and later read that variable, so the key is not a child-process argument.
// OIDC Trusted Publishing supplies the key on the release job.
#endregion

namespace DevCli.Commands;

/// <summary>
/// Run full CI/CD pipeline
/// </summary>
[NuruRoute("workflow", Description = "Run full CI/CD pipeline")]
internal sealed class WorkflowCommand : ICommand<Unit>
{
  private const string NuGetApiKeyVariable = "NUGET_API_KEY";

  [Option("api-key", Description = "NuGet API key for publishing. NUGET_API_KEY is preferred so the key is not a process argument.")]
  public string? ApiKey { get; set; }

  internal sealed class Handler : IRequestHandler<WorkflowCommand, Unit>
  {
    private readonly ITerminal Terminal;
    private readonly IRepoCleanService RepoCleanService;
    private readonly NuGetVersionService NuGetVersionService;
    private readonly IRepoConfigService ConfigService;
    private readonly IPackableProjectService PackableProjectService;

    public Handler(
      ITerminal terminal,
      IRepoCleanService repoCleanService,
      NuGetVersionService nuGetVersionService,
      IRepoConfigService configService,
      IPackableProjectService packableProjectService)
    {
      Terminal = terminal;
      RepoCleanService = repoCleanService;
      NuGetVersionService = nuGetVersionService;
      ConfigService = configService;
      PackableProjectService = packableProjectService;
    }

    public async ValueTask<Unit> Handle(WorkflowCommand command, CancellationToken ct)
    {
      // Auto-detect from GitHub Actions environment
      string? eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
      string? apiKey = ResolveApiKey(command.ApiKey);
      bool isRelease = eventName == "release" || !string.IsNullOrWhiteSpace(apiKey);

      string? repoRoot = RepoRoot.Find();
      if (repoRoot is null || !File.Exists(Path.Combine(repoRoot, "timewarp-jaribu.slnx")))
      {
        string fromBinary = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        repoRoot = File.Exists(Path.Combine(fromBinary, "timewarp-jaribu.slnx"))
          ? fromBinary
          : Path.GetFullPath(Directory.GetCurrentDirectory());
      }

      if (isRelease)
      {
        await RunReleaseWorkflowAsync(repoRoot, apiKey, ct);
      }
      else
      {
        await RunPrWorkflowAsync(repoRoot, ct);
      }

      return Unit.Value;
    }

    private async Task RunPrWorkflowAsync(string repoRoot, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();
      Terminal.WriteLine("CI Pipeline: clean -> build -> test");
      Terminal.WriteLine("");

      // Step 1: Clean
      Terminal.WriteLine("Step 1/3: Clean");
      int exitCode = await DotNet.Clean(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithVerbosity("q")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(cancellationToken);
      if (exitCode != 0) throw new InvalidOperationException("Clean failed!");

      // Step 2: Build
      Terminal.WriteLine("\nStep 2/3: Build");
      exitCode = await DotNet.Build(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithConfiguration("Release")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(cancellationToken);
      if (exitCode != 0) throw new InvalidOperationException("Build failed!");

      // Step 3: Test (ci-runner and the CI-safe MTP project)
      Terminal.WriteLine("\nStep 3/3: Test");
      await CiTestSuite.RunAsync(repoRoot, cancellationToken).ConfigureAwait(false);

      Terminal.WriteLine("\n✓ CI Pipeline completed successfully");
    }

    private async Task RunReleaseWorkflowAsync(string repoRoot, string? apiKey, CancellationToken ct)
    {
      Terminal.WriteLine("Release Pipeline: clean -> build -> test -> check-version -> pack");
      Terminal.WriteLine("");

      // Step 1: Clean
      Terminal.WriteLine("Step 1/5: Clean");
      int exitCode = await DotNet.Clean(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithVerbosity("q")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(ct);
      if (exitCode != 0) throw new InvalidOperationException("Clean failed!");

      // Step 2: Build
      Terminal.WriteLine("\nStep 2/5: Build");
      exitCode = await DotNet.Build(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithConfiguration("Release")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(ct);
      if (exitCode != 0) throw new InvalidOperationException("Build failed!");

      // Step 3: Test (same ci-runner and CI-safe MTP project as PR)
      Terminal.WriteLine("\nStep 3/5: Test");
      await CiTestSuite.RunAsync(repoRoot, ct).ConfigureAwait(false);

      // Step 4: Tag, NuGet index, and the shared check-version gate
      Terminal.WriteLine("\nStep 4/5: Check Version");
      IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, ct)
        .ConfigureAwait(false);
      ReleaseGate.RequirePackageIds([.. packableProjects.Select(project => project.PackageId)]);

      string version = await EvaluateReleaseVersionAsync(repoRoot, packableProjects, ct);
      string? releaseTag = Environment.GetEnvironmentVariable("GITHUB_REF_NAME");
      if (!ReleaseGate.TagMatchesVersion(releaseTag, version))
      {
        string shownTag = string.IsNullOrWhiteSpace(releaseTag) ? "(not set)" : releaseTag;
        throw new InvalidOperationException(
          $"Release tag '{shownTag}' does not match {ReleaseGate.ExpectedTag(version)}.");
      }

      Terminal.WriteLine($"Current version: {version}");
      Terminal.WriteLine($"Release tag: {ReleaseGate.ExpectedTag(version)}");
      await EnsureNuGetIndexKnownAsync(packableProjects, ct).ConfigureAwait(false);

      Environment.ExitCode = 0;
      CheckVersionCommand.Handler checkVersionHandler = new(
        Terminal,
        NuGetVersionService,
        ConfigService,
        PackableProjectService);
      await checkVersionHandler
        .Handle(new CheckVersionCommand { SourceVersion = version }, ct)
        .ConfigureAwait(false);
      if (Environment.ExitCode != 0)
      {
        throw new InvalidOperationException($"Version {version} is not available to publish.");
      }

      // Step 5: Pack only the projects just cleared into artifacts/packages
      Terminal.WriteLine("\nStep 5/5: Pack");
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      ReleaseGate.ClearPackageDirectory(artifactsDir);

      foreach (PackableProject project in packableProjects)
      {
        Terminal.WriteLine($"  Packing {project.PackageId}...");
        exitCode = await DotNet.Pack(project.ProjectPath)
          .WithConfiguration("Release")
          .WithProperty("JaribuExposeInternals", "false")
          .WithOutput(artifactsDir)
          .WithWorkingDirectory(repoRoot)
          .RunAsync(ct);

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Pack {project.PackageId} failed!");
        }
      }

      IReadOnlyList<string> freshPackages = ReleaseGate.RequireFreshPackagePaths(
        artifactsDir,
        [.. packableProjects.Select(project => project.PackageId)],
        version);
      foreach (string packagePath in freshPackages)
      {
        ReleaseGate.RequirePackageGrantsNoInternals(packagePath);
      }

      if (!string.IsNullOrEmpty(apiKey))
      {
        Terminal.WriteLine("\nPushing packages to NuGet...");
        foreach (string packagePath in freshPackages)
        {
          string packageName = Path.GetFileName(packagePath);
          Terminal.WriteLine($"  Pushing {packageName}...");

          exitCode = await DotNet.NuGet()
            .WithEnvironmentVariable(NuGetApiKeyVariable, apiKey)
            .Push(packagePath)
            .WithSource("https://api.nuget.org/v3/index.json")
            .WithSkipDuplicate()
            .RunAsync(ct);

          if (exitCode != 0)
          {
            throw new InvalidOperationException($"NuGet push failed: {packageName}");
          }
        }

        Terminal.WriteLine("✓ Packages pushed to NuGet.org");
      }

      Terminal.WriteLine("\n✓ Release Pipeline completed successfully");
      Terminal.WriteLine($"  Packages created in: {artifactsDir}");
      foreach (string packagePath in freshPackages)
      {
        Terminal.WriteLine($"  {Path.GetFileName(packagePath)}");
      }
    }

    private static string? ResolveApiKey(string? optionApiKey)
    {
      if (!string.IsNullOrWhiteSpace(optionApiKey))
      {
        return optionApiKey;
      }

      string? environmentApiKey = Environment.GetEnvironmentVariable(NuGetApiKeyVariable);
      if (string.IsNullOrWhiteSpace(environmentApiKey))
      {
        return null;
      }

      return environmentApiKey;
    }

    private static async Task<string> EvaluateReleaseVersionAsync
    (
      string repoRoot,
      IReadOnlyList<PackableProject> packableProjects,
      CancellationToken cancellationToken
    )
    {
      string? version = null;
      foreach (PackableProject project in packableProjects)
      {
        string projectVersion = await EvaluateProjectVersionAsync(repoRoot, project.ProjectPath, cancellationToken)
          .ConfigureAwait(false);
        if (version is null)
        {
          version = projectVersion;
          continue;
        }

        if (!string.Equals(version, projectVersion, StringComparison.Ordinal))
        {
          throw new InvalidOperationException(
            $"{project.PackageId} evaluates to {projectVersion}, which differs from {version}.");
        }
      }

      if (string.IsNullOrWhiteSpace(version))
      {
        throw new InvalidOperationException("Could not evaluate <Version> from the packable projects.");
      }

      return version;
    }

    private static async Task<string> EvaluateProjectVersionAsync
    (
      string repoRoot,
      string projectPath,
      CancellationToken cancellationToken
    )
    {
      CommandOutput result = await Shell.Builder("dotnet")
        .WithArguments("msbuild", projectPath, "-getProperty:Version", "-nologo")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);

      if (result.ExitCode != 0)
      {
        throw new InvalidOperationException(
          $"Could not evaluate <Version> for {projectPath}: {result.Stderr}");
      }

      string version = result.Stdout.Trim();
      if (string.IsNullOrWhiteSpace(version) || version.Contains('\n') || version.Contains(' '))
      {
        throw new InvalidOperationException(
          $"MSBuild did not return a single <Version> for {projectPath}.");
      }

      return version;
    }

    private async Task EnsureNuGetIndexKnownAsync
    (
      IReadOnlyList<PackableProject> packableProjects,
      CancellationToken cancellationToken
    )
    {
      Terminal.WriteLine("Checking NuGet.org...");
      using HttpClient client = new();
      foreach (PackableProject project in packableProjects)
      {
        Uri url = NuGetVersionService.GetIndexUrl(project.PackageId);
        HttpResponseMessage response;
        try
        {
          response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
          throw new InvalidOperationException(
            $"NuGet.org index for {project.PackageId} could not be read ({ex.Message}). Availability is unknown.",
            ex);
        }

        using (response)
        {
          int statusCode = (int)response.StatusCode;
          if (!ReleaseGate.IndexStatusIsKnown(statusCode))
          {
            throw new InvalidOperationException(
              $"NuGet.org index for {project.PackageId} returned HTTP {statusCode}. Availability is unknown.");
          }
        }

        Terminal.WriteLine($"  {project.PackageId}: index HTTP 200");
      }
    }

  }
}
