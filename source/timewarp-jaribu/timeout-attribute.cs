namespace TimeWarp.Jaribu;

using System;

#region Purpose
// Declares a positive millisecond budget for a test method or for a class's hooks.
#endregion

#region Design
// Zero and negative values are rejected here so Task.Delay and the timer
// constructor never see them. A method attribute limits that test body.
// A class attribute limits Setup, CleanUp, SetupOnce, CleanUpOnce, and
// session fixture creation while that class runs. The class value does not
// limit test bodies; those use their own method attribute.
// When the class has no attribute, TestRunner.DefaultHookTimeout is the hook budget.
#endregion

/// <summary>
/// Limits how long a test method, or a class's hooks and session-fixture creation, may run.
/// </summary>
/// <remarks>
/// On a method, the value limits that test body.
/// On a class, the value limits Setup, CleanUp, SetupOnce, CleanUpOnce, and session fixture
/// creation while that class runs. A method attribute does not change the class hook limit.
/// When a class has no attribute, <see cref="TestRunner.DefaultHookTimeout"/> applies to those hooks.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class TimeoutAttribute : Attribute
{
  /// <summary>Timeout in milliseconds. Always greater than zero.</summary>
  public int Milliseconds { get; }

  /// <summary>
  /// Creates a timeout.
  /// </summary>
  /// <param name="milliseconds">Timeout in milliseconds. Must be greater than zero.</param>
  /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
  public TimeoutAttribute(int milliseconds)
  {
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(milliseconds);
    Milliseconds = milliseconds;
  }
}
