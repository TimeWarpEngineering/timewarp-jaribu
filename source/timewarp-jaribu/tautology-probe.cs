namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Records production methods a test enters and throws when prove mode breaks one.
#endregion

#region Design
// The instrumenter inserts Enter at the start of each production method.
// Enter returns immediately when no scope is active, so an instrumented build
// stays quiet during a normal run. The runner pushes a scope only for coverage
// or prove mode, and it records only while the test body is running.
// A broken prefix matches every overload of the claimed member.
// The probe types themselves are not instrumented, so Enter cannot recurse.
#endregion

/// <summary>
/// In-process hook the instrumented build calls on method entry.
/// </summary>
public static class TautologyProbe
{
  private static readonly AsyncLocal<Scope?> Ambient = new();
  private static bool InstrumentedSeen;

  /// <summary>
  /// Records <paramref name="token"/> and throws <see cref="ProbeBrokenException"/> when that member is broken.
  /// A null scope or a paused scope returns without touching the token.
  /// </summary>
  /// <param name="token">
  /// <c>Assembly|Namespace.Type.Method(parameter types)</c> written by the instrumenter.
  /// </param>
  public static void Enter(string token)
  {
    Scope? scope = Ambient.Value;
    if (scope is null || !scope.Recording || token is null)
    {
      return;
    }

    if (scope.Seen.Add(token))
    {
      scope.Order.Add(token);
    }

    if (scope.BrokenPrefix is not null &&
        token.StartsWith(scope.BrokenPrefix, StringComparison.Ordinal))
    {
      throw new ProbeBrokenException(token);
    }
  }

  /// <summary>True when this async flow has a coverage or prove scope.</summary>
  internal static bool IsActive => Ambient.Value is not null;

  /// <summary>Turns body recording on or off. Setup and cleanup stay off.</summary>
  internal static void SetRecording(bool recording)
  {
    if (Ambient.Value is Scope scope)
    {
      scope.Recording = recording;
    }
  }

  /// <summary>Pushes a scope for one test attempt. The caller pops it.</summary>
  internal static Scope Push(string? brokenPrefix)
  {
    Scope scope = new()
    {
      Previous = Ambient.Value,
      BrokenPrefix = brokenPrefix
    };
    Ambient.Value = scope;
    return scope;
  }

  /// <summary>Restores the scope that was active before <paramref name="scope"/>.</summary>
  internal static void Pop(Scope scope)
  {
    ArgumentNullException.ThrowIfNull(scope);
    Ambient.Value = scope.Previous;
  }

  /// <summary>
  /// True when a loaded assembly carries <see cref="InstrumentedAssemblyAttribute"/>.
  /// A true result is remembered. A false result is checked again after more assemblies load.
  /// </summary>
  internal static bool AnyInstrumented()
  {
    if (InstrumentedSeen)
    {
      return true;
    }

    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
    {
      try
      {
        if (assembly.GetCustomAttributes(typeof(InstrumentedAssemblyAttribute), inherit: false).Length > 0)
        {
          InstrumentedSeen = true;
          return true;
        }
      }
      catch (Exception exception) when (exception is FileNotFoundException
        or FileLoadException
        or TypeLoadException
        or ReflectionTypeLoadException
        or BadImageFormatException)
      {
        // An unrelated loaded assembly can fail reflection. It is not the probe signal.
      }
    }

    return false;
  }

  /// <summary>One test attempt's hits and break prefix.</summary>
  internal sealed class Scope
  {
    public Scope? Previous { get; init; }

    public bool Recording { get; set; }

    public string? BrokenPrefix { get; init; }

    public List<string> Order { get; } = [];

    public HashSet<string> Seen { get; } = [];

    public IReadOnlyList<string> Snapshot() => [.. Order];
  }
}
