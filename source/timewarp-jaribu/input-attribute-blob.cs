namespace TimeWarp.Jaribu;

using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

#region Purpose
// Decodes an [Input] custom-attribute blob into argument values.
#endregion

#region Design
// The blob is the ECMA-335 custom-attribute format for one params object?[]
// argument. A length of -1 is the compiler's lone null and becomes one null
// argument. Element tags cover the primitives, strings, typeof, enums, and
// single-dimensional arrays the C# compiler emits. Anything else throws so
// the caller keeps reflection order.
#endregion

internal static partial class InputRowOrder
{
  private static object?[] ReadArguments(ref BlobReader reader, MethodInfo method)
  {
    if (reader.ReadUInt16() != 1)
    {
      throw new InvalidOperationException("Custom attribute prolog is not 1.");
    }

    int length = reader.ReadInt32();
    object?[] values;
    if (length == -1)
    {
      values = [null];
    }
    else if (length < 0)
    {
      throw new InvalidOperationException("Custom attribute array length is negative.");
    }
    else
    {
      values = new object?[length];
      for (int index = 0; index < length; index++)
      {
        values[index] = ReadBoxed(ref reader, method);
      }
    }

    if (reader.ReadUInt16() != 0 || reader.RemainingBytes != 0)
    {
      throw new InvalidOperationException("Custom attribute has unsupported trailing data.");
    }

    return values;
  }

  private static object? ReadBoxed(ref BlobReader reader, MethodInfo method)
  {
    byte code = reader.ReadByte();
    if (code == 0xFF)
    {
      return null;
    }

    return ReadTyped(ref reader, code, method);
  }

  private static object? ReadTyped(ref BlobReader reader, byte code, MethodInfo method)
  {
    switch (code)
    {
      case 0x02:
        return reader.ReadByte() != 0;
      case 0x03:
        return (char)reader.ReadUInt16();
      case 0x04:
        return reader.ReadSByte();
      case 0x05:
        return reader.ReadByte();
      case 0x06:
        return reader.ReadInt16();
      case 0x07:
        return reader.ReadUInt16();
      case 0x08:
        return reader.ReadInt32();
      case 0x09:
        return reader.ReadUInt32();
      case 0x0A:
        return reader.ReadInt64();
      case 0x0B:
        return reader.ReadUInt64();
      case 0x0C:
        return reader.ReadSingle();
      case 0x0D:
        return reader.ReadDouble();
      case 0x0E:
        return ReadSerString(ref reader);
      case 0x1D:
        return ReadSzArray(ref reader, method);
      case 0x50:
        string? typeName = ReadSerString(ref reader);
        if (typeName is null)
        {
          throw new InvalidOperationException("Attribute type name is null.");
        }

        return ResolveType(typeName, method);
      case 0x55:
        return ReadEnum(ref reader, method);
      default:
        throw new InvalidOperationException($"Unsupported attribute element 0x{code:X2}.");
    }
  }

  private static Array ReadSzArray(ref BlobReader reader, MethodInfo method)
  {
    byte elementCode = reader.ReadByte();
    Type? enumType = null;
    if (elementCode == 0x55)
    {
      string? enumName = ReadSerString(ref reader);
      if (enumName is null)
      {
        throw new InvalidOperationException("Enum array type name is null.");
      }

      enumType = ResolveType(enumName, method);
    }

    int length = reader.ReadInt32();
    if (length < 0)
    {
      throw new InvalidOperationException("Attribute array length is negative.");
    }

    Type elementType = enumType ?? ElementType(elementCode);
    Array array = Array.CreateInstance(elementType, length);
    for (int index = 0; index < length; index++)
    {
      object? value = elementCode == 0x1C
        ? ReadBoxed(ref reader, method)
        : ReadRawElement(ref reader, elementCode, enumType);
      array.SetValue(value, index);
    }

    return array;
  }

  private static object? ReadRawElement(ref BlobReader reader, byte code, Type? enumType)
  {
    if (enumType is not null)
    {
      return ReadEnumValue(ref reader, enumType);
    }

