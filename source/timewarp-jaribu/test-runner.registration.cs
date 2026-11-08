namespace TimeWarp.Jaribu;

#region Purpose
// Registers test classes and session fixtures, and begins or ends a host-owned session.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Registers a test class for batch execution with RunAllTests().
  /// </summary>
  /// <typeparam name="T">The test class to register.</typeparam>
  public static void RegisterTests<T>()
  {
    Type testType = typeof(T);
    lock (RegistrationGate)
    {
      if (!InternalRegisteredTestClasses.Contains(testType))
      {
        InternalRegisteredTestClasses.Add(testType);
      }
    }
  }

  /// <summary>
  /// Clears all registered test classes.
  /// </summary>
  public static void ClearRegisteredTests()
  {
    lock (RegistrationGate)
    {
      InternalRegisteredTestClasses.Clear();
    }
  }

  /// <summary>
  /// Registers a session-scoped fixture type. Call from a <c>[ModuleInitializer]</c>
  /// alongside <see cref="RegisterTests{T}"/>. Fail-fast on double-registration or when
  /// <typeparamref name="T"/> lacks <c>public static Task&lt;T&gt; CreateAsync()</c>.
  /// </summary>
  /// <typeparam name="T">
  /// Fixture type implementing <see cref="IAsyncDisposable"/> with
  /// <c>public static Task&lt;T&gt; CreateAsync()</c>.
  /// </typeparam>
  public static void RegisterSessionFixture<T>() where T : class, IAsyncDisposable
  {
    SessionFixture.Register(typeof(T));
  }

  /// <summary>
  /// Clears all session fixture registrations and resets session nesting to zero —
  /// a reset-the-world escape hatch for meta-tests only. Throws when a created
  /// fixture instance is still live; end the session first so it is disposed.
  /// Do not call from ordinary test code: resetting nesting silently unbalances
  /// any outer session (MTP or RunAllTests).
  /// </summary>
  /// <exception cref="InvalidOperationException">A created fixture instance is still live.</exception>
  public static void ClearRegisteredSessionFixtures()
  {
    SessionFixture.Clear();
  }

  /// <summary>
  /// Begins a host-owned test session (nesting-aware). Prefer for MTP adapters and
  /// multi-class drivers that share session fixtures; pair with
  /// <see cref="EndTestSessionAsync"/>.
  /// </summary>
  public static void BeginTestSession()
  {
    SessionFixture.BeginSession();
  }

  /// <summary>
  /// Ends a host-owned test session. When the outermost session ends, disposes all
  /// created session fixtures (registrations are kept).
  /// </summary>
  /// <param name="cancellationToken">
  /// Stops waiting for fixture disposal. Defaults to <see cref="CancellationToken.None"/>.
  /// </param>
  /// <exception cref="OperationCanceledException">
  /// Thrown when <paramref name="cancellationToken"/> fires after dispose has been started
  /// for every created fixture.
  /// </exception>
  public static Task EndTestSessionAsync(CancellationToken cancellationToken = default)
  {
    return SessionFixture.EndSessionAsync(cancellationToken);
  }
}
