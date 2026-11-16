namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

#region Purpose
// file:line for a tautology finding, from the test method's portable PDB.
#endregion

#region Design
// A missing PDB, a missing sequence point, or an unreadable image yields the
// method name and line 0. The report still has a location column. This reader
// does not cache. Coverage and prove mode are not the inner loop.
#endregion

/// <summary>Resolves <c>file:line</c> for a test method.</summary>
internal static class TautologyLocation
{
  /// <summary>
  /// Returns <c>path:line</c> using a 1-based line, or <c>Type.Method:0</c> when the PDB has no span.
  /// </summary>
  public static string Format(MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(method);
    string fallback = Fallback(method);
    try
    {
      string? assemblyPath = method.Module.Assembly.Location;
      if (string.IsNullOrEmpty(assemblyPath))
      {
        return fallback;
      }

      using MetadataReaderProvider? provider = Open(assemblyPath);
      if (provider is null)
      {
        return fallback;
      }

      MetadataReader reader = provider.GetMetadataReader();
      MethodDefinitionHandle handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
      return TrySpan(reader, handle) ?? fallback;
    }
    catch (Exception exception) when (exception is BadImageFormatException
      or IOException
      or InvalidOperationException
      or UnauthorizedAccessException
      or ArgumentException
      or NotSupportedException)
    {
      return fallback;
    }
  }

  private static string Fallback(MethodInfo method)
  {
    string typeName = method.DeclaringType?.Name ?? "method";
    return typeName + "." + method.Name + ":0";
  }

  private static MetadataReaderProvider? Open(string assemblyPath)
  {
    string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
    if (File.Exists(pdbPath))
    {
      FileStream? stream = File.OpenRead(pdbPath);
      try
      {
        MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(stream);
        stream = null;
        return provider;
      }
      finally
      {
        stream?.Dispose();
      }
    }

    using FileStream peStream = File.OpenRead(assemblyPath);
    using PEReader peReader = new(peStream, PEStreamOptions.LeaveOpen);
    foreach (DebugDirectoryEntry entry in peReader.ReadDebugDirectory())
    {
      if (entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb)
      {
        return peReader.ReadEmbeddedPortablePdbDebugDirectoryData(entry);
      }
    }

    return null;
  }

  private static string? TrySpan(MetadataReader reader, MethodDefinitionHandle handle)
  {
    MethodDebugInformation info = reader.GetMethodDebugInformation(handle);
    string? path = null;
    int line = 0;
    foreach (SequencePoint point in info.GetSequencePoints())
    {
      if (point.IsHidden || point.StartLine <= 0)
      {
        continue;
      }

      if (path is null && !point.Document.IsNil)
      {
        path = reader.GetString(reader.GetDocument(point.Document).Name);
      }

      if (line == 0 || point.StartLine < line)
      {
        line = point.StartLine;
      }
    }

    if (string.IsNullOrEmpty(path) || line == 0)
    {
      return null;
    }

    return path + ":" + line.ToString(System.Globalization.CultureInfo.InvariantCulture);
  }
}
