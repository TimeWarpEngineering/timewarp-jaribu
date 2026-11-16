namespace TimeWarp.Jaribu.Instrument;

using Mono.Cecil;
using Mono.Cecil.Cil;

#region Purpose
// Build-time instrumenter. Every eligible method starts with TautologyProbe.Enter.
#endregion

#region Design
// Constructors are skipped. The CLR requires a base constructor call first, and
// a static constructor that throws would break type load.
// Probe types are skipped so Enter cannot recurse.
// A second run sees InstrumentedAssemblyAttribute and leaves the assembly alone.
#endregion

/// <summary>Rewrites one assembly so production methods check the prove-mode flag.</summary>
internal static class Instrumenter
{
  private static readonly string[] SkippedTypes =
  [
    "TautologyProbe",
    "ProbeBrokenException",
    "InstrumentedAssemblyAttribute",
    "TautologyException"
  ];

  public static void Main(string[] args)
  {
    ArgumentNullException.ThrowIfNull(args);
    if (args.Length != 2)
    {
      throw new ArgumentException("Usage: timewarp-jaribu-instrument <assembly> <probeAssembly>");
    }

    Rewrite(args[0], args[1]);
  }

  private static void Rewrite(string assemblyPath, string probePath)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(assemblyPath);
    ArgumentException.ThrowIfNullOrWhiteSpace(probePath);
    using AssemblyDefinition assembly = Read(assemblyPath);
    if (HasMarker(assembly))
    {
      return;
    }

    bool sameAssembly = string.Equals(
      Path.GetFullPath(assemblyPath),
      Path.GetFullPath(probePath),
      StringComparison.OrdinalIgnoreCase);
    MethodReference enter = ImportEnter(assembly, probePath, sameAssembly);
    AddMarker(assembly, probePath, sameAssembly);

    foreach (ModuleDefinition module in assembly.Modules)
    {
      foreach (TypeDefinition type in AllTypes(module.Types))
      {
        if (SkipType(type))
        {
          continue;
        }

        foreach (MethodDefinition method in type.Methods)
        {
          if (!method.HasBody || method.IsConstructor || method.Body.Instructions.Count == 0)
          {
            continue;
          }

          if (method.Name.Contains('<', StringComparison.Ordinal))
          {
            continue;
          }

          if (method.CustomAttributes.Any(attribute =>
                attribute.AttributeType.Name == "CompilerGeneratedAttribute"))
          {
            continue;
          }

          Insert(method, enter, Token(assembly, method));
        }
      }
    }

    bool symbols = assembly.MainModule.HasSymbols;
    string? directory = Path.GetDirectoryName(assemblyPath);
    if (string.IsNullOrEmpty(directory))
    {
      throw new InvalidOperationException($"'{assemblyPath}' has no directory.");
    }

    string tempDirectory = Path.Combine(directory, "jaribu-instrument-temp");
    Directory.CreateDirectory(tempDirectory);
    string tempPath = Path.Combine(tempDirectory, Path.GetFileName(assemblyPath));
    assembly.Write(tempPath, new WriterParameters
    {
      WriteSymbols = symbols,
      SymbolWriterProvider = symbols ? new DefaultSymbolWriterProvider() : null
    });
    File.Move(tempPath, assemblyPath, overwrite: true);
    string tempPdb = Path.ChangeExtension(tempPath, ".pdb");
    string pdb = Path.ChangeExtension(assemblyPath, ".pdb");
    if (File.Exists(tempPdb))
    {
      File.Move(tempPdb, pdb, overwrite: true);
    }

