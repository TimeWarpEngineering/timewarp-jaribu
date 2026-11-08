#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:include test-runner.silent-no-ops.filters.cs

#region Purpose
// Near-miss signatures, empty runs, hook validation, class names, and registration snapshots.
#endregion

#region Design
// Fixtures are not ModuleInitializer-registered. The suite restores RegisteredTestClasses
// after it mutates the list. Near-miss return types warn by default and fail when
// strictSignatures is set. Ordinary void stays silent. allowEmpty opts out of the
// empty-run failure only.
// why: task 044
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;

  sealed class SilentNoOpsCollectingSink : ITestResultSink
  {
    public List<TestNodeInfo> Results { get; } = [];

    public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;
    public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestCompletedAsync(TestNodeInfo node)
    {
      Results.Add(node);
      return Task.CompletedTask;
    }

    public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;
    public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
  }

  [TestTag("Jaribu")]
  public partial class SilentNoOps_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SilentNoOps_Given_>();

    public static async Task NearMiss_Should_WarnAndStillPass()
    {
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<NearMissBesideRealFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(1);
      stats.TotalTests.ShouldBe(1);
      ShouldWarn(stats, nameof(NearMissBesideRealFixture.ReturnsTaskOfInt));
      ShouldWarn(stats, nameof(NearMissBesideRealFixture.ReturnsValueTask));
      ShouldWarn(stats, nameof(NearMissBesideRealFixture.ReturnsValueTaskOfInt));
      ShouldWarn(stats, nameof(NearMissBesideRealFixture.AsyncVoidMethod));
      ShouldWarn(stats, nameof(NearMissBesideRealFixture.OpenGeneric));
      ShouldNotWarn(stats, nameof(NearMissBesideRealFixture.OrdinaryVoid));
      ShouldNotWarn(stats, nameof(NearMissBesideRealFixture.Real));
      await Task.CompletedTask;
    }

    public static async Task NearMiss_Should_FailWhenStrict()
    {
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<NearMissBesideRealFixture>(
        sink,
        strictSignatures: true);

      stats.Success.ShouldBeFalse();
      stats.PassedCount.ShouldBe(1);
      sink.Results.Count(node => node.State == TestNodeState.Error).ShouldBe(5);
      sink.Results.ShouldContain(node => node.DisplayName == nameof(NearMissBesideRealFixture.ReturnsValueTask));
      await Task.CompletedTask;
    }

    public static async Task OnlyNearMiss_Should_FailAsEmptyUnlessAllowEmpty()
    {
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<OnlyNearMissFixture>(sink);

      stats.Success.ShouldBeFalse();
      sink.Results.Single().DisplayName.ShouldBe("NoTests");
      ShouldWarn(stats, nameof(OnlyNearMissFixture.ReturnsTaskOfInt));

      SilentNoOpsCollectingSink allowed = new();
      TestRunStats allowedStats = await TestRunner.RunTestsAsync<OnlyNearMissFixture>(
        allowed,
        allowEmpty: true);
      allowedStats.Success.ShouldBeTrue();
      allowedStats.TotalTests.ShouldBe(0);
      allowed.Results.ShouldBeEmpty();
      ShouldWarn(allowedStats, nameof(OnlyNearMissFixture.ReturnsTaskOfInt));

      SilentNoOpsCollectingSink strict = new();
      TestRunStats strictStats = await TestRunner.RunTestsAsync<OnlyNearMissFixture>(
        strict,
        strictSignatures: true,
        allowEmpty: true);
      strictStats.Success.ShouldBeFalse();
      strict.Results.Single().State.ShouldBe(TestNodeState.Error);
      strict.Results.Single().DisplayName.ShouldBe(nameof(OnlyNearMissFixture.ReturnsTaskOfInt));
    }

    public static async Task BadSetup_Should_FailClassWithoutRunningBody()
    {
      BadSetupFixture.Reset();
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BadSetupFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      BadSetupFixture.BodyRuns.ShouldBe(0);
      BadSetupFixture.CleanUpRuns.ShouldBe(0);
      sink.Results.Single().State.ShouldBe(TestNodeState.Error);
      sink.Results.Single().Exception!.Message.ShouldContain("public static Task");
      sink.Results.Single().Exception!.Message.ShouldContain(typeof(BadSetupFixture).FullName!);

      MethodInfo method = typeof(BadSetupFixture).GetMethod(nameof(BadSetupFixture.Only))!;
      TestNodeInfo single = await TestRunner.RunSingleTestAsync(typeof(BadSetupFixture), method);
      single.State.ShouldBe(TestNodeState.Error);
      BadSetupFixture.BodyRuns.ShouldBe(0);
    }

    public static async Task EmptyClassBadHook_Should_FailEvenWhenAllowEmpty()
    {
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<EmptyBadHookFixture>(sink, allowEmpty: true);

      stats.Success.ShouldBeFalse();
      sink.Results.Single().DisplayName.ShouldBe("HookSignature");
      sink.Results.Single().State.ShouldBe(TestNodeState.Error);
      sink.Results.Single().Exception!.Message.ShouldContain(typeof(EmptyBadHookFixture).FullName!);
      await Task.CompletedTask;
    }

    public static async Task BadCleanUp_Should_FailClassWithoutRunningBody()
    {
      BadCleanUpFixture.BodyRuns = 0;
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BadCleanUpFixture>(sink);

      stats.Success.ShouldBeFalse();
      BadCleanUpFixture.BodyRuns.ShouldBe(0);
      sink.Results.Single().Exception!.Message.ShouldContain(typeof(BadCleanUpFixture).FullName!);
      await Task.CompletedTask;
    }

    public static async Task InheritedHook_Should_NameTheDeclaringType()
    {
      SignatureDerivedHooks.BodyRuns = 0;
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<SignatureDerivedHooks>(sink);

      stats.Success.ShouldBeFalse();
      SignatureDerivedHooks.BodyRuns.ShouldBe(0);
      string message = sink.Results.Single().Exception!.Message;
      message.ShouldContain(typeof(SignatureBaseHooks).FullName!);
      message.ShouldNotContain(typeof(SignatureDerivedHooks).FullName!);
      await Task.CompletedTask;
    }

    public static async Task MethodFilter_Should_NotTreatAFilteredClassAsEmpty()
    {
      SilentNoOpsCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<FilterKeepsRealFixture>(
        sink,
        methodNameContains: "missing");

      stats.Success.ShouldBeTrue();
      stats.TotalTests.ShouldBe(0);
      sink.Results.ShouldBeEmpty();
      await Task.CompletedTask;
    }

    public static async Task ClassName_Should_StripOnlyATrailingTestsSuffix()
    {
      SilentNoOpsCollectingSink widget = new();
      TestRunStats widgetStats = await TestRunner.RunTestsAsync<WidgetTests>(widget);
      widgetStats.ClassName.ShouldBe("Widget");

      SilentNoOpsCollectingSink helper = new();
      TestRunStats helperStats = await TestRunner.RunTestsAsync<MyTestsHelper>(helper);
      helperStats.ClassName.ShouldBe(nameof(MyTestsHelper));

      SilentNoOpsCollectingSink doubled = new();
      TestRunStats doubledStats = await TestRunner.RunTestsAsync<TestsTests>(doubled);
      doubledStats.ClassName.ShouldBe("Tests");
      await Task.CompletedTask;
    }

    public static async Task RegisteredClasses_Should_ReturnASnapshot()
    {
      Type[] saved = [.. TestRunner.RegisteredTestClasses];
      try
      {
        IReadOnlyList<Type> snapshot = TestRunner.RegisteredTestClasses;
        int count = snapshot.Count;
        TestRunner.RegisterTests<SnapshotProbe>();

        snapshot.Count.ShouldBe(count);
        TestRunner.RegisteredTestClasses.Count.ShouldBe(count + 1);
        object.ReferenceEquals(snapshot, TestRunner.RegisteredTestClasses).ShouldBeFalse();
        Should.Throw<InvalidCastException>(() => _ = (List<Type>)TestRunner.RegisteredTestClasses);
      }
      finally
      {
        RestoreRegistrations(saved);
      }

      await Task.CompletedTask;
    }

    public static async Task RunAllTests_Should_FailWhenNothingIsRegistered()
    {
      Type[] saved = [.. TestRunner.RegisteredTestClasses];
      try
      {
        TestRunner.ClearRegisteredTests();
        int emptyExit = await TestRunner.RunAllTests();
        int allowedExit = await TestRunner.RunAllTests(allowEmpty: true);
        emptyExit.ShouldBe(1);
        allowedExit.ShouldBe(0);

        TestRunner.RegisterTests<EmptyRegistrationMarker>();
        int classExit = await TestRunner.RunAllTests();
        int classAllowed = await TestRunner.RunAllTests(allowEmpty: true);
        classExit.ShouldBe(1);
        classAllowed.ShouldBe(0);
      }
      finally
      {
        RestoreRegistrations(saved);
      }
    }

    public static async Task ClearCache_Should_BeAbsentFromTheRunner()
    {
      string[] names =
      [
        .. typeof(TestRunner)
          .GetMethods(BindingFlags.Public | BindingFlags.Static)
          .SelectMany(method => method.GetParameters())
          .Select(parameter => parameter.Name)
          .OfType<string>()
      ];
      names.ShouldNotContain("clearCache");
      await Task.CompletedTask;
    }

    private static void ShouldWarn(TestRunStats stats, string methodName)
    {
      stats.SignatureWarnings.Any(warning =>
        warning.Contains($".{methodName}'", StringComparison.Ordinal)).ShouldBeTrue();
    }

    private static void ShouldNotWarn(TestRunStats stats, string methodName)
    {
      stats.SignatureWarnings.Any(warning =>
        warning.Contains($".{methodName}'", StringComparison.Ordinal)).ShouldBeFalse();
    }

    private static void RestoreRegistrations(Type[] saved)
    {
      TestRunner.ClearRegisteredTests();
      MethodInfo register = typeof(TestRunner).GetMethod(nameof(TestRunner.RegisterTests))!;
      foreach (Type type in saved)
      {
        register.MakeGenericMethod(type).Invoke(null, null);
      }
    }
  }

  public class NearMissBesideRealFixture
  {
    public static async Task Real()
    {
      await Task.CompletedTask;
    }

    public static Task<int> ReturnsTaskOfInt() => Task.FromResult(1);

    public static ValueTask ReturnsValueTask() => ValueTask.CompletedTask;

    public static async ValueTask<int> ReturnsValueTaskOfInt()
    {
      await Task.CompletedTask;
      return 1;
    }

    public static async void AsyncVoidMethod()
    {
      await Task.CompletedTask;
    }

    public static async Task OpenGeneric<T>()
    {
      await Task.CompletedTask;
    }

    public static void OrdinaryVoid()
    {
    }
  }

  public class OnlyNearMissFixture
  {
    public static Task<int> ReturnsTaskOfInt() => Task.FromResult(1);
  }

  public class BadSetupFixture
  {
    public static int BodyRuns;
    public static int CleanUpRuns;

    public static void Reset()
    {
      BodyRuns = 0;
      CleanUpRuns = 0;
    }

    public static void Setup()
    {
    }

    public static async Task CleanUp()
    {
      CleanUpRuns++;
      await Task.CompletedTask;
    }

    public static async Task Only()
    {
      BodyRuns++;
      await Task.CompletedTask;
    }
  }

  public class EmptyBadHookFixture
  {
    public static void Setup()
    {
    }
  }

  public class BadCleanUpFixture
  {
    public static int BodyRuns;

    public static ValueTask CleanUp() => ValueTask.CompletedTask;

    public static async Task Only()
    {
      BodyRuns++;
      await Task.CompletedTask;
    }
  }

  public class SignatureBaseHooks
  {
    public static void SetupOnce()
    {
    }
  }

  public class SignatureDerivedHooks : SignatureBaseHooks
  {
    public static int BodyRuns;

    public static async Task Only()
    {
      BodyRuns++;
      await Task.CompletedTask;
    }
  }

  public class FilterKeepsRealFixture
  {
    public static async Task Only()
    {
      await Task.CompletedTask;
    }
  }

  public class WidgetTests
  {
    public static async Task Only()
    {
      await Task.CompletedTask;
    }
  }

  public class MyTestsHelper
  {
    public static async Task Only()
    {
      await Task.CompletedTask;
    }
  }

  public class TestsTests
  {
    public static async Task Only()
    {
      await Task.CompletedTask;
    }
  }

  public class SnapshotProbe
  {
    public static async Task Only()
    {
      await Task.CompletedTask;
    }
  }

  public class EmptyRegistrationMarker
  {
  }
}
