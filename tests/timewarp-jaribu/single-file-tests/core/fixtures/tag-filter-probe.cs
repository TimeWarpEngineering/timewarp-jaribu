#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Child process whose entry run reads JARIBU_FILTER_TAG and JARIBU_EXCLUDE_TAG.
#endregion

#region Design
// The parent test sets the variables and reads this process's report.
// There is no class tag, so a method include filter does not skip the class.
// A method tagged both smoke and slow is the include-then-exclude case.
#endregion

using TimeWarp.Jaribu;

return await TestRunner.RunTests<TagFilterProbe>();

/// <summary>Probe class executed only by the tag-filter environment test.</summary>
public class TagFilterProbe
{
  /// <summary>No tags. Include filters keep it.</summary>
  public static async Task Untagged()
  {
    await Task.CompletedTask;
  }

  /// <summary>Include tag whose environment value may carry surrounding spaces.</summary>
  [TestTag("Keep")]
  public static async Task Keep()
  {
    await Task.CompletedTask;
  }

  /// <summary>Exclude tag.</summary>
  [TestTag("slow")]
  public static async Task Slow()
  {
    await Task.CompletedTask;
  }

  /// <summary>Second include tag.</summary>
  [TestTag("smoke")]
  public static async Task Smoke()
  {
    await Task.CompletedTask;
  }

  /// <summary>Passes an include of smoke and then hits an exclude of slow.</summary>
  [TestTag("smoke")]
  [TestTag("slow")]
  public static async Task SmokeSlow()
  {
    await Task.CompletedTask;
  }
}
