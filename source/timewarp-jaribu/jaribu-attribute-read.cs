#region Purpose
// Turns a Jaribu attribute constructor rejection into a named failure the runner can report.
#endregion

#region Design
// The runner caches the first read of each member. ArgumentException covers a
// null argument and a non-positive Timeout. CustomAttributeFormatException covers
// a blob the runtime cannot decode. Other exceptions are not an attribute
// rejection and still propagate, and that read is not cached. A cached rejection
// is the same exception the constructor threw. The run reports one Error node
// per method and keeps the process up.
#endregion

namespace TimeWarp.Jaribu;

using System.Reflection;

/// <summary>
/// Reads Jaribu attributes and names a constructor rejection.
/// </summary>
internal static class JaribuAttributeRead
{
  /// <summary>One attribute whose constructor rejected the member.</summary>
  internal readonly record struct Rejection(string AttributeName, string Target, Exception Exception)
  {
    /// <summary>Node message. It names the attribute.</summary>
    public string Message =>
      $"{AttributeName} rejected {Target}: {Exception.GetType().Name}: {Exception.Message}";
  }

  /// <summary>
  /// Returns true when a method Skip, TestTag, Input, or Timeout constructor throws.
  /// </summary>
  internal static bool TryRejectMethod(MethodInfo method, out Rejection rejection)
  {
    ArgumentNullException.ThrowIfNull(method);
    return TestRunner.GetMethodMetadata(method).TryReject(out rejection);
  }

  /// <summary>
  /// Returns true when a class Timeout or TestTag constructor throws.
  /// </summary>
  internal static bool TryRejectClass(Type testClass, out Rejection rejection)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    return TestRunner.GetClassMetadata(testClass).TryReject(out rejection);
  }

  /// <summary>
  /// A null predicate keeps the case. A predicate that itself fails to read an attribute keeps it too.
  /// </summary>
  internal static bool PredicateKeeps(
    Func<ReportedTestCase, bool>? casePredicate,
    ReportedTestCase testCase)
  {
    if (casePredicate is null)
    {
      return true;
    }

    try
    {
      return casePredicate(testCase);
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      return true;
    }
  }

}
