#region Purpose
// Orchestrates the full CI/CD pipeline
#endregion

#region Design
// PR/merge: classify -> clean -> build -> test [-> pack]
// Classify is PullRequestBuildClassifier (TimeWarp.Nuru.DevCli). On a pull
// request that changes only declared non-build paths it prints its message
// and the run exits 0. Push and release do not take that skip. readme.md and
// assets/logo.png are packed, so .timewarp/dev.jsonc lists them under
// pullRequestBuild.buildPaths. There is no local copy of the rule.
// --pack adds the pack step. workflow.yml passes it on the ubuntu leg only; that
// leg uploads artifacts/packages as Packages-<run_number> on master (task 074).
// Release: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push
// Release is the package promotion model (DevCli CiRunPromotion): it builds,
// tests, and packs nothing. It ships the bytes a successful workflow.yml run at
// the tagged commit uploaded. Locate-run excludes pull_request runs and puts push
// runs first; the first candidate with a live Packages-* artifact is downloaded
// into a cleared artifacts/packages.
// Test runs ci-runner and the CI-safe MTP project on the PR/merge pipeline.
// Both inherit JARIBU_FILTER_TAG. The workflow sets it per OS leg.
// The root is RepoRoot.Find when that directory contains timewarp-jaribu.slnx.
// Otherwise the binary-relative walk, then the working directory.
// The release tag must be "v" plus the version `dotnet msbuild -getProperty:Version`
// evaluates for each packable project. A props string search is not used.
// A NuGet flat-container index is known only on HTTP 200. Any other status, and a
// failed request, is unknown and the release stops. On 200, the shared check-version
// handler decides whether that evaluated version is already published.
// Pack deletes artifacts/packages first and rebuilds each packable project with
// JaribuExposeInternals=false so the testing platform omits its test
// InternalsVisibleTo grants. Pack and release both require artifacts/packages to
// hold exactly one nupkg per packable id at the evaluated version, and refuse any
// packed lib assembly that still grants internals. Push sends that list, not a
// glob, with --skip-duplicate.
// Auto-detects mode from GITHUB_EVENT_NAME. A non-empty API key also selects release.
// The key is NUGET_API_KEY or --api-key. Push forwards it only as NUGET_API_KEY.
// NuGet 7.6 and later read that variable, so the key is not a child-process argument.
// OIDC Trusted Publishing supplies the key on the release job; GH_TOKEN there
// lets gh list runs and download the artifact.
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

  [Option("pack", Description = "PR/merge mode: pack the release packages into artifacts/packages after test.")]
  public bool Pack { get; set; }

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
        await RunPrWorkflowAsync(repoRoot, command.Pack, ct);
      }

      return Unit.Value;
    }

    private async Task RunPrWorkflowAsync(string repoRoot, bool pack, CancellationToken cancellationToken)
    {
      cancellationToken.ThrowIfCancellationRequested();

      PullRequestBuildDecision decision = await PullRequestBuildClassifier.DecideAsync(repoRoot, cancellationToken)
        .ConfigureAwait(false);
      Terminal.WriteLine(decision.Message);
      if (!decision.RequiresBuild)
      {
        return;
      }

      int steps = pack ? 4 : 3;
      Terminal.WriteLine(pack ? "CI Pipeline: clean -> build -> test -> pack" : "CI Pipeline: clean -> build -> test");
      Terminal.WriteLine("");

      // Step 1: Clean
      Terminal.WriteLine($"Step 1/{steps}: Clean");
      int exitCode = await DotNet.Clean(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithVerbosity("q")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(cancellationToken);
      if (exitCode != 0) throw new InvalidOperationException("Clean failed!");

      // Step 2: Build
      Terminal.WriteLine($"\nStep 2/{steps}: Build");
      exitCode = await DotNet.Build(Path.Combine(repoRoot, "timewarp-jaribu.slnx"))
        .WithConfiguration("Release")
        .WithWorkingDirectory(repoRoot)
        .RunAsync(cancellationToken);
      if (exitCode != 0) throw new InvalidOperationException("Build failed!");

      // Step 3: Test (ci-runner and the CI-safe MTP project)
      Terminal.WriteLine($"\nStep 3/{steps}: Test");
      await CiTestSuite.RunAsync(repoRoot, cancellationToken).ConfigureAwait(false);

      if (pack)
      {
        // Step 4: Pack the release packages (uploaded as Packages-<run_number> on master)
        Terminal.WriteLine($"\nStep 4/{steps}: Pack");
        await PackAsync(repoRoot, cancellationToken).ConfigureAwait(false);
      }

      Terminal.WriteLine("\n✓ CI Pipeline completed successfully");
    }

    private async Task RunReleaseWorkflowAsync(string repoRoot, string? apiKey, CancellationToken ct)
    {
      Terminal.WriteLine("Release Pipeline: tag-gate -> check-version -> locate-run -> download-artifact -> verify -> push");
      Terminal.WriteLine("");

      // Step 1: Tag gate
      Terminal.WriteLine("Step 1/6: Tag Gate");
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

      // Step 2: NuGet index and the shared check-version gate
      Terminal.WriteLine("\nStep 2/6: Check Version");
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

      // Step 3: Successful CI runs of workflow.yml at the tagged commit
      Terminal.WriteLine("\nStep 3/6: Locate CI Run");
      (string headSha, IReadOnlyList<CiRunSummary> candidateRuns) = await ReleasePromotion.LocateCiRunsAsync(repoRoot, ct)
        .ConfigureAwait(false);
      Terminal.WriteLine($"Found {candidateRuns.Count} candidate CI run(s) for commit {headSha}.");

      // Step 4: Download the newest live Packages-* artifact among them
      Terminal.WriteLine("\nStep 4/6: Download Artifact");
      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      await ReleasePromotion
        .DownloadPackagesArtifactAsync(Terminal, repoRoot, artifactsDir, headSha, candidateRuns, ct)
        .ConfigureAwait(false);

      // Step 5: The downloaded set is exactly the packable set at this version, with no grants
      Terminal.WriteLine("\nStep 5/6: Verify Package Set");
      IReadOnlyList<string> packagePaths = VerifyPackages(artifactsDir, packableProjects, version);
      Terminal.WriteLine("Package set verified.");

      // Step 6: Push the promoted bytes
      Terminal.WriteLine("\nStep 6/6: Push");
      if (string.IsNullOrEmpty(apiKey))
      {
        throw new InvalidOperationException("Release push needs a NuGet API key (NUGET_API_KEY or --api-key).");
      }

      foreach (string packagePath in packagePaths)
      {
        string packageName = Path.GetFileName(packagePath);
        Terminal.WriteLine($"  Pushing {packageName}...");

        int exitCode = await DotNet.NuGet()
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

      Terminal.WriteLine("\n✓ Release Pipeline completed successfully");
      foreach (string packagePath in packagePaths)
      {
        Terminal.WriteLine($"  {Path.GetFileName(packagePath)}");
      }
    }

    private async Task PackAsync(string repoRoot, CancellationToken ct)
    {
      IReadOnlyList<PackableProject> packableProjects = await PackableProjectService
        .GetPackableProjectsAsync(repoRoot, ct)
        .ConfigureAwait(false);
      ReleaseGate.RequirePackageIds([.. packableProjects.Select(project => project.PackageId)]);
      string version = await EvaluateReleaseVersionAsync(repoRoot, packableProjects, ct);

      string artifactsDir = Path.Combine(repoRoot, "artifacts", "packages");
      ReleaseGate.ClearPackageDirectory(artifactsDir);

      foreach (PackableProject project in packableProjects)
      {
        Terminal.WriteLine($"  Packing {project.PackageId} {version}...");

        // GeneratePackageOnBuild is on, so a forced rebuild writes the package. An
        // incremental pack after the Release test build skips CoreCompile and keeps
        // the InternalsVisibleTo grants it compiled in; a clean followed by pack hits
        // NU5026 for the same reason. Rebuild with the grants off instead.
        int exitCode = await DotNet.Build(project.ProjectPath)
          .WithConfiguration("Release")
          .WithNoIncremental()
          .WithProperty("JaribuExposeInternals", "false")
          .WithProperty("PackageOutputPath", artifactsDir + Path.DirectorySeparatorChar)
          .WithWorkingDirectory(repoRoot)
          .RunAsync(ct);

        if (exitCode != 0)
        {
          throw new InvalidOperationException($"Pack {project.PackageId} failed!");
        }
      }

      foreach (string packagePath in VerifyPackages(artifactsDir, packableProjects, version))
      {
        Terminal.WriteLine($"  {Path.GetFileName(packagePath)}");
      }
    }

    private static IReadOnlyList<string> VerifyPackages
    (
      string artifactsDir,
      IReadOnlyList<PackableProject> packableProjects,
      string version
    )
    {
      IReadOnlyList<string> packagePaths = ReleaseGate.RequireFreshPackagePaths(
        artifactsDir,
        [.. packableProjects.Select(project => project.PackageId)],
        version);
      foreach (string packagePath in packagePaths)
      {
        ReleaseGate.RequirePackageGrantsNoInternals(packagePath);
      }

      return packagePaths;
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
      CommandOutput result = await DotNet.MsBuild(projectPath)
        .WithGetProperty("Version")
        .WithNoLogo()
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
