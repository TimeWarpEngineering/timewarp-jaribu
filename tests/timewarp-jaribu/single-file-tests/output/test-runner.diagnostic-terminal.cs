#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Checks that runner diagnostics and real node timestamps use the supplied terminal and clock.
#endregion

#region Design
// Naming convention: SUT_Action_Given_Should_Result
// - Namespace = SUT (TestRunner_)
// - Class = Action + Given (DiagnosticTerminal_Given_)
// - Method = Given condition + Should + Result
//
// Each test swaps RegisteredTestClasses and restores it, matching the other
// multi-class meta-tests. The dispose case uses ClearRegisteredSessionFixtures,
// the meta-test escape hatch, so RunAllTests owns the outermost session and the
// dispose line is written on the supplied terminal.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Reflection;

  [TestTag("Output")]
  public class DiagnosticTerminal_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<DiagnosticTerminal_Given_>();

    public static async Task TwoClasses_Should_WriteGrandTotalToSuppliedTerminal()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        RegisterTests<DiagnosticPassingA>();
        RegisterTests<DiagnosticPassingB>();
        using TestTerminal terminal = new();

        int exitCode = await RunAllTests(terminal: terminal);

        exitCode.ShouldBe(0);
        terminal.Output.ShouldContain("Grand Total");
      });
    }

    public static async Task EmptyRegistry_Should_WriteWarningAndReturnOne()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        using TestTerminal terminal = new();

        int exitCode = await RunAllTests(terminal: terminal);

        exitCode.ShouldBe(1);
        terminal.Output.ShouldContain("No test classes registered");
      });
    }

    public static async Task StatusHelpers_Should_WriteToSuppliedTerminal()
    {
      using TestTerminal terminal = new();

      TestHelpers.TestPassed(terminal);
      TestHelpers.TestFailed(terminal, "because");
      TestHelpers.TestSkipped(terminal, "later");

      terminal.Output.ShouldContain("PASSED");
      terminal.Output.ShouldContain("FAILED: because");
      terminal.Output.ShouldContain("SKIPPED: later");
      await Task.CompletedTask;
    }

    public static async Task OutermostDisposeFailure_Should_WriteToSuppliedTerminal()
    {
      await WithIsolatedRegistryAsync(async () =>
      {
        ClearRegisteredSessionFixtures();
        RegisterSessionFixture<DiagnosticDisposeFixture>();
        RegisterTests<DiagnosticUsesDisposeFixture>();
        try
        {
          using TestTerminal terminal = new();
          int exitCode = await RunAllTests(terminal: terminal);

          exitCode.ShouldBe(1);
          terminal.Output.ShouldContain("Session fixture dispose failed");
          terminal.Output.ShouldContain("dispose boom");
        }
        finally
        {
          await EndTestSessionAsync();
          ClearRegisteredSessionFixtures();
        }
      });
    }

    public static async Task PassingNode_Should_CarryStartAndEnd()
    {
      DiagnosticCollectingSink sink = new();
      TestRunStats stats = await RunTestsAsync<DiagnosticPassingA>(sink);
      stats.Success.ShouldBeTrue();

      TestNodeInfo node = sink.Results.Single();
      DateTimeOffset start = node.StartTime.ShouldNotBeNull();
      DateTimeOffset end = node.EndTime.ShouldNotBeNull();
      TimeSpan duration = node.Duration.ShouldNotBeNull();
      end.ShouldBeGreaterThanOrEqualTo(start);
      (end - start).ShouldBe(duration);

      MethodInfo method = typeof(DiagnosticPassingA).GetMethod(nameof(DiagnosticPassingA.Passes))!;
      TestNodeInfo single = await RunSingleTestAsync(typeof(DiagnosticPassingA), method);
      DateTimeOffset singleStart = single.StartTime.ShouldNotBeNull();
      DateTimeOffset singleEnd = single.EndTime.ShouldNotBeNull();
      TimeSpan singleDuration = single.Duration.ShouldNotBeNull();
      (singleEnd - singleStart).ShouldBe(singleDuration);
    }

    private static async Task WithIsolatedRegistryAsync(Func<Task> body)
    {
      Type[] saved = [.. RegisteredTestClasses];
      try
      {
        ClearRegisteredTests();
        await body();
      }
      finally
      {
        ClearRegisteredTests();
        MethodInfo register = typeof(TestRunner).GetMethod(nameof(RegisterTests))!;
        foreach (Type type in saved)
        {
          register.MakeGenericMethod(type).Invoke(null, null);
        }
      }
    }
  }

  sealed class DiagnosticCollectingSink : ITestResultSink
  {
    public List<TestNodeInfo> Results { get; } = [];

    public Task OnTestDiscoveredAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestStartedAsync(TestNodeInfo node) => Task.CompletedTask;

    public Task OnTestCompletedAsync(TestNodeInfo node)
    {
      Results.Add(node);
      return Task.CompletedTask;
    }

    public Task OnRunStartedAsync(string className, string? filterTag = null) => Task.CompletedTask;

    public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results) => Task.CompletedTask;
  }

  public sealed class DiagnosticDisposeFixture : IAsyncDisposable
  {
    public static Task<DiagnosticDisposeFixture> CreateAsync()
      => Task.FromResult(new DiagnosticDisposeFixture());

    public ValueTask DisposeAsync()
      => throw new InvalidOperationException("dispose boom");
  }

  public class DiagnosticUsesDisposeFixture
  {
    public static async Task SetupOnce()
    {
      _ = await SessionFixture.GetAsync<DiagnosticDisposeFixture>();
    }

    public static async Task OnlyTest()
    {
      await Task.CompletedTask;
    }
  }

  public class DiagnosticPassingA
  {
    public static async Task Passes()
    {
      await Task.CompletedTask;
    }
  }

  public class DiagnosticPassingB
  {
    public static async Task Passes()
    {
      await Task.CompletedTask;
    }
  }
}