    Directory.Delete(tempDirectory, recursive: true);
  }

  private static AssemblyDefinition Read(string path)
  {
    try
    {
      return AssemblyDefinition.ReadAssembly(path, new ReaderParameters
      {
        InMemory = true,
        ReadSymbols = true,
        SymbolReaderProvider = new DefaultSymbolReaderProvider(throwIfNoSymbol: false)
      });
    }
    catch (Exception exception) when (exception is BadImageFormatException
      or InvalidOperationException
      or IOException
      or ArgumentException
      or NotSupportedException)
    {
      return AssemblyDefinition.ReadAssembly(path, new ReaderParameters
      {
        InMemory = true,
        ReadSymbols = false
      });
    }
  }

  private static bool HasMarker(AssemblyDefinition assembly)
  {
    return assembly.CustomAttributes.Any(attribute =>
      attribute.AttributeType.Name == "InstrumentedAssemblyAttribute");
  }

  private static MethodReference ImportEnter(
    AssemblyDefinition assembly,
    string probePath,
    bool sameAssembly)
  {
    ModuleDefinition module = assembly.MainModule;
    if (sameAssembly)
    {
      TypeDefinition local = FindType(module, "TimeWarp.Jaribu.TautologyProbe");
      MethodDefinition enter = local.Methods.Single(method => method.Name == "Enter" && method.Parameters.Count == 1);
      return enter;
    }

    using AssemblyDefinition probe = AssemblyDefinition.ReadAssembly(probePath);
    TypeDefinition probeType = FindType(probe.MainModule, "TimeWarp.Jaribu.TautologyProbe");
    MethodDefinition probeEnter = probeType.Methods.Single(method => method.Name == "Enter" && method.Parameters.Count == 1);
    return module.ImportReference(probeEnter);
  }

  private static void AddMarker(AssemblyDefinition assembly, string probePath, bool sameAssembly)
  {
    MethodReference constructor;
    if (sameAssembly)
    {
      TypeDefinition marker = FindType(assembly.MainModule, "TimeWarp.Jaribu.InstrumentedAssemblyAttribute");
      constructor = marker.Methods.Single(method => method.IsConstructor && method.Parameters.Count == 0);
    }
    else
    {
      using AssemblyDefinition probe = AssemblyDefinition.ReadAssembly(probePath);
      TypeDefinition marker = FindType(probe.MainModule, "TimeWarp.Jaribu.InstrumentedAssemblyAttribute");
      MethodDefinition ctor = marker.Methods.Single(method => method.IsConstructor && method.Parameters.Count == 0);
      constructor = assembly.MainModule.ImportReference(ctor);
    }

    assembly.CustomAttributes.Add(new CustomAttribute(constructor));
  }

  private static TypeDefinition FindType(ModuleDefinition module, string fullName)
  {
    foreach (TypeDefinition type in AllTypes(module.Types))
    {
      if (type.FullName == fullName)
      {
        return type;
      }
    }

    throw new InvalidOperationException($"'{fullName}' was not found in '{module.Name}'.");
  }

  private static IEnumerable<TypeDefinition> AllTypes(IEnumerable<TypeDefinition> types)
  {
    foreach (TypeDefinition type in types)
    {
      yield return type;
      foreach (TypeDefinition nested in AllTypes(type.NestedTypes))
      {
        yield return nested;
      }
    }
  }

  private static bool SkipType(TypeDefinition type)
  {
    TypeDefinition current = type;
    while (current.DeclaringType is not null)
    {
      current = current.DeclaringType;
    }

    if (current.Namespace != "TimeWarp.Jaribu")
    {
      return false;
    }

    if (SkippedTypes.Contains(current.Name, StringComparer.Ordinal))
    {
      return true;
    }

    return current.CustomAttributes.Any(attribute =>
      attribute.AttributeType.Name == "CompilerGeneratedAttribute");
  }

  private static string Token(AssemblyDefinition assembly, MethodDefinition method)
  {
    string assemblyName = assembly.Name.Name;
    string typeName = method.DeclaringType.FullName.Replace('/', '+');
    string parameters = string.Join(
      ',',
      method.Parameters.Select(parameter => parameter.ParameterType.FullName.Replace('/', '+')));
    return assemblyName + "|" + typeName + "." + method.Name + "(" + parameters + ")";
  }

  private static void Insert(MethodDefinition method, MethodReference enter, string token)
  {
    ILProcessor processor = method.Body.GetILProcessor();
    Instruction first = method.Body.Instructions[0];
    processor.InsertBefore(first, processor.Create(OpCodes.Ldstr, token));
    processor.InsertBefore(first, processor.Create(OpCodes.Call, enter));
  }
}
