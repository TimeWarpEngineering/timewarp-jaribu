namespace TimeWarp.Jaribu.Instrument;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

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
    MethodReference enter = ImportMethod(assembly, probePath, sameAssembly, "Enter");
    MethodReference leave = ImportMethod(assembly, probePath, sameAssembly, "Leave");
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
          if (SkipMethod(method))
          {
            continue;
          }

          Insert(method, enter, leave, Token(assembly, method));
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
    if (symbols && File.Exists(tempPdb))
    {
      File.Move(tempPdb, pdb, overwrite: true);
    }
    else if (File.Exists(pdb))
    {
      File.Delete(pdb);
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

  private static MethodReference ImportMethod(
    AssemblyDefinition assembly,
    string probePath,
    bool sameAssembly,
    string name)
  {
    ModuleDefinition module = assembly.MainModule;
    if (sameAssembly)
    {
      TypeDefinition local = FindType(module, "TimeWarp.Jaribu.TautologyProbe");
      return local.Methods.Single(method => method.Name == name && method.Parameters.Count == (name == "Enter" ? 1 : 0));
    }

    using AssemblyDefinition probe = AssemblyDefinition.ReadAssembly(probePath);
    TypeDefinition probeType = FindType(probe.MainModule, "TimeWarp.Jaribu.TautologyProbe");
    MethodDefinition probeMethod = probeType.Methods.Single(method =>
      method.Name == name && method.Parameters.Count == (name == "Enter" ? 1 : 0));
    return module.ImportReference(probeMethod);
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
    for (TypeDefinition? current = type; current is not null; current = current.DeclaringType)
    {
      if (current.Name.Contains('<', StringComparison.Ordinal))
      {
        return true;
      }

      if (HasCompilerGenerated(current))
      {
        return true;
      }
    }

    TypeDefinition root = type;
    while (root.DeclaringType is not null)
    {
      root = root.DeclaringType;
    }

    return root.Namespace == "TimeWarp.Jaribu" &&
      SkippedTypes.Contains(root.Name, StringComparer.Ordinal);
  }

  private static bool SkipMethod(MethodDefinition method)
  {
    if (!method.HasBody || method.IsConstructor || method.Body.Instructions.Count == 0)
    {
      return true;
    }

    if (method.Name.Contains('<', StringComparison.Ordinal))
    {
      return true;
    }

    if (!HasCompilerGenerated(method))
    {
      return false;
    }

    return !IsAccessor(method.Name);
  }

  private static void StripTailCalls(MethodBody body)
  {
    // tail. is illegal inside the try this wrap adds.
    ILProcessor processor = body.GetILProcessor();
    foreach (Instruction instruction in body.Instructions.ToList())
    {
      if (instruction.OpCode == OpCodes.Tail)
      {
        processor.Remove(instruction);
      }
    }
  }

  private static void RetargetReturns(MethodBody body, Dictionary<Instruction, Instruction> stores)
  {
    foreach (Instruction instruction in body.Instructions)
    {
      if (instruction.Operand is Instruction target && stores.TryGetValue(target, out Instruction? store))
      {
        instruction.Operand = store;
      }
      else if (instruction.Operand is Instruction[] targets)
      {
        for (int index = 0; index < targets.Length; index++)
        {
          if (stores.TryGetValue(targets[index], out Instruction? switched))
          {
            targets[index] = switched;
          }
        }
      }
    }
  }

  private static bool ReturnsValue(MethodDefinition method)
  {
    TypeReference returnType = method.ReturnType;
    while (returnType is IModifierType modifier)
    {
      // init-only setters are void modreq(IsExternalInit). The modifier is not a value.
      returnType = modifier.ElementType;
    }

    if (returnType.MetadataType == MetadataType.Void || returnType.FullName == "System.Void")
    {
      return false;
    }

    return returnType != method.Module.TypeSystem.Void;
  }

  private static bool IsAccessor(string name)
  {
    return name.StartsWith("get_", StringComparison.Ordinal)
      || name.StartsWith("set_", StringComparison.Ordinal)
      || name.StartsWith("add_", StringComparison.Ordinal)
      || name.StartsWith("remove_", StringComparison.Ordinal);
  }

  private static bool HasCompilerGenerated(ICustomAttributeProvider provider)
  {
    return provider.CustomAttributes.Any(attribute =>
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

  private static void Insert(MethodDefinition method, MethodReference enter, MethodReference leave, string token)
  {
    MethodBody body = method.Body;
    body.SimplifyMacros();
    StripTailCalls(body);
    ILProcessor processor = body.GetILProcessor();
    Instruction originalFirst = body.Instructions[0];
    Instruction load = processor.Create(OpCodes.Ldstr, token);
    Instruction callEnter = processor.Create(OpCodes.Call, enter);
    processor.InsertBefore(originalFirst, load);
    processor.InsertBefore(originalFirst, callEnter);

    Instruction callLeave = processor.Create(OpCodes.Call, leave);
    Instruction endFinally = processor.Create(OpCodes.Endfinally);
    foreach (ExceptionHandler existing in body.ExceptionHandlers)
    {
      existing.TryEnd ??= callLeave;
      existing.HandlerEnd ??= callLeave;
    }

    List<Instruction> returns = [];
    foreach (Instruction instruction in body.Instructions)
    {
      if (instruction.OpCode == OpCodes.Ret)
      {
        returns.Add(instruction);
      }
    }

    bool returnsValue = returns.Count > 0 && ReturnsValue(method);
    Instruction? returnPoint = null;
    if (returns.Count > 0)
    {
      if (returnsValue)
      {
        VariableDefinition result = new(method.ReturnType);
        body.Variables.Add(result);
        body.InitLocals = true;
        returnPoint = processor.Create(OpCodes.Ldloc, result);
        Dictionary<Instruction, Instruction> stores = [];
        foreach (Instruction instruction in returns)
        {
          // A branch that targeted the ret must store too. Otherwise `a || b`
          // leaves the bool on the stack and leave rejects the method.
          Instruction store = processor.Create(OpCodes.Stloc, result);
          processor.InsertBefore(instruction, store);
          stores.Add(instruction, store);
          instruction.OpCode = OpCodes.Leave;
          instruction.Operand = returnPoint;
        }

        RetargetReturns(body, stores);
      }
      else
      {
        returnPoint = processor.Create(OpCodes.Ret);
        foreach (Instruction instruction in returns)
        {
          instruction.OpCode = OpCodes.Leave;
          instruction.Operand = returnPoint;
        }
      }
    }

    processor.Append(callLeave);
    processor.Append(endFinally);
    if (returnPoint is not null)
    {
      processor.Append(returnPoint);
      if (returnsValue)
      {
        processor.Append(processor.Create(OpCodes.Ret));
      }
    }

    body.ExceptionHandlers.Add(new ExceptionHandler(ExceptionHandlerType.Finally)
    {
      TryStart = load,
      TryEnd = callLeave,
      HandlerStart = callLeave,
      HandlerEnd = returnPoint
    });
    if (body.MaxStackSize < 1)
    {
      body.MaxStackSize = 1;
    }

    body.OptimizeMacros();
  }
}
