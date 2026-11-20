namespace TimeWarp.Jaribu;

using System.Reflection;

#region Purpose
// One filter for class tags, method tags, and method name, shared by discovery and the class run.
#endregion

#region Design
// Include and exclude are resolved once per class run. A null argument on a
// side reads that side's environment only when the run depth is 0, the same
// moment tautology options decide. Inside a run, null stays off and does not
// inherit the outer filter. A non-null argument suppresses that side's
// environment read. filterTag and excludeTag are each one tag, including a
// value that contains a comma. filterTags and excludeTags add more tags, and
// each entry is one tag. JARIBU_FILTER_TAG and JARIBU_EXCLUDE_TAG are comma
// lists: split, trim, drop blanks, and keep the first spelling of a repeat.
// A blank explicit value adds nothing. Comparison is ordinal ignore-case
// through Normalize, and tags include inherited attributes.
// A member with no tags matches any include filter. A member with tags
// matches include when any of its tags equals any include tag. Exclude is
// applied after include. A class or method that carries any exclude tag is
// skipped. The message names the first matching exclude tag in filter order,
// and a class match is named before a method match.
// A class include miss and a class exclude both contribute one method-level
// Skipped node and do not expand [Input] rows. The include message is
// No matching tag 'ci' or No matching tag 'ci', 'smoke'. The exclude message
// is excluded by tag "slow".
// Method-name filters and a caller's case predicate omit non-matches and do
// not emit Skipped nodes. The MTP adapter does not call this type. It passes
// the raw tag lists, or null, into the public discovery and run methods.
// [Skip] stays a Skipped node. Its reason is the attribute text.
// SkippedNode is the factory for those short-circuit nodes.
// A class or method whose Jaribu attribute constructor throws is one
// method-level case. This filter does not decide the Error state.
// The runner reports that case as Error. A class tag miss does not
// turn it into Skipped.
// RunSingleTestAsync keeps its own node because that call measures duration
// and keeps the caller's parameters.
#endregion

/// <summary>
/// Selects reported cases and builds the Skipped node for a tag miss, an excluded tag, or <see cref="SkipAttribute"/>.
/// </summary>
internal static class TestFilter
{
  /// <summary>Environment variable that supplies include tags when the caller passes null.</summary>
  internal const string EnvironmentVariableName = "JARIBU_FILTER_TAG";

  /// <summary>Environment variable that supplies exclude tags when the caller passes null.</summary>
  internal const string ExcludeEnvironmentVariableName = "JARIBU_EXCLUDE_TAG";

  /// <summary>
  /// Returns null when <paramref name="value"/> is null or whitespace. Otherwise returns the trimmed value.
  /// </summary>
  public static string? Normalize(string? value)
  {
    if (string.IsNullOrWhiteSpace(value))
    {
      return null;
    }

    return value.Trim();
  }

  /// <summary>
  /// Resolves include and exclude for this run. <paramref name="depth"/> is the tautology depth
  /// before the run increments it. A depth above zero does not read the environment.
  /// </summary>
  public static TagSelection Resolve(
    string? filterTag,
    IEnumerable<string>? filterTags,
    string? excludeTag,
    IEnumerable<string>? excludeTags,
    int depth)
  {
    IReadOnlyList<string> include = ResolveSide(filterTag, filterTags, EnvironmentVariableName, depth);
    IReadOnlyList<string> exclude = ResolveSide(excludeTag, excludeTags, ExcludeEnvironmentVariableName, depth);
    return new TagSelection(include, exclude);
  }

  /// <summary>
  /// One include tag and no exclude, without reading the environment.
  /// A null or blank value is no filter. A comma stays part of the tag.
  /// </summary>
  public static TagSelection IncludeOnly(string? filterTag)
  {
    string? tag = Normalize(filterTag);
    if (tag is null)
    {
      return TagSelection.None;
    }

    return new TagSelection([tag], []);
  }

  /// <summary>
  /// Returns the class skip reason, or null when methods are checked individually.
  /// Include is applied before exclude.
  /// </summary>
  public static string? ClassSkipReason(Type testClass, TagSelection selection)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    if (!IncludeMatches(testClass, selection))
    {
      return TagMissMessage(selection.Include);
    }

