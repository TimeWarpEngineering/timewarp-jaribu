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
  /// Drops every session-fixture registration and zeroes host depth and run depth.
  /// This is a process-wide reset for meta-tests. It does not dispose.
  /// A live instance throws; end the session first so it is disposed.
  /// The clear epoch advances, so a host that began before this call can end
  /// without throwing and without disposing. <see cref="EndTestSessionAsync"/>
  /// throws when host depth is already zero, including after this reset.
  /// Do not call from ordinary test code: it abandons an outer MTP or RunAllTests session.
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
  /// Ends a host-owned test session. When host depth and any run leases are both
  /// zero, disposes created session fixtures in reverse creation order
  /// (registrations are kept).
  /// </summary>
  /// <param name="cancellationToken">
  /// Stops waiting for fixture disposal. Defaults to <see cref="CancellationToken.None"/>.
  /// </param>
  /// <exception cref="InvalidOperationException">
  /// Thrown when no host session is active.
  /// </exception>
  /// <exception cref="OperationCanceledException">
  /// Thrown when <paramref name="cancellationToken"/> fires after dispose has been started
  /// for every created fixture.
  /// </exception>
  public static Task EndTestSessionAsync(CancellationToken cancellationToken = default)
  {
    return SessionFixture.EndSessionAsync(cancellationToken);
  }
}
