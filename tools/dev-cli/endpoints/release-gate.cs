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
// Each packed lib assembly must carry no InternalsVisibleTo attribute. The testing
// platform compiles test grants unless JaribuExposeInternals is false, and the
// release pack sets that property. This check is what stops those grants shipping.
#endregion

namespace DevCli.Commands;

using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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

  public static IReadOnlyList<string> ReadInternalsVisibleTo(string assemblyPath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
    using FileStream stream = File.OpenRead(assemblyPath);
    return ReadInternalsVisibleTo(stream, Path.GetFileName(assemblyPath));
  }

  public static void RequireNoInternalsVisibleTo(string assemblyPath)
  {
    IReadOnlyList<string> friends = ReadInternalsVisibleTo(assemblyPath);
    if (friends.Count == 0)
    {
      return;
    }

    throw new InvalidOperationException(
      $"{Path.GetFileName(assemblyPath)} grants internals to {string.Join(", ", friends)}.");
  }

  public static void RequirePackageGrantsNoInternals(string packagePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);

    int checkedAssemblies = 0;
    using ZipArchive zip = ZipFile.OpenRead(packagePath);
    foreach (ZipArchiveEntry entry in zip.Entries)
    {
      if (!IsPackedLibAssembly(entry.FullName))
      {
        continue;
      }

      using Stream entryStream = entry.Open();
      using MemoryStream copy = new();
      entryStream.CopyTo(copy);
      copy.Position = 0;
      List<string> friends = ReadInternalsVisibleTo(copy, entry.Name);
      checkedAssemblies++;
      if (friends.Count > 0)
      {
        throw new InvalidOperationException(
          $"{entry.Name} in {Path.GetFileName(packagePath)} grants internals to {string.Join(", ", friends)}.");
      }
    }

    if (checkedAssemblies == 0)
    {
      throw new InvalidOperationException(
        $"{Path.GetFileName(packagePath)} contains no lib assembly to check.");
    }
  }

  private static bool IsPackedLibAssembly(string entryName)
  {
    string name = entryName.Replace('\\', '/');
    return name.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) &&
      name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) &&
      !name.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase);
  }

  private static List<string> ReadInternalsVisibleTo(Stream assembly, string displayName)
  {
    try
    {
      using PEReader peReader = new(assembly, PEStreamOptions.LeaveOpen);
      if (!peReader.HasMetadata)
      {
        throw new InvalidOperationException($"{displayName} has no CLI metadata.");
      }

      MetadataReader reader = peReader.GetMetadataReader();
      List<string> friends = [];
      foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
      {
        string? friend = ReadFriendName(reader, reader.GetCustomAttribute(handle));
        if (friend is not null)
        {
          friends.Add(friend);
        }
      }

      return friends;
    }
    catch (BadImageFormatException ex)
    {
      throw new InvalidOperationException($"{displayName} is not a managed assembly.", ex);
    }
  }

  private static string? ReadFriendName(MetadataReader reader, CustomAttribute attribute)
  {
    if (!IsInternalsVisibleToConstructor(reader, attribute.Constructor))
    {
      return null;
    }

    BlobReader blob = reader.GetBlobReader(attribute.Value);
    if (blob.ReadUInt16() != 1)
    {
      throw new InvalidOperationException("InternalsVisibleTo attribute prolog is not 0x0001.");
    }

    string? friend = ReadSerializedString(ref blob);
    return friend ?? string.Empty;
  }

  private static bool IsInternalsVisibleToConstructor(MetadataReader reader, EntityHandle constructor)
  {
    if (constructor.Kind != HandleKind.MemberReference)
    {
      return false;
    }

    MemberReference member = reader.GetMemberReference((MemberReferenceHandle)constructor);
    if (member.Parent.Kind != HandleKind.TypeReference)
    {
      return false;
    }

    TypeReference type = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
    MetadataStringComparer comparer = reader.StringComparer;
    return comparer.Equals(type.Namespace, "System.Runtime.CompilerServices") &&
      comparer.Equals(type.Name, "InternalsVisibleToAttribute");
  }

  private static string? ReadSerializedString(ref BlobReader reader)
  {
    if (reader.RemainingBytes == 0)
    {
      throw new InvalidOperationException("InternalsVisibleTo attribute has no argument.");
    }

    byte marker = reader.ReadByte();
    if (marker == 0xFF)
    {
      return null;
    }

    int length = marker;
    if ((marker & 0x80) != 0)
    {
      if ((marker & 0xC0) == 0x80)
      {
        length = ((marker & 0x3F) << 8) | reader.ReadByte();
      }
      else if ((marker & 0xE0) == 0xC0)
      {
        length = ((marker & 0x1F) << 24) |
          (reader.ReadByte() << 16) |
          (reader.ReadByte() << 8) |
          reader.ReadByte();
      }
      else
      {
        throw new InvalidOperationException("InternalsVisibleTo argument length is not a compressed integer.");
      }
    }

    return reader.ReadUTF8(length);
  }

  private static string Join(List<string> names) =>
    names.Count == 0 ? "(none)" : string.Join(", ", names);
}
