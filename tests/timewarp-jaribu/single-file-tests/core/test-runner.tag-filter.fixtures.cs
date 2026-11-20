#region Purpose
// Unregistered fixtures for the tag-filter runfile.
#endregion

#region Design
// The meta-test file includes this sibling. The MTP project lists it because a
// normal project ignores #:include.
#endregion

namespace TestRunner_;

  /// <summary>Methods for an any-of include filter. No class tag.</summary>
  public class TagFilterIncludeFixture
  {
    public static int CiRuns;
    public static int SmokeRuns;
    public static int UnitRuns;
    public static int PlainRuns;

    public static void Reset()
    {
      CiRuns = 0;
      SmokeRuns = 0;
      UnitRuns = 0;
      PlainRuns = 0;
    }

    [TestTag("ci")]
    public static async Task Ci()
    {
      CiRuns++;
      await Task.CompletedTask;
    }

    [TestTag("smoke")]
    public static async Task Smoke()
    {
      SmokeRuns++;
      await Task.CompletedTask;
    }

    [TestTag("unit")]
    public static async Task Unit()
    {
      UnitRuns++;
      await Task.CompletedTask;
    }

    public static async Task Plain()
    {
      PlainRuns++;
      await Task.CompletedTask;
    }
  }

  /// <summary>Methods for an any-of exclude filter.</summary>
  public class TagFilterExcludeFixture
  {
    public static int SlowRuns;
    public static int FlakyRuns;
    public static int PlainRuns;

    public static void Reset()
    {
      SlowRuns = 0;
      FlakyRuns = 0;
      PlainRuns = 0;
    }

    [TestTag("slow")]
    public static async Task Slow()
    {
      SlowRuns++;
      await Task.CompletedTask;
    }

    [TestTag("flaky")]
    public static async Task Flaky()
    {
      FlakyRuns++;
      await Task.CompletedTask;
    }

    public static async Task Plain()
    {
      PlainRuns++;
      await Task.CompletedTask;
    }
  }

  /// <summary>One method carries both the include tag and the exclude tag.</summary>
  public class TagFilterBothFixture
  {
    public static int SmokeRuns;
    public static int SmokeSlowRuns;
    public static int SlowRuns;

    public static void Reset()
    {
      SmokeRuns = 0;
      SmokeSlowRuns = 0;
      SlowRuns = 0;
    }

    [TestTag("smoke")]
    public static async Task Smoke()
    {
      SmokeRuns++;
      await Task.CompletedTask;
    }

    [TestTag("smoke")]
    [TestTag("slow")]
    public static async Task SmokeSlow()
    {
      SmokeSlowRuns++;
      await Task.CompletedTask;
    }

    [TestTag("slow")]
    public static async Task Slow()
    {
      SlowRuns++;
      await Task.CompletedTask;
    }
  }

  /// <summary>Class exclude applies to every method, including an input row.</summary>
  [TestTag("slow")]
  public class TagFilterClassExcludeFixture
  {
    public static int BodyRuns;
    public static int SetupOnceRuns;

    public static void Reset()
    {
      BodyRuns = 0;
      SetupOnceRuns = 0;
    }

    public static async Task SetupOnce()
    {
      SetupOnceRuns++;
      await Task.CompletedTask;
    }

    public static async Task Alpha()
    {
      BodyRuns++;
      await Task.CompletedTask;
    }

    [Input("a")]
    [Input("b")]
    public static async Task Rows(string value)
    {
      _ = value;
      BodyRuns++;
      await Task.CompletedTask;
    }
  }

  /// <summary>Class include matches any of the requested tags.</summary>
  [TestTag("ci")]
  public class TagFilterClassIncludeFixture
  {
    public static int PlainRuns;
    public static int OtherRuns;

    public static void Reset()
    {
      PlainRuns = 0;
      OtherRuns = 0;
    }

    public static async Task Plain()
    {
      PlainRuns++;
      await Task.CompletedTask;
    }

    [TestTag("other")]
    public static async Task Other()
    {
      OtherRuns++;
      await Task.CompletedTask;
    }
  }

  /// <summary>Nested runs must ignore an environment exclude of this tag.</summary>
  public class TagFilterNestedFixture
  {
    public static int SlowRuns;
    public static int PlainRuns;

    public static void Reset()
    {
      SlowRuns = 0;
      PlainRuns = 0;
    }

    [TestTag("slow")]
    public static async Task Slow()
    {
      SlowRuns++;
      await Task.CompletedTask;
    }

    public static async Task Plain()
    {
      PlainRuns++;
      await Task.CompletedTask;
    }
  }
