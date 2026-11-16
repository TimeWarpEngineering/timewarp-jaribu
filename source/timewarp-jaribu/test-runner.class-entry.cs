namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Public class-run entry points. The core stays in test-runner.class-run.cs.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <typeparam name="T">The test class containing test methods.</typeparam>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">
  /// Optional tag filter (Skipped semantics). Null reads <c>JARIBU_FILTER_TAG</c>.
  /// A blank value is not a filter. A class or method whose tags exist and none match
  /// emits one Skipped node per method. A rejected attribute on that method is an Error node instead.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature is an Error node. The default stores a warning.
  /// A method predicate or case predicate that selects no nodes omits those Error nodes.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a class with no discoverable tests succeeds. The default fails that run.
  /// </param>
  /// <param name="tautology">
  /// Coverage and prove mode. Null on the outer run reads the environment and assembly markers.
  /// Null inside a run stays off, so a test that calls the runner does not prove its fixtures.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the class run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync<T>(
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    TautologyOptions? tautology = null,
    CancellationToken cancellationToken = default) where T : class
  {
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(
      typeof(T),
      sink,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate,
      strictSignatures,
      allowEmpty,
      tautology,
      cancellationToken);
  }

  /// <summary>
  /// Runs all tests in the specified test class using the provided sink.
  /// </summary>
  /// <param name="testClass">The test class type.</param>
  /// <param name="sink">The sink to receive test execution events.</param>
  /// <param name="filterTag">
  /// Optional tag filter (Skipped semantics). Null reads <c>JARIBU_FILTER_TAG</c>.
  /// A blank value is not a filter. A class or method whose tags exist and none match
  /// emits one Skipped node per method. A rejected attribute on that method is an Error node instead.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Selection: non-matches are omitted, not Skipped.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// </param>
  /// <param name="methodPredicate">
  /// Optional additional selection predicate (e.g. MTP uid/tree filter). Non-matches are omitted.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="casePredicate">
  /// Optional per-node predicate. A method is omitted when none of its reported cases match.
  /// When set, an empty class does not emit a NoTests or HookSignature node.
  /// When it selects no nodes, strict mode also omits near-miss Error nodes.
  /// </param>
  /// <param name="strictSignatures">
  /// When true, each near-miss test signature is an Error node. The default stores a warning.
  /// A method predicate or case predicate that selects no nodes omits those Error nodes.
  /// </param>
  /// <param name="allowEmpty">
  /// When true, a class with no discoverable tests succeeds. The default fails that run.
  /// </param>
  /// <param name="tautology">
  /// Coverage and prove mode. Null on the outer run reads the environment and assembly markers.
  /// Null inside a run stays off, so a test that calls the runner does not prove its fixtures.
  /// </param>
  /// <param name="cancellationToken">
  /// Stops the class run. Defaults to <see cref="CancellationToken.None"/> so existing callers stay source compatible.
  /// </param>
  /// <returns>TestRunStats containing aggregated results.</returns>
  public static Task<TestRunStats> RunTestsAsync(
    Type testClass,
    ITestResultSink sink,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null,
    bool strictSignatures = false,
    bool allowEmpty = false,
    TautologyOptions? tautology = null,
    CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(sink);
    return RunTestsAsyncCore(
      testClass,
      sink,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate,
      strictSignatures,
      allowEmpty,
      tautology,
      cancellationToken);
  }
}
