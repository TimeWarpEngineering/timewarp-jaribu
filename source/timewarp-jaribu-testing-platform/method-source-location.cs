namespace TimeWarp.Jaribu.TestingPlatform;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Testing.Platform.Extensions.Messages;

#region Purpose
// Reads a method span from a portable PDB so MTP can navigate to the test.
#endregion

#region Design
// Sequence points are optional. A missing assembly path, a missing PDB, or an unreadable
// image yields no property and does not fail the run. PDB lines and columns are 1-based;
// LinePosition is 0-based, matching Roslyn.
#endregion

/// <summary>
/// Resolves <see cref="TestFileLocationProperty"/> from a method's portable PDB when one is present.
/// </summary>
internal static class MethodSourceLocation
{
  public static TestFileLocationProperty? TryCreate(MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(method);

    try
    {
      string? assemblyPath = method.Module.Assembly.Location;
      if (string.IsNullOrEmpty(assemblyPath))
      {
        return null;
      }

      string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
      if (!File.Exists(pdbPath))
      {
        return null;
      }

      using FileStream pdbStream = File.OpenRead(pdbPath);
      using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
      MetadataReader reader = provider.GetMetadataReader();
      MethodDefinitionHandle handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
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
    catch (BadImageFormatException)
    {
      return null;
    }
    catch (IOException)
    {
      return null;
    }
    catch (InvalidOperationException)
    {
      return null;
    }
    catch (UnauthorizedAccessException)
    {
      return null;
    }
  }
}
