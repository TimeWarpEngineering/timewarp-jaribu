namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

#region Purpose
// Reports near-miss test signatures, empty runs, and the displayed class name.
#endregion

#region Design
// A discoverable test is public static non-generic Task and is not a lifecycle hook.
// Task<T>, ValueTask, ValueTask<T>, async void, and open generic Task are near-misses.
// Ordinary void stays silent. Hook names are validated by ResolveHook, not here.
// strictSignatures turns each near-miss into an Error node. Otherwise the text is
// stored on TestRunStats.SignatureWarnings and the run still succeeds when real
// tests passed. No discoverable tests fails unless allowEmpty is set. A filter
// that selects nothing is not an empty class: the caller passes classHadTests
// from discovery before selection. The displayed class name drops one trailing
// "Tests" only when characters remain before that suffix.
#endregion

public static partial class TestRunner
{
  private readonly record struct NearMiss(string MethodName, string Message);

  /// <summary>
  /// Class name shown on the sink. Strips one trailing <c>Tests</c> suffix.
  /// </summary>
  private static string ReportedClassName(Type testClass)
  {
    const string Suffix = "Tests";
    string name = testClass.Name;
    if (name.Length > Suffix.Length && name.EndsWith(Suffix, StringComparison.Ordinal))
    {
      return name[..^Suffix.Length];
    }

    return name;
  }

  /// <summary>
  /// Public static methods that look like tests and are not discovered.
  /// </summary>
  private static List<NearMiss> DescribeNearMissTests(Type testClass)
  {
    List<NearMiss> nearMisses = [];
    foreach (MethodInfo method in testClass.GetMethods(BindingFlags.Public | BindingFlags.Static))
    {
      if (!IsNearMissTest(method))
      {
        continue;
      }

      string declaringType = method.DeclaringType?.FullName ?? testClass.FullName ?? testClass.Name;
      string returned = DescribeNearMissReturn(method);
      nearMisses.Add(new NearMiss(
        method.Name,
        $"Near-miss test '{declaringType}.{method.Name}' returns '{returned}'. " +
        "Tests must be public static non-generic Task."));
    }

    return nearMisses;
  }

  /// <summary>
  /// Records strict signature failures and the empty-run failure.
  /// </summary>
  private static async Task ApplySignatureDiagnosticsAsync(
    Type testClass,
    IReadOnlyList<NearMiss> nearMisses,
    bool classHadTests,
    bool strictSignatures,
    bool allowEmpty,
    bool cancellationRequested,
    ITestResultSink sink,
    List<TestNodeInfo> results)
  {
    if (!cancellationRequested && strictSignatures)
    {
      for (int index = 0; index < nearMisses.Count; index++)
      {
        NearMiss nearMiss = nearMisses[index];
        string hookName = nearMiss.MethodName;
        if (results.Exists(node => node.DisplayName == hookName))
        {
          hookName = $"{hookName}#{index.ToString(CultureInfo.InvariantCulture)}";
        }

        await EmitSyntheticHookFailureAsync(
          testClass,
          hookName,
          new InvalidOperationException(nearMiss.Message),
          sink,
          results,
          nearMiss.Message,
          TestNodeState.Error).ConfigureAwait(false);
      }
    }

    bool alreadyFailing = results.Exists(node => TestRunStats.IsFailingState(node.State));
    if (classHadTests || allowEmpty || cancellationRequested || alreadyFailing)
    {
      return;
    }

    InvalidOperationException empty = new(
      "No tests executed. Pass allowEmpty: true to allow an empty run.");
    await EmitSyntheticHookFailureAsync(
      testClass,
      "NoTests",
      empty,
      sink,
      results,
      empty.Message,
      TestNodeState.Error).ConfigureAwait(false);
  }

  private static bool IsNearMissTest(MethodInfo method)
  {
    if (method.IsSpecialName || !method.IsPublic || !method.IsStatic || IsHookName(method.Name))
    {
      return false;
    }

    if (IsAsyncVoid(method))
    {
      return true;
    }

    if (method.ReturnType == typeof(Task))
    {
      return method.IsGenericMethodDefinition;
    }

    if (method.ReturnType == typeof(ValueTask))
    {
      return true;
    }

    if (!method.ReturnType.IsGenericType)
    {
      return false;
    }

    Type definition = method.ReturnType.GetGenericTypeDefinition();
    return definition == typeof(Task<>) || definition == typeof(ValueTask<>);
  }

  private static bool IsHookName(string name)
  {
    return name is "Setup" or "CleanUp" or "SetupOnce" or "CleanUpOnce";
  }

  private static bool IsAsyncVoid(MethodInfo method)
  {
    return method.ReturnType == typeof(void) &&
      method.GetCustomAttribute<AsyncStateMachineAttribute>() is not null;
  }

  private static string DescribeNearMissReturn(MethodInfo method)
  {
    if (IsAsyncVoid(method))
    {
      return "async void";
    }

    if (method.IsGenericMethodDefinition && method.ReturnType == typeof(Task))
    {
      return "open generic Task";
    }

    return FormatReturnType(method.ReturnType);
  }

  private static string FormatReturnType(Type returnType)
  {
    if (!returnType.IsGenericType)
    {
      return returnType.Name;
    }

    Type definition = returnType.GetGenericTypeDefinition();
    string name = definition == typeof(Task<>) ? "Task"
      : definition == typeof(ValueTask<>) ? "ValueTask"
      : definition.Name;
    string arguments = string.Join(
      ", ",
      returnType.GetGenericArguments().Select(FormatReturnType));
    return $"{name}<{arguments}>";
  }
}
