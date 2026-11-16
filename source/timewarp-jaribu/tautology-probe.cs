namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Records production methods a test enters and throws when prove mode breaks one.
#endregion

#region Design
// The instrumenter inserts Enter at the start of each production method and Leave
// in a finally, so depth tracks the call the test made directly.
// Enter returns immediately when no scope is active, so an instrumented build
// stays quiet during a normal run. The runner pushes a scope only for coverage
// or prove mode, and it records only while the test body is running.
// A broken prefix matches every overload of the claimed member.
// The probe types themselves are not instrumented, so Enter cannot recurse.
// Seen and the entry list are guarded because a test may enter from several tasks.
#endregion

/// <summary>
/// In-process hook the instrumented build calls on method entry.
/// </summary>
public static class TautologyProbe
{
  private static readonly AsyncLocal<Scope?> Ambient = new();
  private static readonly AsyncLocal<int> FlowDepth = new();
  private static readonly AsyncLocal<int> IgnoredDepth = new();
  private static bool InstrumentedSeen;

  /// <summary>One recorded entry and how deep it sat under the test.</summary>
  internal readonly record struct Hit(string Token, int Depth);

  /// <summary>
  /// Records <paramref name="token"/> and throws <see cref="ProbeBrokenException"/> when that member is broken.
  /// A null scope returns without touching the token. Depth still advances while a scope is active
  /// so <see cref="Leave"/> stays paired when recording is paused.
  /// </summary>
  /// <param name="token">
  /// <c>Assembly|Namespace.Type.Method(parameter types)</c> written by the instrumenter.
  /// </param>
  public static void Enter(string token)
  {
    Scope? scope = Ambient.Value;
    if (scope is null)
    {
      return;
    }

    int depth = FlowDepth.Value + 1;
    FlowDepth.Value = depth;
    if (token is null)
    {
      return;
    }

    // The runner awaits the test through one helper. That frame sits at the
    // same depth as the test's own call, so it must not become the claim.
    if (depth == IgnoredDepth.Value)
    {
      return;
    }

    lock (scope.Gate)
    {
      if (!scope.Recording)
      {
        return;
      }

      scope.Entries.Add(new Hit(token, depth));
      scope.Seen.Add(token);
      if (IsBroken(scope, token))
      {
        throw new ProbeBrokenException(token);
      }
    }
  }

  /// <summary>
  /// Drops one frame of depth after an instrumented method returns or throws.
  /// A null scope does nothing, matching an <see cref="Enter"/> that returned immediately.
  /// </summary>
  public static void Leave()
  {
    if (Ambient.Value is null)
    {
      return;
    }

    int depth = FlowDepth.Value;
    if (depth > 0)
    {
      FlowDepth.Value = depth - 1;
    }
  }

  /// <summary>True when this async flow has a coverage or prove scope.</summary>
  internal static bool IsActive => Ambient.Value is not null;

  /// <summary>True when the active scope is recording the test body.</summary>
  internal static bool IsRecording
  {
    get
    {
      if (Ambient.Value is not Scope scope)
      {
        return false;
      }

      lock (scope.Gate)
      {
        return scope.Recording;
      }
    }
  }

  /// <summary>
  /// Ignores the next instrumented frame on this flow. Returns the previous depth so the caller can restore it.
  /// Zero means no frame is ignored.
  /// </summary>
  internal static int PushIgnoredFrame()
  {
    int previous = IgnoredDepth.Value;
    IgnoredDepth.Value = FlowDepth.Value + 1;
    return previous;
  }

  /// <summary>Restores the ignored frame from <see cref="PushIgnoredFrame"/>.</summary>
  internal static void PopIgnoredFrame(int previous) => IgnoredDepth.Value = previous;

  /// <summary>Turns body recording on or off. Setup and cleanup stay off.</summary>
  internal static void SetRecording(bool recording)
  {
    if (Ambient.Value is Scope scope)
    {
      lock (scope.Gate)
      {
        scope.Recording = recording;
      }
    }
  }

  /// <summary>Pushes a scope for one test attempt. The caller pops it.</summary>
  internal static Scope Push(IReadOnlyList<string>? brokenPrefixes)
  {
    Scope scope = new()
    {
      Previous = Ambient.Value,
      BrokenPrefixes = brokenPrefixes
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

  /// <summary>One test attempt's hits and break prefixes.</summary>
  internal sealed class Scope
  {
    public Scope? Previous { get; init; }

    public bool Recording { get; set; }

    public IReadOnlyList<string>? BrokenPrefixes { get; init; }

    public List<Hit> Entries { get; } = [];

    public HashSet<string> Seen { get; } = [];

    internal object Gate { get; } = new();

    public IReadOnlyList<Hit> Snapshot()
    {
      lock (Gate)
      {
        return [.. Entries];
      }
    }
  }

  private static bool IsBroken(Scope scope, string token)
  {
    if (scope.BrokenPrefixes is null)
    {
      return false;
    }

    foreach (string prefix in scope.BrokenPrefixes)
    {
      if (token.StartsWith(prefix, StringComparison.Ordinal))
      {
        return true;
      }
    }

    return false;
  }
}
