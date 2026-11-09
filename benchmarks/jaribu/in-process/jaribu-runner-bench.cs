#region Purpose
// Times TestRunner.RunTestsAsync through NullSink, without a process launch or the terminal table.
#endregion

#region Design
// ShortRunJob keeps the baseline repeatable. The fixture benchmark opens one host
// session so the session fixture is created once across the 100 classes, matching
// RunAllTests. Other benchmarks time a single class. Counts come from the generator spec.
#endregion

using System.Globalization;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Exporters.Json;
using Jaribu.Benchmarks;
using TimeWarp.Jaribu;

[MemoryDiagnoser]
[ShortRunJob]
[JsonExporterAttribute.Full]
public class JaribuRunnerBench
{
  [Benchmark]
  public Task Startup() => Run<StartupTests>(1);

  [Benchmark]
  public Task Volume1K() => Run<Volume1KTests>(1_000);

  [Benchmark]
  public Task Volume10K() => Run<Volume10KTests>(10_000);

  [Benchmark]
  public Task Parameterized() => Run<ParameterizedTests>(5_000);

  [Benchmark]
  public Task Async1K() => Run<Async1KTests>(1_000);

  [Benchmark]
  public async Task Fixtures()
  {
    long epoch = TestRunner.BeginTestSession();
    try
    {
      Assembly assembly = typeof(StartupTests).Assembly;
      for (int index = 0; index < 100; index++)
      {
        string name = "Jaribu.Benchmarks.FixtureClass" + index.ToString("000", CultureInfo.InvariantCulture);
        Type? type = assembly.GetType(name, throwOnError: true);
        await Run(type!, 10);
      }
    }
    finally
    {
      await TestRunner.EndTestSessionAsync(epoch);
    }
  }

  private static async Task Run<T>(int expected) where T : class
  {
    await Run(typeof(T), expected);
  }

  private static async Task Run(Type testClass, int expected)
  {
    TestRunStats stats = await TestRunner.RunTestsAsync(testClass, NullSink.Instance);
    if (!stats.Success || stats.PassedCount != expected)
    {
      throw new InvalidOperationException(
        testClass.Name + " passed " + stats.PassedCount.ToString(CultureInfo.InvariantCulture)
        + " failed " + stats.FailedCount.ToString(CultureInfo.InvariantCulture)
        + ", expected " + expected.ToString(CultureInfo.InvariantCulture));
    }
  }
}
