namespace TimeWarp.Jaribu;

#region Purpose
// Marks one argument row of a parameterized test.
#endregion

#region Design
// The constructor is params object?[]. The compiler binds a lone null as a
// null array, not as a one-element array. That row is one null argument.
// An empty [Input] stays an empty array and is a zero-argument row.
#endregion

/// <summary>
/// Provides input parameters for a parameterized test.
/// Can be applied multiple times to run the test with different inputs.
/// Similar to xUnit's [InlineData] or Fixie's [Input].
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InputAttribute : Attribute
{
  /// <summary>
  /// Provides one input row.
  /// A single null argument is <c>[Input(null)]</c>.
  /// </summary>
  /// <param name="parameters">The parameter values to pass to the test method.</param>
  public InputAttribute(params object?[]? parameters)
  {
    Parameters = parameters ?? [null];
  }

  /// <summary>
  /// Gets the parameter values for this test input.
  /// </summary>
  public object?[] Parameters { get; }
}
