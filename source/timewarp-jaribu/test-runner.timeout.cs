namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Hook and fixture timeout policy, and the clock that schedules every timeout.
#endregion

#region Design
// Method TimeoutAttribute limits that test body only. Class TimeoutAttribute
// limits Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation
// for the class run. DefaultHookTimeout fills in when the class has no attribute.
// Neither default is implicit: a missing class attribute and a null default
// leave hooks unbounded, because session fixtures include hosts that outlast
// a short unit-test budget.
//
// The active class budget is ambient so SessionFixture.GetAsync can read it
// without a new argument. TimeProvider is process-wide so a test can replace
// the clock. Failure marks travel on the exception instance so fixture
// creation stays Error, and a fixture create timeout stays Timeout, without
// a public exception type.
#endregion

public static partial class TestRunner
{
  private static TimeProvider Clock = TimeProvider.System;
  private static TimeSpan? HookTimeoutDefault;
  private static readonly AsyncLocal<int?> AmbientHookTimeoutMilliseconds = new();

  /// <summary>
  /// Clock that schedules test, hook, and session-fixture timeouts.
  /// </summary>
  /// <exception cref="ArgumentNullException">The value is null.</exception>
  public static TimeProvider TimeProvider
  {
    get => Clock;
    set
    {
      ArgumentNullException.ThrowIfNull(value);
      Clock = value;
    }
  }

  /// <summary>
  /// Timeout for Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture creation
  /// when the running class has no <see cref="TimeoutAttribute"/>.
  /// Null applies no hook timeout.
  /// </summary>
  /// <exception cref="ArgumentOutOfRangeException">
  /// The value is zero, negative, or longer than <see cref="int.MaxValue"/> milliseconds.
  /// </exception>
  public static TimeSpan? DefaultHookTimeout
  {
    get => HookTimeoutDefault;
    set
    {
      if (value is TimeSpan timeout &&
          (timeout <= TimeSpan.Zero || timeout.TotalMilliseconds > int.MaxValue))
      {
        throw new ArgumentOutOfRangeException(
          nameof(value),
          value,
          "Hook timeout must be greater than zero and fit in a millisecond count.");
      }

      HookTimeoutDefault = value;
    }
  }

  /// <summary>
  /// Hook budget for the class run on this async flow. Session fixture creation reads it.
  /// </summary>
  internal static int? CurrentHookTimeoutMilliseconds => AmbientHookTimeoutMilliseconds.Value;

  /// <summary>
  /// Restores the previous hook budget when disposed.
  /// </summary>
  internal struct HookTimeoutScope : IDisposable
  {
    private readonly int? Previous;
    private bool Disposed;

    public HookTimeoutScope(Type testClass)
    {
      ArgumentNullException.ThrowIfNull(testClass);
      Previous = AmbientHookTimeoutMilliseconds.Value;
      AmbientHookTimeoutMilliseconds.Value = ResolveHookTimeoutMilliseconds(testClass);
    }

    public void Dispose()
    {
      if (Disposed)
      {
        return;
      }

      Disposed = true;
      AmbientHookTimeoutMilliseconds.Value = Previous;
    }
  }

  internal static int? ResolveHookTimeoutMilliseconds(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    TimeoutAttribute? attribute = testClass.GetCustomAttribute<TimeoutAttribute>(inherit: true);
    if (attribute is not null)
    {
      return attribute.Milliseconds;
    }

    if (HookTimeoutDefault is not TimeSpan timeout)
    {
      return null;
    }

    return checked((int)timeout.TotalMilliseconds);
  }

  internal static string TimeoutAfter(int milliseconds, string site)
    => $"Timeout after {milliseconds}ms in {site}";
}

/// <summary>
/// Marks an exception that must stay Error or Timeout when it surfaces from a test body.
/// </summary>
internal static class FailureMark
{
  private const string KindKey = "TimeWarp.Jaribu.FailureKind";

  internal const string FixtureCreation = "fixture-creation";
  internal const string FixtureCreationTimeout = "fixture-creation-timeout";
  internal const string HookTimeout = "hook-timeout";

  internal static Exception Mark(Exception exception, string kind)
  {
    ArgumentNullException.ThrowIfNull(exception);
    exception.Data[KindKey] = kind;
    return exception;
  }

  internal static bool Is(Exception exception, string kind)
  {
    ArgumentNullException.ThrowIfNull(exception);
    return exception.Data.Contains(KindKey) && Equals(exception.Data[KindKey], kind);
  }

  /// <summary>
  /// Fixture creation is Error. A fixture or hook timeout is Timeout.
  /// Anything else keeps <paramref name="fallback"/>.
  /// </summary>
  internal static TestNodeState StateFor(Exception exception, TestNodeState fallback)
  {
    if (Is(exception, HookTimeout) || Is(exception, FixtureCreationTimeout))
    {
      return TestNodeState.Timeout;
    }

    if (Is(exception, FixtureCreation))
    {
      return TestNodeState.Error;
    }

    return fallback;
  }
}
