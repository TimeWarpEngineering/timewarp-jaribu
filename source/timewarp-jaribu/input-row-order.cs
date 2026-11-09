namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;

#region Purpose
// Reads [Input] rows in metadata-table order.
#endregion

#region Design
// GetCustomAttributes does not promise declaration order. The custom-attribute
// table does: the C# compiler writes [Input] rows in source order. Each blob
// is decoded only far enough to match it to the attribute instance reflection
// already built, and that instance is what the runner invokes. A lone null
// array matches the one-null row the InputAttribute constructor stores.
// A missing location, a dynamic method, or an unrecognized blob keeps the
// reflection order. The null-array rule still applies through the constructor.
#endregion

/// <summary>
/// Orders <see cref="InputAttribute"/> rows by the assembly metadata table.
/// </summary>
internal static partial class InputRowOrder
{
  private static readonly ConditionalWeakTable<MethodInfo, InputList> Cache = [];

  /// <summary>
  /// Returns the <see cref="InputAttribute"/> rows on <paramref name="method"/> in metadata order.
  /// </summary>
  public static InputAttribute[] Ordered(MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(method);
    return Cache.GetValue(method, static methodInfo => new InputList(Read(methodInfo))).Inputs;
  }

  private static InputAttribute[] Read(MethodInfo method)
  {
    InputAttribute[] reflected = [.. method.GetCustomAttributes<InputAttribute>()];
    if (reflected.Length <= 1)
    {
      return reflected;
    }

    try
    {
      if (TryOrderByMetadata(method, reflected, out InputAttribute[] ordered))
      {
        return ordered;
      }
    }
    catch (BadImageFormatException)
    {
      return reflected;
    }
    catch (IOException)
    {
      return reflected;
    }
    catch (UnauthorizedAccessException)
    {
      return reflected;
    }
    catch (InvalidOperationException)
    {
      return reflected;
    }
    catch (ArgumentException)
    {
      return reflected;
    }

    return reflected;
  }

  private static bool TryOrderByMetadata(
    MethodInfo method,
    InputAttribute[] reflected,
    out InputAttribute[] ordered)
  {
    ordered = reflected;
    if (!TryReadDecodedArguments(method, out List<object?[]> decoded) ||
        decoded.Count != reflected.Length)
    {
      return false;
    }

    bool[] used = new bool[reflected.Length];
    InputAttribute[] result = new InputAttribute[reflected.Length];
    for (int index = 0; index < decoded.Count; index++)
    {
      int match = -1;
      for (int candidate = 0; candidate < reflected.Length; candidate++)
      {
        if (used[candidate])
        {
          continue;
        }

        if (SameArguments(reflected[candidate].Parameters, decoded[index]))
        {
          match = candidate;
          break;
        }
      }

      if (match < 0)
      {
        return false;
      }

      used[match] = true;
      result[index] = reflected[match];
    }

    ordered = result;
    return true;
  }

  private static bool TryReadDecodedArguments(MethodInfo method, out List<object?[]> decoded)
  {
    decoded = [];
#pragma warning disable IL3000 // Single-file hosts have an empty Location. The caller keeps reflection order.
    string location = method.Module.Assembly.Location;
#pragma warning restore IL3000
    if (string.IsNullOrEmpty(location) || !File.Exists(location))
    {
      return false;
    }

    using FileStream stream = new(location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    using PEReader peReader = new(stream, PEStreamOptions.LeaveOpen);
    if (!peReader.HasMetadata)
    {
      return false;
    }

    MetadataReader reader = peReader.GetMetadataReader();
    MethodDefinitionHandle handle = MetadataTokens.MethodDefinitionHandle(method.MetadataToken);
    MethodDefinition definition = reader.GetMethodDefinition(handle);
    foreach (CustomAttributeHandle attributeHandle in definition.GetCustomAttributes())
    {
      CustomAttribute attribute = reader.GetCustomAttribute(attributeHandle);
      if (!IsInputAttribute(reader, attribute.Constructor))
      {
        continue;
      }

      BlobReader blob = reader.GetBlobReader(attribute.Value);
      decoded.Add(ReadArguments(ref blob, method));
    }

    return true;
  }

  private static bool IsInputAttribute(MetadataReader reader, EntityHandle constructor)
  {
    switch (constructor.Kind)
    {
      case HandleKind.MethodDefinition:
        MethodDefinition definition = reader.GetMethodDefinition((MethodDefinitionHandle)constructor);
        TypeDefinition typeDefinition = reader.GetTypeDefinition(definition.GetDeclaringType());
        return IsInputName(reader, typeDefinition.Namespace, typeDefinition.Name);
      case HandleKind.MemberReference:
        MemberReference member = reader.GetMemberReference((MemberReferenceHandle)constructor);
        return member.Parent.Kind switch
        {
          HandleKind.TypeReference => IsTypeReference(reader, (TypeReferenceHandle)member.Parent),
          HandleKind.TypeDefinition => IsTypeDefinition(reader, (TypeDefinitionHandle)member.Parent),
          _ => false
        };
      default:
        return false;
    }
  }

  private static bool IsTypeReference(MetadataReader reader, TypeReferenceHandle handle)
  {
    TypeReference typeReference = reader.GetTypeReference(handle);
    return IsInputName(reader, typeReference.Namespace, typeReference.Name);
  }

  private static bool IsTypeDefinition(MetadataReader reader, TypeDefinitionHandle handle)
  {
    TypeDefinition typeDefinition = reader.GetTypeDefinition(handle);
    return IsInputName(reader, typeDefinition.Namespace, typeDefinition.Name);
  }

  private static bool IsInputName(MetadataReader reader, StringHandle namespaceHandle, StringHandle nameHandle)
  {
    return reader.StringComparer.Equals(nameHandle, nameof(InputAttribute)) &&
      reader.StringComparer.Equals(namespaceHandle, "TimeWarp.Jaribu");
  }

  private static bool SameArguments(object?[] left, object?[] right)
  {
    if (left.Length != right.Length)
    {
      return false;
    }

    for (int index = 0; index < left.Length; index++)
    {
      if (!SameValue(left[index], right[index]))
      {
        return false;
      }
    }

    return true;
  }

  private static bool SameValue(object? left, object? right)
  {
    if (left is null || right is null)
    {
      return left is null && right is null;
    }

    if (left is Array leftArray && right is Array rightArray)
    {
      if (leftArray.Rank != rightArray.Rank ||
          leftArray.Length != rightArray.Length ||
          leftArray.GetType() != rightArray.GetType())
      {
        return false;
      }

      int index = 0;
      foreach (object? item in leftArray)
      {
        if (!SameValue(item, rightArray.GetValue(index)))
        {
          return false;
        }

        index++;
      }

      return true;
    }

    return left.Equals(right);
  }

  private sealed class InputList(InputAttribute[] inputs)
  {
    public InputAttribute[] Inputs { get; } = inputs;
  }
}
