namespace TimeWarp.Jaribu.TestingPlatform;

using System.Globalization;
using System.Reflection;

#region Purpose
// Turns one reported case into the path and property bag TreeNodeFilter expects.
#endregion

#region Design
// The path is /Assembly/Namespace/Type/Method. Assembly is the simple name.
// Type keeps nested names with '+' and drops the namespace, matching the
// method-identifier property. A slash inside a segment is percent-encoded so
// it cannot open another level. '+' stays literal because filters write it that way.
// The bag carries TestTag for each class and method tag, and jaribu.parameter.N
// for each [Input] value, the same keys MtpSink publishes. Property filters
// read only TestMetadataProperty. NopFilter and the uid list stay on this
// switch so an unrecognized filter fails the request instead of running every case.
#endregion

/// <summary>
/// Matches one reported case against the MTP execution filter.
/// </summary>
internal static class MtpTreeFilter
{
  /// <summary>Metadata key for a <see cref="TestTagAttribute"/> value.</summary>
  public const string TestTagKey = "TestTag";

  /// <summary>Metadata key prefix for one <see cref="InputAttribute"/> argument.</summary>
  public const string ParameterKeyPrefix = "jaribu.parameter.";

  /// <summary>
  /// Returns whether <paramref name="filter"/> selects <paramref name="testCase"/>.
  /// </summary>
  public static bool Matches(Type testClass, ReportedTestCase testCase, ITestExecutionFilter filter)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(filter);

#pragma warning disable TPEXP // TreeNodeFilter is experimental
    return filter switch
    {
      NopFilter => true,
      TestNodeUidListFilter uidFilter =>
        uidFilter.TestNodeUids.Any(uid => uid.Value == testCase.Uid),
      TreeNodeFilter treeFilter =>
        treeFilter.MatchesFilter(BuildPath(testClass, testCase.Method), BuildProperties(testClass, testCase)),
      _ => throw new NotSupportedException(
        $"Unsupported MTP filter type '{filter.GetType().FullName}'.")
    };
#pragma warning restore TPEXP
  }

  /// <summary>
  /// Builds <c>/Assembly/Namespace/Type/Method</c> for <paramref name="method"/>.
  /// </summary>
  public static string BuildPath(Type testClass, MethodInfo method)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);

    string assemblyName = testClass.Assembly.GetName().Name ?? string.Empty;
    string namespaceName = testClass.Namespace ?? string.Empty;
    return string.Concat(
      "/",
      EncodeSegment(assemblyName),
      "/",
      EncodeSegment(namespaceName),
      "/",
      EncodeSegment(MetadataTypeName(testClass)),
      "/",
      EncodeSegment(method.Name));
  }

  /// <summary>
  /// Builds the property bag property filters evaluate for <paramref name="testCase"/>.
  /// </summary>
  public static PropertyBag BuildProperties(Type testClass, ReportedTestCase testCase)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    PropertyBag bag = new();
    AddTags(bag, testClass);
    AddTags(bag, testCase.Method);

    IReadOnlyList<object?>? parameters = testCase.Parameters;
    if (parameters is not { Count: > 0 })
    {
      return bag;
    }

    for (int index = 0; index < parameters.Count; index++)
    {
      string key = string.Concat(
        ParameterKeyPrefix,
        index.ToString(CultureInfo.InvariantCulture));
      bag.Add(new TestMetadataProperty(key, FormatParameter(parameters[index])));
    }

    return bag;
  }

  private static void AddTags(PropertyBag bag, MemberInfo member)
  {
    foreach (TestTagAttribute tag in member.GetCustomAttributes<TestTagAttribute>())
    {
      if (string.IsNullOrEmpty(tag.Tag))
      {
        continue;
      }

      bag.Add(new TestMetadataProperty(TestTagKey, tag.Tag));
    }
  }

  /// <summary>
  /// Metadata type name without the namespace. Nested types keep <c>+</c>. Generic arity keeps the backtick.
  /// </summary>
  private static string MetadataTypeName(Type type)
  {
    string fullName = type.FullName ?? type.Name;
    string namespaceName = type.Namespace ?? string.Empty;
    if (namespaceName.Length == 0)
    {
      return fullName;
    }

    string prefix = string.Concat(namespaceName, ".");
    if (fullName.StartsWith(prefix, StringComparison.Ordinal))
    {
      return fullName[prefix.Length..];
    }

    return fullName;
  }

  private static string EncodeSegment(string segment)
  {
    if (segment.IndexOf('/', StringComparison.Ordinal) < 0)
    {
      return segment;
    }

    return segment.Replace("/", "%2F", StringComparison.Ordinal);
  }

  private static string FormatParameter(object? value)
  {
    return value switch
    {
      null => "null",
      string text => text,
      bool flag => flag ? "true" : "false",
      IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
      _ => value.ToString() ?? string.Empty
    };
  }
}