    return code switch
    {
      0x02 => reader.ReadByte() != 0,
      0x03 => (char)reader.ReadUInt16(),
      0x04 => reader.ReadSByte(),
      0x05 => reader.ReadByte(),
      0x06 => reader.ReadInt16(),
      0x07 => reader.ReadUInt16(),
      0x08 => reader.ReadInt32(),
      0x09 => reader.ReadUInt32(),
      0x0A => reader.ReadInt64(),
      0x0B => reader.ReadUInt64(),
      0x0C => reader.ReadSingle(),
      0x0D => reader.ReadDouble(),
      0x0E => ReadSerString(ref reader),
      _ => throw new InvalidOperationException($"Unsupported attribute array element 0x{code:X2}.")
    };
  }

  private static object ReadEnum(ref BlobReader reader, MethodInfo method)
  {
    string? name = ReadSerString(ref reader);
    if (name is null)
    {
      throw new InvalidOperationException("Enum type name is null.");
    }

    return ReadEnumValue(ref reader, ResolveType(name, method));
  }

  private static object ReadEnumValue(ref BlobReader reader, Type enumType)
  {
    Type underlying = Enum.GetUnderlyingType(enumType);
    object raw;
    if (underlying == typeof(byte))
    {
      raw = reader.ReadByte();
    }
    else if (underlying == typeof(sbyte))
    {
      raw = reader.ReadSByte();
    }
    else if (underlying == typeof(short))
    {
      raw = reader.ReadInt16();
    }
    else if (underlying == typeof(ushort))
    {
      raw = reader.ReadUInt16();
    }
    else if (underlying == typeof(int))
    {
      raw = reader.ReadInt32();
    }
    else if (underlying == typeof(uint))
    {
      raw = reader.ReadUInt32();
    }
    else if (underlying == typeof(long))
    {
      raw = reader.ReadInt64();
    }
    else if (underlying == typeof(ulong))
    {
      raw = reader.ReadUInt64();
    }
    else
    {
      throw new InvalidOperationException($"Unsupported enum underlying type '{underlying}'.");
    }

    return Enum.ToObject(enumType, raw);
  }

  private static Type ElementType(byte code)
  {
    return code switch
    {
      0x02 => typeof(bool),
      0x03 => typeof(char),
      0x04 => typeof(sbyte),
      0x05 => typeof(byte),
      0x06 => typeof(short),
      0x07 => typeof(ushort),
      0x08 => typeof(int),
      0x09 => typeof(uint),
      0x0A => typeof(long),
      0x0B => typeof(ulong),
      0x0C => typeof(float),
      0x0D => typeof(double),
      0x0E => typeof(string),
      0x1C => typeof(object),
      _ => throw new InvalidOperationException($"Unsupported attribute array type 0x{code:X2}.")
    };
  }

  private static string? ReadSerString(ref BlobReader reader)
  {
    byte first = reader.ReadByte();
    if (first == 0xFF)
    {
      return null;
    }

    int length;
    if ((first & 0x80) == 0)
    {
      length = first;
    }
    else if ((first & 0xC0) == 0x80)
    {
      length = ((first & 0x3F) << 8) | reader.ReadByte();
    }
    else if ((first & 0xE0) == 0xC0)
    {
      length = ((first & 0x1F) << 24) |
        (reader.ReadByte() << 16) |
        (reader.ReadByte() << 8) |
        reader.ReadByte();
    }
    else
    {
      throw new InvalidOperationException("Custom attribute string length is invalid.");
    }

    byte[] bytes = reader.ReadBytes(length);
    return Encoding.UTF8.GetString(bytes);
  }

  private static Type ResolveType(string name, MethodInfo method)
  {
    Type? resolved = Type.GetType(name, throwOnError: false);
    if (resolved is not null)
    {
      return resolved;
    }

    Assembly assembly = method.Module.Assembly;
    resolved = assembly.GetType(name, throwOnError: false, ignoreCase: false);
    if (resolved is not null)
    {
      return resolved;
    }

    int comma = name.IndexOf(',', StringComparison.Ordinal);
    if (comma > 0)
    {
      string typeName = name[..comma].Trim();
      resolved = Type.GetType(typeName, throwOnError: false) ??
        assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
      if (resolved is not null)
      {
        return resolved;
      }
    }

    throw new InvalidOperationException($"Attribute type '{name}' was not found.");
  }

}
