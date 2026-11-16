namespace TimeWarp.Jaribu;

#region Purpose
// One test's tautology score and the one-line reason the report prints.
#endregion

/// <summary>
/// Score and verdict for one test under coverage or prove mode.
/// </summary>
/// <param name="TestId">Fully qualified test identifier.</param>
/// <param name="Rule">Rule or claim, for example <c>coverage</c> or <c>inferred:Type.Method</c>.</param>
/// <param name="Reason">One-line explanation.</param>
/// <param name="Location"><c>file:line</c>, or the method name when no PDB line is available.</param>
/// <param name="Covered">Production methods the test entered.</param>
/// <param name="Proven">Claims whose break made the test fail and whose restore passed.</param>
/// <param name="Seconds">Wall time of the attempts, in seconds.</param>
/// <param name="KillsPerSecond">Proven claims divided by <paramref name="Seconds"/>. Zero when nothing was proven.</param>
/// <param name="Violation">True when the test is unproven, uncovered, or missing a required claim.</param>
/// <param name="Known">True when <see cref="KnownTautologyAttribute"/> excluded the test from strict failures.</param>
public sealed record TautologyFinding(
  string TestId,
  string Rule,
  string Reason,
  string Location,
  int Covered,
  int Proven,
  double Seconds,
  double KillsPerSecond,
  bool Violation,
  bool Known);
