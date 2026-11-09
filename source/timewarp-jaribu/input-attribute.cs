namespace TimeWarp.Jaribu;

/// <summary>
/// Provides input parameters for a parameterized test.
/// Can be applied multiple times to run the test with different inputs.
/// Similar to xUnit's [InlineData] or Fixie's [Input].
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class InputAttribute : Attribute
{
  /// <summary>
  /// Gets the parameter values for this test input.
  /// </summary>
  public object?[] Parameters { get; }

  /// <summary>
  /// Creates one input row.
  /// </summary>
  /// <param name="parameters">
  /// Argument values for one invocation. A null element is a null argument.
  /// A null array is rejected. <c>[Input(null)]</c> binds as a null array, not as one null argument.
  /// </param>
  /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is null.</exception>
  public InputAttribute(params object?[] parameters)
  {
    ArgumentNullException.ThrowIfNull(parameters);
    Parameters = parameters;
  }
}
