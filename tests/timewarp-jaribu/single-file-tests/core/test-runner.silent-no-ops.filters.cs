#region Purpose
// Overloaded near-misses, selection filters on empty classes, and synthetic node identity.
#endregion

#region Design
// Fixtures stay unregistered. Strict mode must report one Error per overload
// and must not throw when two public methods share a synthetic node name.
// A method predicate or case predicate that selects nothing omits strict
// near-miss Error nodes. A method-name filter still reports them.
// why: task 044 review round 1
// why: task 044 review round 2, M5
#endregion

namespace TestRunner_;

public partial class SilentNoOps_Given_
{
  public static async Task OverloadedNearMiss_Should_ReportEachError()
  {
    System.Reflection.MethodInfo taskOfInt = typeof(OverloadedNearMissFixture).GetMethod(
      nameof(OverloadedNearMissFixture.Bar),
      Type.EmptyTypes)!;
    System.Reflection.MethodInfo valueTask = typeof(OverloadedNearMissFixture).GetMethod(
      nameof(OverloadedNearMissFixture.Bar),
      [typeof(int)])!;

    SilentNoOpsCollectingSink sink = new();
    TestRunStats stats = await TestRunner.RunTestsAsync<OverloadedNearMissFixture>(
      sink,
      strictSignatures: true);

    stats.Success.ShouldBeFalse();
    stats.PassedCount.ShouldBe(1);
    TestNodeInfo[] errors = [.. sink.Results.Where(node => node.State == TestNodeState.Error)];
    errors.Length.ShouldBe(2);
    errors.Select(node => node.Uid).Distinct().Count().ShouldBe(2);
    errors.ShouldContain(node => taskOfInt.Equals(node.Method) && node.Uid.Contains("#near-miss-", StringComparison.Ordinal));
    errors.ShouldContain(node => valueTask.Equals(node.Method) && node.Uid.Contains("#near-miss-", StringComparison.Ordinal));

    SilentNoOpsCollectingSink besideInput = new();
    TestRunStats inputStats = await TestRunner.RunTestsAsync<NearMissBesideInputFixture>(
      besideInput,
      strictSignatures: true);
    inputStats.PassedCount.ShouldBe(1);
    TestNodeInfo inputError = besideInput.Results.Single(node => node.State == TestNodeState.Error);
    inputError.Uid.ShouldContain("#near-miss-");
    inputError.Method!.ReturnType.ShouldBe(typeof(Task<int>));
    besideInput.Results.Single(node => node.State == TestNodeState.Passed).Uid.ShouldContain("[0]");

    SilentNoOpsCollectingSink filtered = new();
    TestRunStats filteredStats = await TestRunner.RunTestsAsync<NearMissBesideFilteredFixture>(
      filtered,
      methodNameContains: "Keep",
      strictSignatures: true);
    filteredStats.PassedCount.ShouldBe(1);
    TestNodeInfo filteredError = filtered.Results.Single(node => node.State == TestNodeState.Error);
    filteredError.Method!.GetParameters().Length.ShouldBe(1);
    filteredError.Uid.ShouldNotBe($"{typeof(NearMissBesideFilteredFixture).FullName}.Bar");
    filteredError.Uid.ShouldContain("#near-miss-");
    await Task.CompletedTask;
  }

