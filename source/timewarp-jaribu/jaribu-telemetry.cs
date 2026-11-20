namespace TimeWarp.Jaribu;

using System.Diagnostics;
using System.Globalization;
using System.Reflection;

#region Purpose
// ActivitySource and span contract for one test class, one test case, and one hook.
#endregion

#region Design
// The source name is TimeWarp.Jaribu. StartActivity is the only per-test work when
// nothing listens: the runtime returns null and the runner keeps going.
// Span names start with jaribu.class, jaribu.test, or jaribu.hook so a trace view
// can group them. Attributes carry the class, method, case, outcome, and hook name.
// An ambient activity is the parent. With no ambient activity, a valid TRACEPARENT
// (and TRACESTATE) is the parent, which is how a child test process joins dev test.
// An invalid header is ignored and the span is a root.
// Failed, timed out, error, and cancelled outcomes set status Error. Passed sets Ok.
// Skipped stays unset. code.filepath is set by the MTP adapter when a PDB path exists.
// Outcome and argument text are the same strings the durations file stores.
#endregion

/// <summary>
/// OpenTelemetry spans for a Jaribu run. The source name is <c>TimeWarp.Jaribu</c>.
/// </summary>
public static partial class JaribuTelemetry
{
  /// <summary>Activity source name. Listeners and exporters use this exact string.</summary>
  public const string ActivitySourceName = "TimeWarp.Jaribu";

  /// <summary>Prefix of a test-class span name.</summary>
  public const string ClassSpanPrefix = "jaribu.class";

  /// <summary>Prefix of a test-case span name.</summary>
  public const string TestSpanPrefix = "jaribu.test";

  /// <summary>Prefix of a hook span name.</summary>
  public const string HookSpanPrefix = "jaribu.hook";

  /// <summary>CLR full name of the test class.</summary>
  public const string ClassAttribute = "jaribu.test.class";

  /// <summary>Test method name.</summary>
  public const string MethodAttribute = "jaribu.test.method";

  /// <summary><c>[Input]</c> row index, present only for a parameterized case.</summary>
  public const string CaseIndexAttribute = "jaribu.test.case.index";

  /// <summary>Formatted <c>[Input]</c> arguments, present only when the case has arguments.</summary>
  public const string CaseArgumentsAttribute = "jaribu.test.case.arguments";

  /// <summary>
  /// <c>passed</c>, <c>failed</c>, <c>skipped</c>, <c>timed_out</c>, <c>error</c>, or <c>cancelled</c>.
  /// </summary>
  public const string OutcomeAttribute = "jaribu.test.outcome";

  /// <summary>Source file path when the runner already resolved one.</summary>
  public const string SourceFileAttribute = "code.filepath";

  /// <summary>Hook method name: Setup, CleanUp, SetupOnce, or CleanUpOnce.</summary>
  public const string HookNameAttribute = "jaribu.hook.name";

  /// <summary>Outcome value for <see cref="TestNodeState.Passed"/>.</summary>
  public const string OutcomePassed = "passed";

  /// <summary>Outcome value for <see cref="TestNodeState.Failed"/>.</summary>
  public const string OutcomeFailed = "failed";

  /// <summary>Outcome value for <see cref="TestNodeState.Skipped"/>.</summary>
  public const string OutcomeSkipped = "skipped";

  /// <summary>Outcome value for <see cref="TestNodeState.Timeout"/>.</summary>
  public const string OutcomeTimedOut = "timed_out";

  /// <summary>Outcome value for <see cref="TestNodeState.Error"/>.</summary>
  public const string OutcomeError = "error";

  /// <summary>Outcome value for <see cref="TestNodeState.Cancelled"/>.</summary>
  public const string OutcomeCancelled = "cancelled";

  /// <summary>Per-test setup hook.</summary>
  public const string HookSetup = "Setup";

  /// <summary>Per-test cleanup hook.</summary>
  public const string HookCleanUp = "CleanUp";

  /// <summary>Class setup hook.</summary>
  public const string HookSetupOnce = "SetupOnce";

  /// <summary>Class cleanup hook.</summary>
  public const string HookCleanUpOnce = "CleanUpOnce";

  private const int ArgumentLimit = 256;
  private const int StatusLimit = 256;

