#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Tests parameterized test execution via [Input] attribute including single input,
// multiple inputs, type mismatches, null parameters, and zero-param edge cases.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (Parameterized_Given_)
// - Method = Given condition + Should + Result
// Full test name: TestRunner_.Parameterized_Given_.Inputs_Should_BindAndRejectMismatches
//
// ParameterizedBehaviorFixture is not registered. A type mismatch or an [Input] row with
// the wrong arity fails the node and does not enter the body. The meta-test passes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;
  sealed class ParameterizedCollectingSink : ITestResultSink
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
  public class Parameterized_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Parameterized_Given_>();

    public static async Task Inputs_Should_BindAndRejectMismatches()
    {
      ParameterizedBehaviorFixture.Reset();
      ParameterizedCollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync<ParameterizedBehaviorFixture>(sink);

      stats.PassedCount.ShouldBe(5);
      stats.FailedCount.ShouldBe(2);
      stats.Success.ShouldBeFalse();
      ParameterizedBehaviorFixture.NoInputRuns.ShouldBe(1);
      ParameterizedBehaviorFixture.SingleBinding.ShouldBe("hello:42");
      ParameterizedBehaviorFixture.MultipleBinding.ShouldBe("first:1,second:2");
      ParameterizedBehaviorFixture.NullBinding.ShouldBe("True:True");
      ParameterizedBehaviorFixture.TypeMismatchRuns.ShouldBe(0);
      ParameterizedBehaviorFixture.ZeroParamRuns.ShouldBe(0);

      TestNodeInfo mismatch = sink.Results.Single(node =>
        node.DisplayName.StartsWith(nameof(ParameterizedBehaviorFixture.TypeMismatch_Should_FailInvocation), StringComparison.Ordinal));
      mismatch.State.ShouldBe(TestNodeState.Error);
      mismatch.Exception.ShouldNotBeNull();
      mismatch.Exception.ShouldBeOfType<ArgumentException>();

      TestNodeInfo zero = sink.Results.Single(node =>
        node.DisplayName == nameof(ParameterizedBehaviorFixture.ZeroParamsForMultiParam_Should_FailOrDefault));
      zero.State.ShouldBe(TestNodeState.Error);
      zero.Exception.ShouldNotBeNull();
      zero.Exception.ShouldBeOfType<TargetParameterCountException>();
    }
  }

  public class ParameterizedBehaviorFixture
  {
    public static int NoInputRuns;
    public static int TypeMismatchRuns;
    public static int ZeroParamRuns;
    public static string SingleBinding = "";
    public static string MultipleBinding = "";
    public static string NullBinding = "";

    public static void Reset()
    {
      NoInputRuns = 0;
      TypeMismatchRuns = 0;
      ZeroParamRuns = 0;
      SingleBinding = "";
      MultipleBinding = "";
      NullBinding = "";
    }

    public static async Task NoInput_Should_RunOnce()
    {
      NoInputRuns++;
      await Task.CompletedTask;
    }

    [Input("hello", 42)]
    public static async Task SingleInput_Should_BindCorrectly(string param1, int param2)
    {
      SingleBinding = $"{param1}:{param2.ToString(CultureInfo.InvariantCulture)}";
      if (param1 != "hello" || param2 != 42)
      {
        throw new InvalidOperationException("Parameter mismatch");
      }

      await Task.CompletedTask;
    }

    [Input("first", 1)]
    [Input("second", 2)]
    public static async Task MultipleInputs_Should_RunForEach(string param1, int param2)
    {
      string row = $"{param1}:{param2.ToString(CultureInfo.InvariantCulture)}";
      MultipleBinding = MultipleBinding.Length == 0 ? row : $"{MultipleBinding},{row}";
      await Task.CompletedTask;
    }

    [Input("not-an-int")]
    public static async Task TypeMismatch_Should_FailInvocation(int _)
    {
      TypeMismatchRuns++;
      await Task.CompletedTask;
    }

    [Input(null, null)]
    public static async Task NullParams_Should_HandleNullables(string? param1, int? param2)
    {
      NullBinding = $"{param1 is null}:{param2 is null}";
      if (param1 is not null || param2 is not null)
      {
        throw new InvalidOperationException("Null mismatch");
      }

      await Task.CompletedTask;
    }

    [Input]
    public static async Task ZeroParamsForMultiParam_Should_FailOrDefault(string param1, int param2)
    {
      ZeroParamRuns++;
      _ = param1;
      _ = param2;
      await Task.CompletedTask;
    }
  }
}
