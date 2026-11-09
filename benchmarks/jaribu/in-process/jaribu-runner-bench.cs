#region Purpose
// Times TestRunner.RunTestsAsync through NullSink, without a process launch or the terminal table.
#endregion

#region Design
// ShortRunJob keeps the baseline repeatable. The fixture benchmark opens one host
// session so the session fixture is created once across the 100 classes, matching
// RunAllTests. Other benchmarks time a single class. Counts come from the generator spec.
#endregion

namespace Jaribu.InProcess;

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
  public Task Startup() => RunAsync<StartupTests>(1);

  [Benchmark]
  public Task Volume1K() => RunAsync<Volume1KTests>(1_000);

  [Benchmark]
  public Task Volume10K() => RunAsync<Volume10KTests>(10_000);

  [Benchmark]
  public Task Parameterized() => RunAsync<ParameterizedTests>(5_000);

  [Benchmark]
  public Task Async1K() => RunAsync<Async1KTests>(1_000);

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
        await RunAsync(type!, 10).ConfigureAwait(false);
      }
    }
    finally
    {
      await TestRunner.EndTestSessionAsync(epoch).ConfigureAwait(false);
    }
  }

  private static async Task RunAsync<T>(int expected) where T : class
  {
    await RunAsync(typeof(T), expected).ConfigureAwait(false);
  }

  private static async Task RunAsync(Type testClass, int expected)
  {
    TestRunStats stats = await TestRunner.RunTestsAsync(testClass, NullSink.Instance).ConfigureAwait(false);
    if (!stats.Success || stats.PassedCount != expected)
    {
      throw new InvalidOperationException(
        testClass.Name + " passed " + stats.PassedCount.ToString(CultureInfo.InvariantCulture)
        + " failed " + stats.FailedCount.ToString(CultureInfo.InvariantCulture)
        + ", expected " + expected.ToString(CultureInfo.InvariantCulture));
    }
  }
}