  /// <summary>The runner's activity source. No listener means <c>StartActivity</c> returns null.</summary>
  public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);

  /// <summary>
  /// Writes <see cref="SourceFileAttribute"/> on the current Jaribu span.
  /// A missing span, another source, or an empty path does nothing.
  /// </summary>
  /// <param name="filePath">Path the caller already resolved, or null.</param>
  public static void SetSourceFile(string? filePath)
  {
    if (string.IsNullOrEmpty(filePath))
    {
      return;
    }

    Activity? activity = Activity.Current;
    if (activity is null || activity.Source != ActivitySource)
    {
      return;
    }

    activity.SetTag(SourceFileAttribute, filePath);
  }

  internal static Activity? StartClass(Type testClass)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    EnsureStarted();
    string className = ClassName(testClass);
    Activity? activity = Start(string.Concat(ClassSpanPrefix, " ", className));
    activity?.SetTag(ClassAttribute, className);
    return activity;
  }

  internal static Activity? StartTest(
    Type testClass,
    MethodInfo method,
    IReadOnlyList<object?>? parameters,
    int? inputOrdinal)
  {
    ArgumentNullException.ThrowIfNull(testClass);
    ArgumentNullException.ThrowIfNull(method);
    EnsureStarted();
    string className = ClassName(testClass);
    string spanName = inputOrdinal is int index
      ? string.Concat(
        TestSpanPrefix,
        " ",
        className,
        ".",
        method.Name,
        "[",
        index.ToString(CultureInfo.InvariantCulture),
        "]")
      : string.Concat(TestSpanPrefix, " ", className, ".", method.Name);
    Activity? activity = Start(spanName);
    if (activity is null)
    {
      return null;
    }

    activity.SetTag(ClassAttribute, className);
    activity.SetTag(MethodAttribute, method.Name);
    if (inputOrdinal is int caseIndex)
    {
      activity.SetTag(CaseIndexAttribute, caseIndex);
    }

    string? arguments = FormatArguments(parameters);
    if (arguments is not null)
    {
      activity.SetTag(CaseArgumentsAttribute, arguments);
    }

    return activity;
  }

  internal static Activity? StartHook(string hookName)
  {
    ArgumentException.ThrowIfNullOrEmpty(hookName);
    EnsureStarted();
    Activity? activity = Start(string.Concat(HookSpanPrefix, " ", hookName));
    activity?.SetTag(HookNameAttribute, hookName);
    return activity;
  }

  internal static void Complete(Activity? activity, TestNodeState state, Exception? exception)
  {
    if (activity is null)
    {
      return;
    }

    string outcome = OutcomeName(state);
    activity.SetTag(OutcomeAttribute, outcome);
    if (IsError(state))
    {
      activity.SetStatus(ActivityStatusCode.Error, StatusDescription(outcome, exception));
      return;
    }

    if (state == TestNodeState.Passed)
    {
      activity.SetStatus(ActivityStatusCode.Ok);
    }
  }

  internal static void CompleteClass(Activity? activity, IReadOnlyList<TestNodeInfo> results)
  {
    if (activity is null)
    {
      return;
    }

    bool failed = false;
    bool passed = false;
    bool skipped = false;
    foreach (TestNodeInfo result in results)
    {
      switch (result.State)
      {
        case TestNodeState.Passed:
          passed = true;
          break;
        case TestNodeState.Skipped:
          skipped = true;
          break;
        case TestNodeState.Failed:
        case TestNodeState.Timeout:
        case TestNodeState.Error:
        case TestNodeState.Cancelled:
          failed = true;
          break;
      }
    }

    TestNodeState state = failed
      ? TestNodeState.Failed
      : passed
        ? TestNodeState.Passed
        : skipped
          ? TestNodeState.Skipped
          : TestNodeState.Passed;
    Complete(activity, state, exception: null);
  }

  private static Activity? Start(string name)
  {
    return ActivitySource.StartActivity(name, ActivityKind.Internal, ParentContext());
  }

  private static ActivityContext ParentContext()
  {
    Activity? current = Activity.Current;
    if (current is not null)
    {
      return current.Context;
    }

    string? traceparent = Environment.GetEnvironmentVariable("TRACEPARENT");
    if (string.IsNullOrWhiteSpace(traceparent))
    {
      return default;
    }

    string? tracestate = Environment.GetEnvironmentVariable("TRACESTATE");
    if (!ActivityContext.TryParse(traceparent, tracestate, isRemote: true, out ActivityContext parsed))
    {
      return default;
    }

    if (parsed.TraceId == default || parsed.SpanId == default)
    {
      return default;
    }

    return parsed;
  }

  private static string ClassName(Type testClass)
  {
    return testClass.FullName ?? testClass.Name;
  }

  private static string? FormatArguments(IReadOnlyList<object?>? parameters)
  {
    if (parameters is null || parameters.Count == 0)
    {
      return null;
    }

    string formatted = TestCaseIdentity.FormatDisplayName("case", parameters);
    if (formatted.Length < 6 || formatted[^1] != ')')
    {
      return Limit(formatted, ArgumentLimit);
    }

    return Limit(formatted[5..^1], ArgumentLimit);
  }

  private static string OutcomeName(TestNodeState state)
  {
    return state switch
    {
      TestNodeState.Passed => OutcomePassed,
      TestNodeState.Failed => OutcomeFailed,
      TestNodeState.Skipped => OutcomeSkipped,
      TestNodeState.Timeout => OutcomeTimedOut,
      TestNodeState.Error => OutcomeError,
      TestNodeState.Cancelled => OutcomeCancelled,
      _ => state.ToString().ToLowerInvariant()
    };
  }

  internal static string OutcomeFor(TestNodeState state)
  {
    return OutcomeName(state);
  }

  internal static string? FormatCaseArguments(IReadOnlyList<object?>? parameters)
  {
    return FormatArguments(parameters);
  }

  private static bool IsError(TestNodeState state)
  {
    return state is TestNodeState.Failed
      or TestNodeState.Timeout
      or TestNodeState.Error
      or TestNodeState.Cancelled;
  }

  private static string StatusDescription(string outcome, Exception? exception)
  {
    string? message = exception?.Message;
    string description = string.IsNullOrEmpty(message) ? outcome : message;
    return Limit(description, StatusLimit);
  }

  private static string Limit(string value, int max)
  {
    if (value.Length <= max)
    {
      return value;
    }

    return string.Concat(value.AsSpan(0, max), "...");
  }
}
