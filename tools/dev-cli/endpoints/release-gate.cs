#region Purpose
// Decisions the release workflow applies before it packs or pushes.
#endregion

#region Design
// The release tag is "v" plus the version MSBuild evaluates. Tags in this repo
// already use that form. A string search of Directory.Build.props is not a version.
// A NuGet flat-container index is a known answer only on HTTP 200. 404 and 5xx
// are unknown, so the workflow refuses instead of treating the version as available.
// Membership of that version in a 200 body belongs to the shared check-version command.
// After pack, artifacts/packages must contain exactly one nupkg per freshly packed
// id. A glob would also push leftovers that dotnet clean does not delete.
#endregion

namespace DevCli.Commands;

internal static class ReleaseGate
{
  public static readonly string[] ReleasePackageIds =
  [
    "TimeWarp.Jaribu",
    "TimeWarp.Jaribu.TestingPlatform"
  ];

  public static string ExpectedTag(string version)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(version);
    return $"v{version}";
  }

  public static bool TagMatchesVersion(string? tag, string version) =>
    string.Equals(tag, ExpectedTag(version), StringComparison.Ordinal);

  public static bool IndexStatusIsKnown(int statusCode) => statusCode == 200;

  public static void RequirePackageIds(IReadOnlyList<string> packageIds)
  {
    ArgumentNullException.ThrowIfNull(packageIds);

    HashSet<string> actual = new(packageIds, StringComparer.Ordinal);
    HashSet<string> expected = new(ReleasePackageIds, StringComparer.Ordinal);
    if (actual.SetEquals(expected))
    {
      return;
    }

    string derived = string.Join(", ", actual.OrderBy(id => id, StringComparer.Ordinal));
    throw new InvalidOperationException(
      $"Release packs exactly {string.Join(" and ", ReleasePackageIds)}. Derived set: {derived}.");
  }

  public static void ClearPackageDirectory(string artifactsDirectory)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(artifactsDirectory);

    if (Directory.Exists(artifactsDirectory))
    {
      Directory.Delete(artifactsDirectory, recursive: true);
    }

    Directory.CreateDirectory(artifactsDirectory);
  }

  public static IReadOnlyList<string> RequireFreshPackagePaths
  (
    string artifactsDirectory,
    IReadOnlyList<string> packageIds,
    string version
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(artifactsDirectory);
    ArgumentNullException.ThrowIfNull(packageIds);
    ArgumentException.ThrowIfNullOrWhiteSpace(version);

    if (packageIds.Count == 0)
    {
      throw new InvalidOperationException("Release produced no packages to push.");
    }

    List<string> expectedNames = [];
    foreach (string packageId in packageIds)
    {
      ArgumentException.ThrowIfNullOrWhiteSpace(packageId);
      expectedNames.Add($"{packageId}.{version}.nupkg");
    }

    HashSet<string> expected = new(expectedNames, StringComparer.Ordinal);
    if (expected.Count != expectedNames.Count)
    {
      throw new InvalidOperationException("Release package ids are not unique.");
    }

    HashSet<string> actualNames = [];
    foreach (string path in Directory.GetFiles(artifactsDirectory, "*.nupkg"))
    {
      actualNames.Add(Path.GetFileName(path));
    }

    List<string> missing = [.. expected.Where(name => !actualNames.Contains(name)).OrderBy(name => name, StringComparer.Ordinal)];
    List<string> unexpected = [.. actualNames.Where(name => !expected.Contains(name)).OrderBy(name => name, StringComparer.Ordinal)];
    if (missing.Count > 0 || unexpected.Count > 0)
    {
      throw new InvalidOperationException(
        $"Fresh package set mismatch. Missing: {Join(missing)}. Unexpected: {Join(unexpected)}.");
    }

    return [.. expectedNames.Select(name => Path.Combine(artifactsDirectory, name))];
  }

  private static string Join(List<string> names) =>
    names.Count == 0 ? "(none)" : string.Join(", ", names);
}