  public static async Task SelectionFilter_Should_SkipEmptyRunNodes()
  {
    SilentNoOpsCollectingSink byName = new();
    TestRunStats byNameStats = await TestRunner.RunTestsAsync<EmptyRegistrationMarker>(
      byName,
      methodNameContains: "missing");
    byNameStats.Success.ShouldBeTrue();
    byName.Results.ShouldBeEmpty();

    SilentNoOpsCollectingSink byCase = new();
    TestRunStats byCaseStats = await TestRunner.RunTestsAsync<EmptyRegistrationMarker>(
      byCase,
      casePredicate: _ => false);
    byCaseStats.Success.ShouldBeTrue();
    byCase.Results.ShouldBeEmpty();

    SilentNoOpsCollectingSink hook = new();
    TestRunStats hookStats = await TestRunner.RunTestsAsync<EmptyBadHookFixture>(
      hook,
      methodNameContains: "missing");
    hookStats.Success.ShouldBeTrue();
    hook.Results.ShouldBeEmpty();

    SilentNoOpsCollectingSink named = new();
    TestRunStats namedStats = await TestRunner.RunTestsAsync<AmbiguousEmptyNameFixture>(named);
    namedStats.Success.ShouldBeFalse();
    TestNodeInfo noTests = named.Results.Single();
    noTests.DisplayName.ShouldBe("NoTests");
    noTests.Method.ShouldBeNull();
    noTests.State.ShouldBe(TestNodeState.Error);

    SilentNoOpsCollectingSink hookName = new();
    TestRunStats hookNameStats = await TestRunner.RunTestsAsync<AmbiguousHookSignatureNameFixture>(hookName);
    hookNameStats.Success.ShouldBeFalse();
    TestNodeInfo hookNode = hookName.Results.Single();
    hookNode.DisplayName.ShouldBe("HookSignature");
    hookNode.Method.ShouldBeNull();
    hookNode.State.ShouldBe(TestNodeState.Error);
    await Task.CompletedTask;
  }

  public static async Task StrictPredicate_Should_SkipUnselectedNearMiss()
  {
    SilentNoOpsCollectingSink byCase = new();
    TestRunStats byCaseStats = await TestRunner.RunTestsAsync<NearMissBesideRealFixture>(
      byCase,
      casePredicate: _ => false,
      strictSignatures: true);
    byCaseStats.Success.ShouldBeTrue();
    byCaseStats.TotalTests.ShouldBe(0);
    byCase.Results.ShouldBeEmpty();
    byCaseStats.SignatureWarnings.ShouldNotBeEmpty();

    SilentNoOpsCollectingSink byMethod = new();
    TestRunStats byMethodStats = await TestRunner.RunTestsAsync<NearMissBesideRealFixture>(
      byMethod,
      methodPredicate: _ => false,
      strictSignatures: true);
    byMethodStats.Success.ShouldBeTrue();
    byMethod.Results.ShouldBeEmpty();

    SilentNoOpsCollectingSink selected = new();
    TestRunStats selectedStats = await TestRunner.RunTestsAsync<NearMissBesideRealFixture>(
      selected,
      casePredicate: testCase => testCase.Method.Name == nameof(NearMissBesideRealFixture.Real),
      strictSignatures: true);
    selectedStats.PassedCount.ShouldBe(1);
    selectedStats.Success.ShouldBeFalse();
    selected.Results.Count(node => node.State == TestNodeState.Error).ShouldBe(5);

    SilentNoOpsCollectingSink byName = new();
    TestRunStats byNameStats = await TestRunner.RunTestsAsync<NearMissBesideFilteredFixture>(
      byName,
      methodNameContains: "missing",
      strictSignatures: true);
    byNameStats.Success.ShouldBeFalse();
    byName.Results.Count(node => node.State == TestNodeState.Error).ShouldBe(1);
    byName.Results.ShouldNotContain(node => node.DisplayName == "NoTests");
    await Task.CompletedTask;
  }
}

public class OverloadedNearMissFixture
{
  public static async Task Real()
  {
    await Task.CompletedTask;
  }

  public static Task<int> Bar() => Task.FromResult(1);

  public static ValueTask Bar(int value)
  {
    _ = value;
    return ValueTask.CompletedTask;
  }
}

public class NearMissBesideInputFixture
{
  [Input(1)]
  public static async Task Foo(int value)
  {
    _ = value;
    await Task.CompletedTask;
  }

  public static Task<int> Foo() => Task.FromResult(1);
}

public class NearMissBesideFilteredFixture
{
  public static async Task Keep()
  {
    await Task.CompletedTask;
  }

  public static async Task Bar()
  {
    await Task.CompletedTask;
  }

  public static ValueTask Bar(int value)
  {
    _ = value;
    return ValueTask.CompletedTask;
  }
}

public class AmbiguousEmptyNameFixture
{
  public static void NoTests()
  {
  }

  public static void NoTests(int value)
  {
    _ = value;
  }
}

public class AmbiguousHookSignatureNameFixture
{
  public static void Setup()
  {
  }

  public static void HookSignature()
  {
  }

  public static void HookSignature(int value)
  {
    _ = value;
  }
}
