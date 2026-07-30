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
// Full test name: TestRunner_.Parameterized_Given_.NoInput_Should_RunOnce
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  [TestTag("Jaribu")]
  public class Parameterized_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<Parameterized_Given_>();

    /// <summary>
    /// No [Input] - should run once with empty parameters.
    /// </summary>
    public static async Task NoInput_Should_RunOnce()
    {
      // Verify no params passed (could check via reflection or log)
      WriteLine("NoInput_Should_RunOnce: No parameters received");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Single [Input] with string and int args.
    /// Expects string param1, int param2.
    /// </summary>
    [Input("hello", 42)]
    public static async Task SingleInput_Should_BindCorrectly(string param1, int param2)
    {
      // Self-verify: log expected values
      if (param1 == "hello" && param2 == 42)
      {
        WriteLine($"SingleInput_Should_BindCorrectly: Passed with {param1}, {param2}");
      }
      else
      {
        throw new InvalidOperationException("Parameter mismatch");
      }

      await Task.CompletedTask;
    }

    /// <summary>
    /// Multiple [Input] - two invocations.
    /// </summary>
    [Input("first", 1)]
    [Input("second", 2)]
    public static async Task MultipleInputs_Should_RunForEach(string param1, int param2)
    {
      // Will run twice; log to distinguish
      WriteLine($"MultipleInputs_Should_RunForEach: {param1}, {param2}");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Type mismatch - expects int but [Input] provides string.
    /// Should fail invocation.
    /// </summary>
    [Input("not-an-int")]
    public static async Task TypeMismatch_Should_FailInvocation(int _)
    {
      // This won't reach here due to conversion failure
      WriteLine("TypeMismatch_Should_FailInvocation: Unexpected success");
      await Task.CompletedTask;
    }

    /// <summary>
    /// Null params for nullable types.
    /// </summary>
    [Input(null, null)]
    public static async Task NullParams_Should_HandleNullables(string? param1, int? param2)
    {
      if (param1 is null && param2 is null)
      {
        WriteLine("NullParams_Should_HandleNullables: Null parameters handled");
      }
      else
      {
        throw new InvalidOperationException("Null mismatch");
      }

      await Task.CompletedTask;
    }

    /// <summary>
    /// [Input] with 0 params for method expecting 2.
    /// Should fail or run with defaults/nulls.
    /// </summary>
    [Input]
    public static async Task ZeroParamsForMultiParam_Should_FailOrDefault(string param1, int param2)
    {
      // Expect failure or null/defaults
      WriteLine($"ZeroParamsForMultiParam_Should_FailOrDefault: {param1 ?? "null"}, {param2}");
      await Task.CompletedTask;
    }
  }
}
