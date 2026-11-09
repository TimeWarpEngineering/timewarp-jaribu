#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include ../../../../tools/dev-cli/endpoints/release-gate.cs

#region Purpose
// Locks the release-gate decisions the workflow uses before it packs or pushes.
#endregion

#region Design
// The helper is the same file the dev CLI compiles. A single-file run includes it.
// The MTP project lists that file itself because a normal project ignores #:include.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace ReleaseGate_
{
  using System.IO.Compression;
  using DevCli.Commands;

  [TestTag("Jaribu")]
  public class Decisions_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Decisions_Given_>();

    public static Task Tag_Should_MatchVPlusEvaluatedVersion()
    {
      ReleaseGate.ExpectedTag("1.0.0-beta.16").ShouldBe("v1.0.0-beta.16");
      ReleaseGate.TagMatchesVersion("v1.0.0-beta.16", "1.0.0-beta.16").ShouldBeTrue();
      ReleaseGate.TagMatchesVersion("1.0.0-beta.16", "1.0.0-beta.16").ShouldBeFalse();
      ReleaseGate.TagMatchesVersion("v1.0.0-beta.15", "1.0.0-beta.16").ShouldBeFalse();
      ReleaseGate.TagMatchesVersion(null, "1.0.0-beta.16").ShouldBeFalse();
      ReleaseGate.TagMatchesVersion("  v1.0.0-beta.16", "1.0.0-beta.16").ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task IndexStatus_Should_BeKnownOnlyOnHttp200()
    {
      ReleaseGate.IndexStatusIsKnown(200).ShouldBeTrue();
      ReleaseGate.IndexStatusIsKnown(404).ShouldBeFalse();
      ReleaseGate.IndexStatusIsKnown(500).ShouldBeFalse();
      ReleaseGate.IndexStatusIsKnown(503).ShouldBeFalse();
      ReleaseGate.IndexStatusIsKnown(0).ShouldBeFalse();
      return Task.CompletedTask;
    }

    public static Task PackageIds_Should_BeTheTwoReleasePackages()
    {
      ReleaseGate.RequirePackageIds(
      [
        "TimeWarp.Jaribu.TestingPlatform",
        "TimeWarp.Jaribu"
      ]);

      Should.Throw<InvalidOperationException>(() => ReleaseGate.RequirePackageIds(["TimeWarp.Jaribu"]));
      Should.Throw<InvalidOperationException>(() =>
        ReleaseGate.RequirePackageIds(
        [
          "TimeWarp.Jaribu",
          "TimeWarp.Jaribu.TestingPlatform",
          "TimeWarp.Jaribu.Extra"
        ]));
      return Task.CompletedTask;
    }

    public static Task FreshPackages_Should_BeExactlyTheFilesJustPacked()
    {
      string root = Directory.CreateTempSubdirectory("jaribu-release-gate-").FullName;
      try
      {
        string stale = Path.Combine(root, "TimeWarp.Jaribu.1.0.0-beta.15.nupkg");
        File.WriteAllText(stale, "stale");
        ReleaseGate.ClearPackageDirectory(root);
        File.Exists(stale).ShouldBeFalse();
        Directory.Exists(root).ShouldBeTrue();

        string version = "1.0.0-beta.16";
        File.WriteAllText(Path.Combine(root, $"TimeWarp.Jaribu.{version}.nupkg"), "core");
        File.WriteAllText(Path.Combine(root, $"TimeWarp.Jaribu.TestingPlatform.{version}.nupkg"), "adapter");
        File.WriteAllText(Path.Combine(root, "Old.Leftover.1.0.0.nupkg"), "leftover");

        Should.Throw<InvalidOperationException>(() =>
          ReleaseGate.RequireFreshPackagePaths(root, ReleaseGate.ReleasePackageIds, version));

        File.Delete(Path.Combine(root, "Old.Leftover.1.0.0.nupkg"));
        IReadOnlyList<string> fresh = ReleaseGate.RequireFreshPackagePaths(
          root,
          ReleaseGate.ReleasePackageIds,
          version);
        fresh.Count.ShouldBe(2);
        Path.GetFileName(fresh[0]).ShouldBe($"TimeWarp.Jaribu.{version}.nupkg");
        Path.GetFileName(fresh[1]).ShouldBe($"TimeWarp.Jaribu.TestingPlatform.{version}.nupkg");

        File.Delete(fresh[0]);
        Should.Throw<InvalidOperationException>(() =>
          ReleaseGate.RequireFreshPackagePaths(root, ReleaseGate.ReleasePackageIds, version));
      }
      finally
      {
        Directory.Delete(root, recursive: true);
      }

      return Task.CompletedTask;
    }

    public static Task PackedAssembly_Should_AcceptALibDllThatGrantsNoInternals()
    {
      string clean = RepoFile(
        "source",
        "timewarp-jaribu",
        "bin",
        "Debug",
        "net10.0",
        "timewarp-jaribu.dll");
      File.Exists(clean).ShouldBeTrue();
      ReleaseGate.ReadInternalsVisibleTo(clean).ShouldBeEmpty();
      ReleaseGate.RequireNoInternalsVisibleTo(clean);

      string root = Directory.CreateTempSubdirectory("jaribu-ivt-clean-").FullName;
      try
      {
        string packagePath = Path.Combine(root, "TimeWarp.Jaribu.0.0.0.nupkg");
        using (FileStream stream = File.Create(packagePath))
        using (ZipArchive zip = new(stream, ZipArchiveMode.Create))
        {
          ZipArchiveEntry entry = zip.CreateEntry("lib/net10.0/TimeWarp.Jaribu.dll");
          using Stream entryStream = entry.Open();
          using FileStream assembly = File.OpenRead(clean);
          assembly.CopyTo(entryStream);
        }

        ReleaseGate.RequirePackageGrantsNoInternals(packagePath);

        string empty = Path.Combine(root, "Empty.0.0.0.nupkg");
        using (FileStream stream = File.Create(empty))
        using (ZipArchive zip = new(stream, ZipArchiveMode.Create))
        {
          zip.CreateEntry("README.md");
        }

        InvalidOperationException missing = Should.Throw<InvalidOperationException>(
          () => ReleaseGate.RequirePackageGrantsNoInternals(empty));
        missing.Message.ShouldContain("no lib assembly");
      }
      finally
      {
        Directory.Delete(root, recursive: true);
      }

      return Task.CompletedTask;
    }

    private static string RepoFile(params string[] parts)
    {
      DirectoryInfo? dir = new(Directory.GetCurrentDirectory());
      while (dir is not null)
      {
        if (File.Exists(Path.Combine(dir.FullName, "timewarp-jaribu.slnx")))
        {
          return Path.Combine([dir.FullName, .. parts]);
        }

        dir = dir.Parent;
      }

      throw new InvalidOperationException("timewarp-jaribu.slnx was not found from the working directory.");
    }
  }
}
