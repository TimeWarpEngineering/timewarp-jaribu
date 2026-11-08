#region Purpose
// One row of arguments for a parameterized test method.
#endregion

#region Design
// A null params array is the compiler's encoding of [Input(null)]: one null argument.
// An empty array stays empty. Callers that want several arguments, including nulls, pass them.
#endregion

namespace TimeWarp.Jaribu;

/// <summary>
/// Provides input parameters for a parameterized test.
/// Can be applied multiple times to run the test with different inputs.
/// Similar to xUnit's [InlineData] or Fixie's [Input].
/// </summary>
/// <param name="parameters">
/// The parameter values to pass to the test method.
/// Null is <c>[Input(null)]</c> and means one null argument.
/// </param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InputAttribute(params object?[]? parameters) : Attribute
{
  /// <summary>
  /// Gets the parameter values for this test input.
  /// A null array, which is what <c>[Input(null)]</c> supplies, is one null argument.
  /// </summary>
  public object?[] Parameters { get; } = parameters ?? [null];
}
