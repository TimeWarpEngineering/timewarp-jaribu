namespace TimeWarp.Jaribu.TestingPlatform;

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.Testing.Platform.Extensions.Messages;

#region Purpose
// Reads a method span from a portable PDB so MTP can navigate to the test.
#endregion

#region Design
// Sequence points are optional. A missing assembly path, a missing PDB, or an unreadable
// image yields no property and does not fail the run. ArgumentException from a bad
// metadata token is the same miss. A side-by-side portable PDB wins. When that file
// is absent, an embedded portable PDB in the PE debug directory is used.
// Each miss is written to trace under the Jaribu.Pdb category.
// PDB lines and columns are 1-based; LinePosition is 0-based, matching Roslyn.
// Async and iterator methods store sequence points on the generated MoveNext. A method
// row with none uses the portable PDB state-machine kickoff map to read that MoveNext.
// The resolved span, including a miss, is cached per MethodInfo so discovery, in-progress,
// and completion do not reopen the portable PDB for every [Input] row.
#endregion

/// <summary>
/// Resolves <see cref="TestFileLocationProperty"/> from a method's portable PDB when one is present.
/// </summary>
internal static class MethodSourceLocation
{
  private static readonly ConcurrentDictionary<MethodInfo, LocationCacheEntry> LocationCache = new();
  private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<int, int>> MoveNextTokenByKickoffToken = new();

  public static TestFileLocationProperty? TryCreate(MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(method);
    LocationCacheEntry locationCacheEntry = LocationCache.GetOrAdd(
      method,
      static methodInfo => new LocationCacheEntry(Read(methodInfo)));
    return locationCacheEntry.Location;
  }

  private readonly record struct PdbSource(MetadataReaderProvider Provider, string CacheKey);

  private static TestFileLocationProperty? Read(MethodInfo method)
  {
    try
    {
      string? assemblyPath = method.Module.Assembly.Location;
      if (string.IsNullOrEmpty(assemblyPath))
      {
        return Miss(method, "assembly location is empty");
      }

      PdbSource? source = OpenPdb(assemblyPath);
      if (source is null)
      {
        return Miss(method, "no side-by-side or embedded portable PDB");
      }

      using MetadataReaderProvider provider = source.Value.Provider;
      MetadataReader reader = provider.GetMetadataReader();
      MethodDefinitionHandle handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
      TestFileLocationProperty? location = TryReadSpan(reader, handle);
      if (location is not null)
      {
        return location;
      }

      // Async and iterator sequence points live on the generated MoveNext.
      IReadOnlyDictionary<int, int> moveNextTokenByKickoffToken = MoveNextTokenByKickoffToken.GetOrAdd(
        source.Value.CacheKey,
        static (_, metadataReader) => BuildMoveNextMap(metadataReader),
        reader);
      if (!moveNextTokenByKickoffToken.TryGetValue(method.MetadataToken, out int moveNextToken))
      {
        return Miss(method, "no sequence points for the method");
      }

      TestFileLocationProperty? moveNext = TryReadSpan(
        reader,
        MetadataTokens.MethodDefinitionHandle(moveNextToken));
      return moveNext ?? Miss(method, "state-machine MoveNext has no sequence points");
    }
    catch (Exception ex) when (ex is BadImageFormatException
      or IOException
      or InvalidOperationException
      or UnauthorizedAccessException
      or ArgumentException
      or NotSupportedException)
    {
      return Miss(method, ex.GetType().Name);
    }
  }

  private static PdbSource? OpenPdb(string assemblyPath)
  {
    string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
    if (File.Exists(pdbPath))
    {
      // FromPortablePdbStream owns the stream after it returns. A throw before that
      // still owns the stream here, so the finally disposes only that failure path.
      FileStream? pdbStream = File.OpenRead(pdbPath);
      try
      {
        MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
        pdbStream = null;
        return new PdbSource(provider, pdbPath);
      }
      finally
      {
        pdbStream?.Dispose();
      }
    }

    MetadataReaderProvider? embedded = TryOpenEmbedded(assemblyPath);
    if (embedded is null)
    {
      return null;
    }

    return new PdbSource(embedded, assemblyPath + "#embedded");
  }

  private static MetadataReaderProvider? TryOpenEmbedded(string assemblyPath)
  {
    using FileStream peStream = File.OpenRead(assemblyPath);
    using PEReader peReader = new(peStream, PEStreamOptions.LeaveOpen);
    foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
    {
      if (entry.Type != DebugDirectoryEntryType.EmbeddedPortablePdb)
      {
        continue;
      }

      return peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
    }

    return null;
  }

  private static TestFileLocationProperty? Miss(MethodInfo method, string reason)
  {
    Trace.WriteLine(
      $"Jaribu PDB lookup miss for {method.DeclaringType?.FullName}.{method.Name}: {reason}",
      "Jaribu.Pdb");
    return null;
  }

  private static Dictionary<int, int> BuildMoveNextMap(MetadataReader reader)
  {
    Dictionary<int, int> map = [];
    foreach (MethodDebugInformationHandle infoHandle in reader.MethodDebugInformation)
    {
      MethodDebugInformation info = reader.GetMethodDebugInformation(infoHandle);
      MethodDefinitionHandle kickoff = info.GetStateMachineKickoffMethod();
      if (kickoff.IsNil)
      {
        continue;
      }

      map[MetadataTokens.GetToken(kickoff)] = MetadataTokens.GetToken(infoHandle.ToDefinitionHandle());
    }

    return map;
  }

  private static TestFileLocationProperty? TryReadSpan(MetadataReader reader, MethodDefinitionHandle handle)
  {
    MethodDebugInformation info = reader.GetMethodDebugInformation(handle);
    SequencePointCollection points = info.GetSequencePoints();
    DocumentHandle document = default;
    int startLine = int.MaxValue;
    int startColumn = 0;
    int endLine = 0;
    int endColumn = 0;
    foreach (SequencePoint point in points)
    {
      if (point.IsHidden || point.StartLine <= 0)
      {
        continue;
      }

      if (document.IsNil && !point.Document.IsNil)
      {
        document = point.Document;
      }

      if (point.StartLine < startLine)
      {
        startLine = point.StartLine;
        startColumn = point.StartColumn;
      }

      if (point.EndLine > endLine)
      {
        endLine = point.EndLine;
        endColumn = point.EndColumn;
      }
    }

    if (document.IsNil || startLine == int.MaxValue)
    {
      return null;
    }

    string path = reader.GetString(reader.GetDocument(document).Name);
    if (string.IsNullOrEmpty(path))
    {
      return null;
    }

    int startLineZero = Math.Max(0, startLine - 1);
    int startColumnZero = Math.Max(0, startColumn - 1);
    int endLineZero = Math.Max(startLineZero, endLine - 1);
    int endColumnZero = Math.Max(0, endColumn - 1);
    return new TestFileLocationProperty(
      path,
      new LinePositionSpan(
        new LinePosition(startLineZero, startColumnZero),
        new LinePosition(endLineZero, endColumnZero)));
  }

  private sealed record LocationCacheEntry(TestFileLocationProperty? Location);
}
