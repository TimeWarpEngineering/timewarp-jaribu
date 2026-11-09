#region Purpose
// Turns a Jaribu attribute constructor rejection into a named failure the runner can report.
#endregion

#region Design
// GetCustomAttribute runs the attribute constructor. ArgumentException covers a
// null argument and a non-positive Timeout. CustomAttributeFormatException covers
// a blob the runtime cannot decode. Other exceptions are not an attribute
// rejection and still propagate. Every read constructs the attribute again.
// A rejection throws on every read. The run reports one Error node per method
// and keeps the process up.
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
    if (Fails(nameof(SkipAttribute), "the method", ReadSkip, out rejection))
    {
      return true;
    }

    if (Fails(nameof(TestTagAttribute), "the method", ReadTags, out rejection))
    {
      return true;
    }

    if (Fails(nameof(InputAttribute), "the method", ReadInputs, out rejection))
    {
      return true;
    }

    if (Fails(nameof(TimeoutAttribute), "the method", ReadTimeout, out rejection))
    {
      return true;
    }

    rejection = default;
    return false;

    void ReadSkip()
    {
      _ = method.GetCustomAttribute<SkipAttribute>();
    }

    void ReadTags()
    {
      TestTagAttribute[] tags = [.. method.GetCustomAttributes<TestTagAttribute>(inherit: true)];
      _ = tags.Length;
    }

    void ReadInputs()
    {
      InputAttribute[] inputs = [.. method.GetCustomAttributes<InputAttribute>()];
      _ = inputs.Length;
    }

    void ReadTimeout()
    {
      _ = method.GetCustomAttribute<TimeoutAttribute>(inherit: false);
    }
  }

  /// <summary>
  /// Returns true when a class Timeout or TestTag constructor throws.
  /// </summary>
  internal static bool TryRejectClass(Type testClass, out Rejection rejection)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    if (Fails(nameof(TimeoutAttribute), "the class", ReadTimeout, out rejection))
    {
      return true;
    }

    if (Fails(nameof(TestTagAttribute), "the class", ReadTags, out rejection))
    {
      return true;
    }

    rejection = default;
    return false;

    void ReadTimeout()
    {
      _ = testClass.GetCustomAttribute<TimeoutAttribute>(inherit: true);
    }

    void ReadTags()
    {
      TestTagAttribute[] tags = [.. testClass.GetCustomAttributes<TestTagAttribute>(inherit: true)];
      _ = tags.Length;
    }
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

  private static bool Fails(string attributeName, string target, Action read, out Rejection rejection)
  {
    try
    {
      read();
      rejection = default;
      return false;
    }
    catch (Exception exception) when (exception is ArgumentException or CustomAttributeFormatException)
    {
      rejection = new Rejection(attributeName, target, exception);
      return true;
    }
  }
}
