#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Child process that writes one durations file for the writer test.
#endregion

#region Design
// multi registers Alpha and Beta and calls RunAllTests. single runs only Alpha.
// The parent sets JARIBU_DURATIONS and JARIBU_DURATIONS_PATH.
#endregion

using TimeWarp.Jaribu;

string mode = args.Length == 0 ? "multi" : args[0];
if (mode == "single")
{
  return await TestRunner.RunTests<DurationProbeAlpha>();
}

if (mode != "multi")
{
  throw new InvalidOperationException("durations probe mode must be multi or single.");
}

TestRunner.RegisterTests<DurationProbeAlpha>();
TestRunner.RegisterTests<DurationProbeBeta>();
return await TestRunner.RunAllTests();

/// <summary>Probe class with a pass, a skip, and two input rows.</summary>
[TestTag("Suite")]
public class DurationProbeAlpha
{
  /// <summary>Passing case tagged Fast.</summary>
  [TestTag("Fast")]
  public static async Task Should_Pass()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>Skipped case. The durations record has no duration.</summary>
  [Skip("not this run")]
  public static async Task Should_Skip()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }

  /// <summary>Two input rows so the file stores case index and arguments.</summary>
  [Input("one")]
  [Input("two")]
  public static async Task Should_Echo(string value)
  {
    ArgumentNullException.ThrowIfNull(value);
    await Task.CompletedTask.ConfigureAwait(false);
  }
}

/// <summary>Second class so multi-file registration is visible in the file.</summary>
public class DurationProbeBeta
{
  /// <summary>Passing case with no tags.</summary>
  public static async Task Should_AlsoPass()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
}