    string? excluded = FirstExclude(testClass, selection);
    if (excluded is not null)
    {
      return ExcludeMessage(excluded);
    }

    return null;
  }

  /// <summary>
  /// Returns the shared tag-miss or exclude message, or null when the class and the method both pass.
  /// </summary>
  public static string? TagSkipReason(Type testClass, MethodInfo method, TagSelection selection)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    if (!IncludeMatches(testClass, selection) || !IncludeMatches(method, selection))
    {
      return TagMissMessage(selection.Include);
    }

    string? classExclude = FirstExclude(testClass, selection);
    if (classExclude is not null)
    {
      return ExcludeMessage(classExclude);
    }

    string? methodExclude = FirstExclude(method, selection);
    if (methodExclude is not null)
    {
      return ExcludeMessage(methodExclude);
    }

    return null;
  }

  /// <summary>Message stamped on a class or method tag miss.</summary>
  public static string TagMissMessage(string filterTag)
  {
    ArgumentException.ThrowIfNullOrEmpty(filterTag);
    return TagMissMessage((IReadOnlyList<string>)[filterTag]);
  }

  /// <summary>Message stamped on a class or method tag miss for one or more include tags.</summary>
  public static string TagMissMessage(IReadOnlyList<string> filterTags)
  {
    ArgumentNullException.ThrowIfNull(filterTags);
    if (filterTags.Count == 0)
    {
      throw new ArgumentException("An include miss names at least one tag.", nameof(filterTags));
    }

    return $"No matching tag '{string.Join("', '", filterTags)}'";
  }

  /// <summary>Message stamped when an exclude tag removes the test.</summary>
  public static string ExcludeMessage(string filterTag)
  {
    ArgumentException.ThrowIfNullOrEmpty(filterTag);
    return $"excluded by tag \"{filterTag}\"";
  }

  /// <summary>
  /// Returns whether the method name contains <paramref name="methodNameContains"/>.
  /// A null filter matches every method.
  /// </summary>
  public static bool MethodNameMatches(MethodInfo method, string? methodNameContains)
  {
    ArgumentNullException.ThrowIfNull(method);
    if (methodNameContains is null)
    {
      return true;
    }

    return method.Name.Contains(methodNameContains, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Lists the cases discovery and a normal run both report.
  /// <paramref name="selection"/> is already resolved: this method does not read the environment.
  /// </summary>
  public static IEnumerable<ReportedTestCase> SelectReportedCases(
    Type testClass,
    TagSelection selection,
    string? methodNameContains = null,
    Func<MethodInfo, bool>? methodPredicate = null,
    Func<ReportedTestCase, bool>? casePredicate = null)
  {
    ArgumentNullException.ThrowIfNull(testClass);

    if (JaribuAttributeRead.TryRejectClass(testClass, out _))
    {
      foreach (MethodInfo method in TestRunner.DiscoverTests(testClass))
      {
        if (!MethodNameMatches(method, methodNameContains))
        {
          continue;
        }

        if (methodPredicate is not null && !methodPredicate(method))
        {
          continue;
        }

        ReportedTestCase rejectedCase = TestCaseIdentity.MethodCase(testClass, method);
        if (!JaribuAttributeRead.PredicateKeeps(casePredicate, rejectedCase))
        {
          continue;
        }

        yield return rejectedCase;
      }

      yield break;
    }

    foreach (MethodInfo method in TestRunner.DiscoverTests(testClass))
    {
      if (!MethodNameMatches(method, methodNameContains))
      {
        continue;
      }

      if (methodPredicate is not null && !methodPredicate(method))
      {
        continue;
      }

      if (JaribuAttributeRead.TryRejectMethod(method, out _))
      {
        ReportedTestCase rejectedCase = TestCaseIdentity.MethodCase(testClass, method);
        if (!JaribuAttributeRead.PredicateKeeps(casePredicate, rejectedCase))
        {
          continue;
        }

        yield return rejectedCase;
        continue;
      }

      foreach (ReportedTestCase testCase in TestCaseIdentity.EnumerateReportedCases(testClass, method, selection))
      {
        if (casePredicate is not null && !casePredicate(testCase))
        {
          continue;
        }

        yield return testCase;
      }
    }
  }

  /// <summary>
  /// Builds the Skipped node for a tag miss, an excluded tag, or <see cref="SkipAttribute"/>.
  /// The node is the method, not an <see cref="InputAttribute"/> row.
  /// </summary>
  public static TestNodeInfo SkippedNode(ReportedTestCase testCase, string? message)
  {
    DateTimeOffset stamp = DateTimeOffset.UtcNow;
    return new TestNodeInfo(
      testCase.Uid,
      testCase.DisplayName,
      TestNodeState.Skipped,
      TimeSpan.Zero,
      Exception: null,
      message,
      Parameters: null,
      Method: testCase.Method,
      StartTime: stamp,
      EndTime: stamp);
  }

  private static List<string> ResolveSide(
    string? single,
    IEnumerable<string>? many,
    string environmentVariable,
    int depth)
  {
    if (single is null && many is null)
    {
      if (depth > 0)
      {
        return [];
      }

      return ReadEnvironmentList(environmentVariable);
    }

    List<string> tags = [];
    AddTag(tags, single);
    if (many is not null)
    {
      foreach (string tag in many)
      {
        AddTag(tags, tag);
      }
    }

    return tags;
  }

  private static List<string> ReadEnvironmentList(string variableName)
  {
    string? raw = Environment.GetEnvironmentVariable(variableName);
    if (string.IsNullOrWhiteSpace(raw))
    {
      return [];
    }

    List<string> tags = [];
    foreach (string part in raw.Split(','))
    {
      AddTag(tags, part);
    }

    return tags;
  }

  private static void AddTag(List<string> tags, string? value)
  {
    string? normalized = Normalize(value);
    if (normalized is null)
    {
      return;
    }

    foreach (string existing in tags)
    {
      if (existing.Equals(normalized, StringComparison.OrdinalIgnoreCase))
      {
        return;
      }
    }

    tags.Add(normalized);
  }

  private static bool IncludeMatches(MemberInfo member, TagSelection selection)
  {
    if (selection.Include.Count == 0)
    {
      return true;
    }

    string[] tags = MemberTags(member);
    if (tags.Length == 0)
    {
      return true;
    }

    foreach (string tag in tags)
    {
      foreach (string filter in selection.Include)
      {
        if (tag.Equals(filter, StringComparison.OrdinalIgnoreCase))
        {
          return true;
        }
      }
    }

    return false;
  }

  private static string? FirstExclude(MemberInfo member, TagSelection selection)
  {
    if (selection.Exclude.Count == 0)
    {
      return null;
    }

    string[] tags = MemberTags(member);
    foreach (string filter in selection.Exclude)
    {
      foreach (string tag in tags)
      {
        if (tag.Equals(filter, StringComparison.OrdinalIgnoreCase))
        {
          return filter;
        }
      }
    }

    return null;
  }

  private static string[] MemberTags(MemberInfo member)
  {
    return member switch
    {
      Type type => TestRunner.GetClassMetadata(type).Tags(),
      MethodInfo method => TestRunner.GetMethodMetadata(method).Tags(),
      _ => throw new ArgumentException("Tag filters apply to classes and methods.", nameof(member))
    };
  }
}

/// <summary>
/// Include and exclude tags for one class run. Empty lists are not filters.
/// </summary>
internal readonly struct TagSelection
{
  /// <summary>No include filter and no exclude filter.</summary>
  public static TagSelection None { get; } = new([], []);

  /// <summary>Creates a resolved selection. The lists are already normalized.</summary>
  public TagSelection(IReadOnlyList<string> include, IReadOnlyList<string> exclude)
  {
    ArgumentNullException.ThrowIfNull(include);
    ArgumentNullException.ThrowIfNull(exclude);
    Include = include;
    Exclude = exclude;
  }

  /// <summary>Include tags in caller order. Empty means every member passes include.</summary>
  public IReadOnlyList<string> Include { get; }

  /// <summary>Exclude tags in caller order. Empty means exclude removes nothing.</summary>
  public IReadOnlyList<string> Exclude { get; }

  /// <summary>
  /// Banner text for the include filter. One tag is unchanged. Several tags are comma-joined.
  /// Null when there is no include filter, including an exclude-only run.
  /// </summary>
  public string? IncludeLabel =>
    Include.Count switch
    {
      0 => null,
      1 => Include[0],
      _ => string.Join(", ", Include)
    };
}
