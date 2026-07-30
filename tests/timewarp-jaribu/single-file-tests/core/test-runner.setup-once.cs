#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests class-scoped SetupOnce / CleanUpOnce fixture hooks: once-only invoke,
// lazy skip/tag filter, failure fan-out, synthetic CleanUpOnce failure, signature
// validation, discovery exclusion, coexistence with per-test Setup/CleanUp,
// parameterized once-count, and RunSingleTestAsync bypass.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (SetupOnce_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.SetupOnce_Given_.OnceOnly_Should_InvokeHooksOnce
//
// Nested fixture classes are driven via RunTestsAsync / RunSingleTestAsync and
// must NOT register via ModuleInitializer (only this meta-test class registers).
// Sink name is unique to avoid clashing with CollectingSink in multi-mode.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;

  /// <summary>
  /// Collects completed test nodes for meta-test assertions.
  /// </summary>
  sealed class SetupOnceCollectingSink : ITestResultSink
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
  public class SetupOnce_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SetupOnce_Given_>();

    public static async Task OnceOnly_Should_InvokeHooksOnceAndCleanupOnPass()
    {
      OnceOnlyFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<OnceOnlyFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(2);
      OnceOnlyFixture.SetupOnceCount.ShouldBe(1);
      OnceOnlyFixture.CleanUpOnceCount.ShouldBe(1);
      OnceOnlyFixture.BodyCount.ShouldBe(2);
    }

    public static async Task CleanupAfterFailures_Should_StillRunCleanUpOnce()
    {
      CleanupAfterFailFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanupAfterFailFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      stats.PassedCount.ShouldBe(1);
      CleanupAfterFailFixture.SetupOnceCount.ShouldBe(1);
      CleanupAfterFailFixture.CleanUpOnceCount.ShouldBe(1);
      CleanupAfterFailFixture.BodyCount.ShouldBe(2);
    }

    public static async Task LazyAllSkip_Should_NotInvokeHooks()
    {
      AllSkipFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<AllSkipFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.SkippedCount.ShouldBe(2);
      stats.PassedCount.ShouldBe(0);
      AllSkipFixture.SetupOnceCount.ShouldBe(0);
      AllSkipFixture.CleanUpOnceCount.ShouldBe(0);
      AllSkipFixture.BodyCount.ShouldBe(0);
    }

    public static async Task LazyAllTagFiltered_Should_NotInvokeHooks()
    {
      AllTagFilteredFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<AllTagFilteredFixture>(sink, filterTag: "never-match");

      stats.Success.ShouldBeTrue();
      stats.SkippedCount.ShouldBe(2);
      AllTagFilteredFixture.SetupOnceCount.ShouldBe(0);
      AllTagFilteredFixture.CleanUpOnceCount.ShouldBe(0);
      AllTagFilteredFixture.BodyCount.ShouldBe(0);
    }

    public static async Task SetupOnceFailure_Should_FanOutFailedAndSkipBodies()
    {
      SetupOnceFailureFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<SetupOnceFailureFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(2);
      stats.PassedCount.ShouldBe(0);
      SetupOnceFailureFixture.SetupOnceCount.ShouldBe(1);
      SetupOnceFailureFixture.CleanUpOnceCount.ShouldBe(1);
      SetupOnceFailureFixture.BodyCount.ShouldBe(0);
      sink.Results.Count.ShouldBe(2);
      foreach (TestNodeInfo node in sink.Results)
      {
        node.State.ShouldBe(TestNodeState.Failed);
        node.Exception.ShouldNotBeNull();
        node.Exception.Message.ShouldContain("SetupOnce boom");
      }
    }

    public static async Task CleanUpOnceFailure_Should_EmitSyntheticNodeAndFailGreenRun()
    {
      CleanUpOnceFailureFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CleanUpOnceFailureFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBe(1);
      CleanUpOnceFailureFixture.SetupOnceCount.ShouldBe(1);
      CleanUpOnceFailureFixture.CleanUpOnceCount.ShouldBe(1);
      CleanUpOnceFailureFixture.BodyCount.ShouldBe(1);

      TestNodeInfo? synthetic = sink.Results.FirstOrDefault(r => r.DisplayName == "CleanUpOnce");
      synthetic.ShouldNotBeNull();
      synthetic.State.ShouldBe(TestNodeState.Failed);
      synthetic.Uid.ShouldBe($"{typeof(CleanUpOnceFailureFixture).FullName}.CleanUpOnce");
      synthetic.Exception.ShouldNotBeNull();
      synthetic.Exception.Message.ShouldContain("CleanUpOnce boom");
    }

    public static async Task BadSignatureVoid_Should_FailClassWithoutCleanUpOnce()
    {
      BadSignatureVoidFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BadSignatureVoidFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      BadSignatureVoidFixture.BodyCount.ShouldBe(0);
      BadSignatureVoidFixture.CleanUpOnceCount.ShouldBe(0);
      sink.Results[0].State.ShouldBe(TestNodeState.Failed);
      sink.Results[0].Exception.ShouldNotBeNull();
      sink.Results[0].Exception!.Message.ShouldContain("public static Task");
    }

    public static async Task BadSignaturePrivate_Should_FailClassWithoutCleanUpOnce()
    {
      BadSignaturePrivateFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BadSignaturePrivateFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      BadSignaturePrivateFixture.BodyCount.ShouldBe(0);
      BadSignaturePrivateFixture.CleanUpOnceCount.ShouldBe(0);
      sink.Results[0].State.ShouldBe(TestNodeState.Failed);
      sink.Results[0].Exception.ShouldNotBeNull();
      sink.Results[0].Exception!.Message.ShouldContain("public static Task");
    }

    public static async Task Discovery_Should_ExcludeOnceHookNames()
    {
      List<string> names = [.. TestRunner.DiscoverTests(typeof(DiscoveryExclusionFixture)).Select(m => m.Name)];

      names.ShouldContain("RealTest");
      names.ShouldNotContain("SetupOnce");
      names.ShouldNotContain("CleanUpOnce");
      names.ShouldNotContain("Setup");
      names.ShouldNotContain("CleanUp");
      names.Count.ShouldBe(1);
      await Task.CompletedTask;
    }

    public static async Task CoexistWithPerTestHooks_Should_CallBothLifetimes()
    {
      CoexistFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<CoexistFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(2);
      CoexistFixture.SetupOnceCount.ShouldBe(1);
      CoexistFixture.CleanUpOnceCount.ShouldBe(1);
      CoexistFixture.SetupCount.ShouldBe(2);
      CoexistFixture.CleanUpCount.ShouldBe(2);
      CoexistFixture.BodyCount.ShouldBe(2);
    }

    public static async Task ParameterizedInputs_Should_InvokeSetupOnceOnce()
    {
      ParameterizedOnceFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ParameterizedOnceFixture>(sink);

      stats.Success.ShouldBeTrue();
      stats.PassedCount.ShouldBe(2);
      ParameterizedOnceFixture.SetupOnceCount.ShouldBe(1);
      ParameterizedOnceFixture.CleanUpOnceCount.ShouldBe(1);
      ParameterizedOnceFixture.BodyCount.ShouldBe(2);
    }

    public static async Task RunSingleTestAsync_Should_BypassClassHooks()
    {
      BypassFixture.Reset();
      MethodInfo method = typeof(BypassFixture).GetMethod(nameof(BypassFixture.OnlyTest))!;
      TestNodeInfo result = await TestRunner.RunSingleTestAsync(typeof(BypassFixture), method);

      result.State.ShouldBe(TestNodeState.Passed);
      BypassFixture.SetupOnceCount.ShouldBe(0);
      BypassFixture.CleanUpOnceCount.ShouldBe(0);
      BypassFixture.BodyCount.ShouldBe(1);
      BypassFixture.SetupCount.ShouldBe(1);
      BypassFixture.CleanUpCount.ShouldBe(1);
    }

    public static async Task Inheritance_DerivedHook_Should_PreferMostDerived()
    {
      DerivedPreferFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<DerivedPreferFixture>(sink);

      stats.Success.ShouldBeTrue();
      DerivedPreferFixture.DerivedSetupOnceCount.ShouldBe(1);
      DerivedPreferFixture.BaseSetupOnceCount.ShouldBe(0);
      DerivedPreferFixture.DerivedCleanUpOnceCount.ShouldBe(1);
      DerivedPreferFixture.BaseCleanUpOnceCount.ShouldBe(0);
      DerivedPreferFixture.BodyCount.ShouldBe(1);
    }

    public static async Task Inheritance_BaseOnlyHook_Should_InvokeOnDerived()
    {
      BaseOnlyHookFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<BaseOnlyHookFixture>(sink);

      stats.Success.ShouldBeTrue();
      BaseOnlyHookFixture.BaseSetupOnceCount.ShouldBe(1);
      BaseOnlyHookFixture.BaseCleanUpOnceCount.ShouldBe(1);
      BaseOnlyHookFixture.BodyCount.ShouldBe(1);
    }

    public static async Task Inheritance_OverloadsOnSameType_Should_FailClass()
    {
      OverloadOnceFixture.Reset();
      SetupOnceCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<OverloadOnceFixture>(sink);

      stats.Success.ShouldBeFalse();
      stats.FailedCount.ShouldBe(1);
      OverloadOnceFixture.BodyCount.ShouldBe(0);
      sink.Results[0].Exception.ShouldNotBeNull();
      sink.Results[0].Exception!.Message.ShouldContain("multiple methods named");
    }
  }

  // --- Fixtures (not registered) ---

  public class OnceOnlyFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task TestA()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    public static async Task TestB()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class CleanupAfterFailFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task PassingTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    public static async Task FailingTest()
    {
      BodyCount++;
      await Task.CompletedTask;
      throw new InvalidOperationException("intentional body failure");
    }
  }

  public class AllSkipFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    [Skip("lazy setup check")]
    public static async Task SkippedA()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    [Skip("lazy setup check")]
    public static async Task SkippedB()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class AllTagFilteredFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    [TestTag("alpha")]
    public static async Task TaggedA()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    [TestTag("beta")]
    public static async Task TaggedB()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class SetupOnceFailureFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
      throw new InvalidOperationException("SetupOnce boom");
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task TestA()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    public static async Task TestB()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class CleanUpOnceFailureFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
      throw new InvalidOperationException("CleanUpOnce boom");
    }

    public static async Task PassingTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class BadSignatureVoidFixture
  {
    public static int BodyCount;
    public static int CleanUpOnceCount;

    public static void Reset()
    {
      BodyCount = 0;
      CleanUpOnceCount = 0;
    }

    public static void SetupOnce()
    {
      // Intentionally wrong signature (void, not Task)
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task RealTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class BadSignaturePrivateFixture
  {
    public static int BodyCount;
    public static int CleanUpOnceCount;

    public static void Reset()
    {
      BodyCount = 0;
      CleanUpOnceCount = 0;
    }

    private static async Task SetupOnce()
    {
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task RealTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class DiscoveryExclusionFixture
  {
    public static async Task SetupOnce() => await Task.CompletedTask;
    public static async Task CleanUpOnce() => await Task.CompletedTask;
    public static async Task Setup() => await Task.CompletedTask;
    public static async Task CleanUp() => await Task.CompletedTask;
    public static async Task RealTest() => await Task.CompletedTask;
  }

  public class CoexistFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int SetupCount;
    public static int CleanUpCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      SetupCount = 0;
      CleanUpCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task Setup()
    {
      SetupCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }

    public static async Task TestA()
    {
      BodyCount++;
      await Task.CompletedTask;
    }

    public static async Task TestB()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class ParameterizedOnceFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    [Input("a")]
    [Input("b")]
    public static async Task ParamTest(string value)
    {
      BodyCount++;
      value.ShouldNotBeNullOrEmpty();
      await Task.CompletedTask;
    }
  }

  public class BypassFixture
  {
    public static int SetupOnceCount;
    public static int CleanUpOnceCount;
    public static int SetupCount;
    public static int CleanUpCount;
    public static int BodyCount;

    public static void Reset()
    {
      SetupOnceCount = 0;
      CleanUpOnceCount = 0;
      SetupCount = 0;
      CleanUpCount = 0;
      BodyCount = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      CleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task Setup()
    {
      SetupCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUp()
    {
      CleanUpCount++;
      await Task.CompletedTask;
    }

    public static async Task OnlyTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class OnceHookBase
  {
    public static int BaseSetupOnceCount;
    public static int BaseCleanUpOnceCount;

    public static void ResetBase()
    {
      BaseSetupOnceCount = 0;
      BaseCleanUpOnceCount = 0;
    }

    public static async Task SetupOnce()
    {
      BaseSetupOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task CleanUpOnce()
    {
      BaseCleanUpOnceCount++;
      await Task.CompletedTask;
    }
  }

  public class DerivedPreferFixture : OnceHookBase
  {
    public static int DerivedSetupOnceCount;
    public static int DerivedCleanUpOnceCount;
    public static int BodyCount;

    public static void Reset()
    {
      ResetBase();
      DerivedSetupOnceCount = 0;
      DerivedCleanUpOnceCount = 0;
      BodyCount = 0;
    }

    public static new async Task SetupOnce()
    {
      DerivedSetupOnceCount++;
      await Task.CompletedTask;
    }

    public static new async Task CleanUpOnce()
    {
      DerivedCleanUpOnceCount++;
      await Task.CompletedTask;
    }

    public static async Task OnlyTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class BaseOnlyHookFixture : OnceHookBase
  {
    public static int BodyCount;

    public static void Reset()
    {
      ResetBase();
      BodyCount = 0;
    }

    public static async Task OnlyTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }

  public class OverloadOnceFixture
  {
    public static int BodyCount;

    public static void Reset()
    {
      BodyCount = 0;
    }

    // Two methods named SetupOnce on the same type — fail-fast resolution error.
    public static async Task SetupOnce()
    {
      await Task.CompletedTask;
    }

    public static async Task SetupOnce(int unused)
    {
      _ = unused;
      await Task.CompletedTask;
    }

    public static async Task OnlyTest()
    {
      BodyCount++;
      await Task.CompletedTask;
    }
  }
}
