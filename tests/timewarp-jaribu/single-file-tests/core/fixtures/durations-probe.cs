#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Child process that writes one durations file for the writer test.
#endregion

#region Design
// multi registers Alpha and Beta and calls RunAllTests. single runs only Alpha.
// nested runs outer classes whose bodies call the runner again, one after another
// and several at once. The file should contain only the outer rows.
// The parent sets JARIBU_DURATIONS and JARIBU_DURATIONS_PATH.
#endregion

using TimeWarp.Jaribu;

string mode = args.Length == 0 ? "multi" : args[0];
if (mode == "single")
{
  return await TestRunner.RunTests<DurationProbeAlpha>();
}

if (mode == "nested")
{
  TestRunner.RegisterTests<DurationProbeOuter>();
  TestRunner.RegisterTests<DurationProbeLater>();
  return await TestRunner.RunAllTests();
}

if (mode != "multi")
{
  throw new InvalidOperationException("durations probe mode must be multi, single, or nested.");
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

/// <summary>Outer class that calls the runner again from a test body.</summary>
public class DurationProbeOuter
{
  /// <summary>Runs one nested class, then eight nested classes at the same time.</summary>
  public static async Task Should_RunNested()
  {
    TestRunStats sequential = await TestRunner
      .RunTestsAsync<DurationProbeNested>(NullSink.Instance)
      .ConfigureAwait(false);
    if (!sequential.Success)
    {
      throw new InvalidOperationException("The sequential nested durations run failed.");
    }

    Task<TestRunStats>[] runs = new Task<TestRunStats>[8];
    for (int index = 0; index < runs.Length; index++)
    {
      runs[index] = Task.Run
      (
        () => TestRunner.RunTestsAsync<DurationProbeNestedConcurrent>(NullSink.Instance)
      );
    }

    TestRunStats[] stats = await Task.WhenAll(runs).ConfigureAwait(false);
    foreach (TestRunStats stat in stats)
    {
      if (!stat.Success)
      {
        throw new InvalidOperationException("A concurrent nested durations run failed.");
      }
    }
  }

  /// <summary>Later case in the same class. It stays in the outer file.</summary>
  public static async Task Should_StillRecord()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
}

/// <summary>Later class. Its case stays in the outer file.</summary>
public class DurationProbeLater
{
  /// <summary>Passing case that must remain in the outer file.</summary>
  public static async Task Should_RecordTheLaterClass()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
}

/// <summary>Nested class that must stay out of the outer durations file.</summary>
public class DurationProbeNested
{
  /// <summary>Passing case whose row must not be recorded by the outer run.</summary>
  public static async Task Should_NotAppear()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
}

/// <summary>Nested class started on other threads. Its rows must stay out of the outer file.</summary>
public class DurationProbeNestedConcurrent
{
  /// <summary>Passing case whose row must not be recorded by the outer run.</summary>
  public static async Task Should_NotAppearConcurrent()
  {
    await Task.CompletedTask.ConfigureAwait(false);
  }
}
