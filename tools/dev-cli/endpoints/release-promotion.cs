#region Purpose
// Release-mode package promotion: find the successful workflow.yml runs at the
// tagged commit and download the Packages-* artifact one of them uploaded.
#endregion

#region Design
// Selection rules are DevCli's CiRunPromotion (TimeWarp.Nuru.DevCli content):
// pull_request runs are excluded, push runs come first, then newest. A run
// counts only when SelectPackagesArtifact finds a live Packages-* artifact, the
// same rule `dev release` step 8 applies, so the local guard and the release
// event agree. Each skipped run is named in the refusal. The artifacts
// directory is cleared before the download so no leftover joins the verified
// set. gh runs in the repo root so {owner}/{repo} resolves from its remote.
// Task 074 (orchestration modeled on timewarp-nuru workflow-command.attestation.cs).
#endregion

namespace DevCli.Commands;

using System.Text.Json;

internal static class ReleasePromotion
{
  public static async Task<(string HeadSha, IReadOnlyList<CiRunSummary> CandidateRuns)> LocateCiRunsAsync
  (
    string repoRoot,
    CancellationToken cancellationToken
  )
  {
    CommandOutput headResult = await Shell.Builder("git")
      .WithArguments("rev-parse", "HEAD")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);
    if (headResult.ExitCode != 0)
    {
      throw new InvalidOperationException($"Could not determine HEAD commit: {headResult.Stderr.Trim()}");
    }

    string headSha = headResult.Stdout.Trim();

    CommandOutput runListResult;
    try
    {
      runListResult = await Gh.Run().List()
        .WithWorkflow("workflow.yml")
        .WithCommit(headSha)
        .WithStatus("success")
        .WithJson("databaseId", "event", "headSha", "createdAt")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
    }
    catch (System.ComponentModel.Win32Exception ex)
    {
      throw new InvalidOperationException(
        "Release promotes CI-built packages and needs the gh CLI. workflow.yml provides GH_TOKEN on the runner.",
        ex);
    }

    if (runListResult.ExitCode != 0)
    {
      throw new InvalidOperationException($"gh run list failed: {runListResult.Stderr.Trim()}");
    }

    List<CiRunSummary>? runs = JsonSerializer.Deserialize(runListResult.Stdout, DevCliJsonContext.Default.ListCiRunSummary);
    IReadOnlyList<CiRunSummary> candidateRuns = CiRunPromotion.OrderCandidateRuns(runs ?? [], headSha);
    if (candidateRuns.Count == 0)
    {
      throw new InvalidOperationException(
        $"No successful CI run of workflow.yml exists for commit {headSha}. Only CI-tested packages are published.");
    }

    return (headSha, candidateRuns);
  }

  public static async Task DownloadPackagesArtifactAsync
  (
    ITerminal terminal,
    string repoRoot,
    string artifactsDir,
    string headSha,
    IReadOnlyList<CiRunSummary> candidateRuns,
    CancellationToken cancellationToken
  )
  {
    List<string> skipped = [];
    foreach (CiRunSummary run in candidateRuns)
    {
      CommandOutput artifactsResult = await Gh.Api($"repos/{{owner}}/{{repo}}/actions/runs/{run.DatabaseId}/artifacts")
        .WithWorkingDirectory(repoRoot)
        .WithNoValidation()
        .CaptureAsync(cancellationToken)
        .ConfigureAwait(false);
      if (artifactsResult.ExitCode != 0)
      {
        throw new InvalidOperationException(
          $"Failed to list artifacts for run {run.DatabaseId}: {artifactsResult.Stderr.Trim()}");
      }

      RunArtifactListResponse? artifactList = JsonSerializer.Deserialize(
        artifactsResult.Stdout,
        DevCliJsonContext.Default.RunArtifactListResponse);
      IReadOnlyList<RunArtifact> artifacts = artifactList?.Artifacts ?? [];
      string? skip = CiRunPromotion.DescribeUnreleasableRun(run, artifacts);
      if (skip is not null)
      {
        skipped.Add(skip);
        continue;
      }

      RunArtifact artifact = CiRunPromotion.SelectPackagesArtifact(artifacts).Artifact!;
      ReleaseGate.ClearPackageDirectory(artifactsDir);
      terminal.WriteLine($"Downloading '{artifact.Name}' from run {run.DatabaseId} ({run.Event})...");

      int exitCode = await Gh.Run()
        .Download(run.DatabaseId.ToString(System.Globalization.CultureInfo.InvariantCulture))
        .WithName(artifact.Name)
        .WithDirectory(artifactsDir)
        .WithWorkingDirectory(repoRoot)
        .RunAsync(cancellationToken)
        .ConfigureAwait(false);
      if (exitCode != 0)
      {
        throw new InvalidOperationException($"Failed to download '{artifact.Name}' from run {run.DatabaseId}.");
      }

      return;
    }

    throw new InvalidOperationException(
      $"No successful CI run for commit {headSha} has a live Packages-* artifact. "
      + $"Re-run CI on that commit (gh run rerun {candidateRuns[0].DatabaseId}). {string.Join("; ", skipped)}");
  }
}
