namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// Lists test methods and reported cases, and applies an optional per-case predicate.
#endregion

public static partial class TestRunner
{
  /// <summary>
  /// Discovers all test methods in the specified test class.
  /// Test methods are public static methods that return Task, excluding lifecycle hooks
  /// (Setup, CleanUp, SetupOnce, CleanUpOnce) and open generic method definitions.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <returns>Enumerable of test method infos.</returns>
  public static IEnumerable<MethodInfo> DiscoverTests(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    return testClass
      .GetMethods(BindingFlags.Public | BindingFlags.Static)
      .Where(method =>
        method.IsPublic &&
        method.IsStatic &&
        method.ReturnType == typeof(Task) &&
        !method.IsGenericMethodDefinition &&
        method.Name is not "CleanUp" and not "Setup" and not "SetupOnce" and not "CleanUpOnce");
  }

  /// <summary>
  /// Lists every reported node for the class, expanding <see cref="InputAttribute"/> rows
  /// the same way a normal run does.
  /// </summary>
  /// <param name="testClass">The test class type to discover tests from.</param>
  /// <param name="filterTag">
  /// One include tag. A comma stays part of the tag.
  /// Null on the outer run reads <c>JARIBU_FILTER_TAG</c> as a comma list. Null inside a run does not.
  /// A blank value is not a filter.
  /// A class or method whose own tags exist and none match contributes one node.
  /// </param>
  /// <param name="methodNameContains">
  /// Optional substring filter on method name (ordinal ignore-case). Non-matches are omitted.
  /// </param>
  /// <param name="methodPredicate">Optional additional selection predicate. Non-matches are omitted.</param>
  /// <param name="casePredicate">Optional per-node predicate. A node is omitted when it does not match.</param>
  /// <param name="filterTags">More include tags. Each entry is one tag. See <paramref name="filterTag"/>.</param>
  /// <param name="excludeTag">One exclude tag. A comma stays part of the tag.</param>
  /// <param name="excludeTags">
  /// More exclude tags. Null with a null <paramref name="excludeTag"/> reads <c>JARIBU_EXCLUDE_TAG</c>
  /// on the outer run only.
  /// </param>
  /// <returns>Reported cases in discovery order.</returns>
  public static IEnumerable<ReportedTestCase> DiscoverTestCases(
    Type testClass,
    string? filterTag = null,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null,
    IEnumerable<string>? filterTags = null,
    string? excludeTag = null,
    IEnumerable<string>? excludeTags = null)
  {
    return TestCaseIdentity.DiscoverTestCases(
      testClass,
      filterTag,
      methodNameContains,
      methodPredicate,
      casePredicate,
      filterTags,
      excludeTag,
      excludeTags);
  }

  /// <summary>
  /// Keeps the cases the predicate accepts. A null predicate keeps every case.
  /// </summary>
  private static List<ReportedTestCase> SelectCases(
    IReadOnlyList<ReportedTestCase> cases,
    Func<ReportedTestCase, bool>? casePredicate)
  {
    if (casePredicate is null)
    {
      return [.. cases];
    }

    return [.. cases.Where(casePredicate)];
  }
}
