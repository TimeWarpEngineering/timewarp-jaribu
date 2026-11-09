#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj

#region Purpose
// Locks overload uids, invariant argument text, FormatTestName scope, and [Input(null)].
#endregion

#region Design
// Fixtures are not registered. The meta-tests drive them through DiscoverTestCases,
// RunTestsAsync, and the public identity helpers. Culture is set to de-DE around
// the formatting checks and restored before the method returns.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TestRunner_
{
  using System.Globalization;
  using System.Reflection;

  [TestTag("Jaribu")]
  public class CaseDisplay_Given_
  {
    private static readonly int[] SampleValues = [1, 2];

    [ModuleInitializer]
    internal static void Register() => RegisterTests<CaseDisplay_Given_>();

    public static async Task Overloads_Should_KeepDistinctUids()
    {
      Type overloaded = typeof(OverloadFixture);
      List<ReportedTestCase> discovered = [.. TestRunner.DiscoverTestCases(overloaded)];
      discovered.Count.ShouldBe(2);
      string intUid = $"{overloaded.FullName}.Sum(System.Int32)[0]";
      string stringUid = $"{overloaded.FullName}.Sum(System.String)[0]";
      discovered.ShouldContain(testCase => testCase.Uid == intUid);
      discovered.ShouldContain(testCase => testCase.Uid == stringUid);
      discovered.ShouldNotContain(testCase => testCase.Uid.EndsWith(".Sum[0]", StringComparison.Ordinal));

      ReportedTestCase intCase = discovered.Single(testCase => testCase.Uid == intUid);
      ReportedTestCase stringCase = discovered.Single(testCase => testCase.Uid == stringUid);
      intCase.DisplayName.ShouldBe("Sum(System.Int32)(2)");
      stringCase.DisplayName.ShouldBe("Sum(System.String)(\"two\")");

      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync(overloaded, sink);
      stats.PassedCount.ShouldBe(2);
      stats.FailedCount.ShouldBe(0);
      sink.Results.Single(node => node.Uid == intUid).DisplayName.ShouldBe(intCase.DisplayName);
      sink.Results.Single(node => node.Uid == stringUid).DisplayName.ShouldBe(stringCase.DisplayName);

      Type single = typeof(SingleSumFixture);
      MethodInfo method = single.GetMethod(nameof(SingleSumFixture.Sum))!;
      TestCaseIdentity.MethodUid(single, method).ShouldBe($"{single.FullName}.Sum");
      TestCaseIdentity.FormatReportedName(single, method, parameters: null).ShouldBe(nameof(SingleSumFixture.Sum));
    }

    public static async Task GenericOverloads_Should_UseParameterTypeNames()
    {
      Type type = typeof(GenericOverloadFixture);
      MethodInfo[] methods = [.. type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.Name == "Row")];
      methods.Length.ShouldBe(2);
      string intUid = TestCaseIdentity.MethodUid(type, methods.Single(method => method.GetParameters()[0].ParameterType == typeof(List<int>)));
      string stringUid = TestCaseIdentity.MethodUid(type, methods.Single(method => method.GetParameters()[0].ParameterType == typeof(List<string>)));
      intUid.ShouldBe($"{type.FullName}.Row(System.Collections.Generic.List<System.Int32>)");
      stringUid.ShouldBe($"{type.FullName}.Row(System.Collections.Generic.List<System.String>)");
      intUid.ShouldNotBe(stringUid);
      await Task.CompletedTask;
    }

    public static async Task Arguments_Should_FormatInvariantlyUnderGermanCulture()
    {
      CultureInfo original = CultureInfo.CurrentCulture;
      CultureInfo originalUi = CultureInfo.CurrentUICulture;
      try
      {
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.CurrentCulture = german;
        CultureInfo.CurrentUICulture = german;

        string direct = TestCaseIdentity.FormatDisplayName(
          "AddsNumbers",
          [1234.5, "1", 1, SampleValues, "say \"hi\""]);
        direct.ShouldBe("AddsNumbers(1234.5, \"1\", 1, [1, 2], \"say \\\"hi\\\"\")");
        direct.ShouldNotContain("1234,5");
        direct.ShouldNotContain("System.Int32[]");

        Type fixture = typeof(FormatFixture);
        List<ReportedTestCase> discovered = [.. TestRunner.DiscoverTestCases(fixture)];
        discovered.Select(testCase => testCase.DisplayName).ShouldBe(
        [
          "Show(1234.5)",
          "Show(\"1\")",
          "Show(1)",
          "Show([1, 2])"
        ]);

        MethodInfo method = fixture.GetMethod(nameof(FormatFixture.Show))!;
        string fallback = TestCaseIdentity.CaseUid(fixture, method, inputOrdinal: null, [1234.5, "1"]);
        fallback.ShouldBe($"{fixture.FullName}.Show(1234.5, \"1\")");

        CollectingSink sink = new();
        TestRunStats stats = await TestRunner.RunTestsAsync(fixture, sink);
        stats.PassedCount.ShouldBe(4);
        stats.FailedCount.ShouldBe(0);
        sink.Results.Select(node => node.DisplayName).ShouldBe(discovered.Select(testCase => testCase.DisplayName));
      }
      finally
      {
        CultureInfo.CurrentCulture = original;
        CultureInfo.CurrentUICulture = originalUi;
      }
    }

    public static async Task FormatTestName_Should_TouchTheMethodNameOnly()
    {
      TestHelpers.FormatTestName("CatchAllInOptionShouldFail").ShouldBe("Catch All In Option Should Fail");
      TestHelpers.FormatTestName("Method(Foo, Bar)").ShouldBe("Method(Foo, Bar)");
      TestHelpers.FormatTestName("AddsNumbers(Foo, Bar)").ShouldBe("Adds Numbers(Foo, Bar)");

      using TimeWarp.Terminal.TestTerminal terminal = new();
      using TerminalSink sink = new(terminal);
      await sink.OnTestStartedAsync(new TestNodeInfo(
        "uid",
        "AddsNumbers(Foo, Bar)",
        TestNodeState.InProgress));
      terminal.Output.ShouldContain("Adds Numbers(Foo, Bar)");
      terminal.Output.ShouldNotContain("Adds Numbers( Foo");
    }

    public static async Task NullInput_Should_BindOneNullArgument()
    {
      Type fixture = typeof(NullInputFixture);
      List<ReportedTestCase> discovered = [.. TestRunner.DiscoverTestCases(fixture)];
      discovered.Count.ShouldBe(1);
      discovered[0].Uid.ShouldBe($"{fixture.FullName}.OneNull[0]");
      discovered[0].DisplayName.ShouldBe("OneNull(null)");
      discovered[0].Parameters.ShouldNotBeNull();
      discovered[0].Parameters!.Count.ShouldBe(1);
      discovered[0].Parameters![0].ShouldBeNull();

      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync(fixture, sink);
      stats.PassedCount.ShouldBe(1);
      stats.FailedCount.ShouldBe(0);
      sink.Results.Single().DisplayName.ShouldBe("OneNull(null)");
      NullInputFixture.SawNull.ShouldBeTrue();
    }

    public static async Task InputOrder_Should_FollowSourceOrder()
    {
      Type fixture = typeof(OrderFixture);
      List<ReportedTestCase> discovered = [.. TestRunner.DiscoverTestCases(fixture)];
      discovered.Count.ShouldBe(4);
      discovered[0].Uid.ShouldEndWith("Letters[0]");
      discovered[1].Uid.ShouldEndWith("Letters[1]");
      discovered[2].Uid.ShouldEndWith("Paint[0]");
      discovered[3].Uid.ShouldEndWith("Paint[1]");
      discovered[0].Parameters.ShouldNotBeNull();
      discovered[0].Parameters![0].ShouldBe("b");
      discovered[1].Parameters![0].ShouldBe("a");
      discovered[2].Parameters![0].ShouldBe(OrderFixture.SampleColor.Blue);
      discovered[3].Parameters![0].ShouldBe(OrderFixture.SampleColor.Red);
      discovered[2].DisplayName.ShouldBe("Paint(Blue)");
      discovered[3].DisplayName.ShouldBe("Paint(Red)");

      CollectingSink sink = new();
      TestRunStats stats = await TestRunner.RunTestsAsync(fixture, sink);
      stats.PassedCount.ShouldBe(4);
      stats.FailedCount.ShouldBe(0);
      sink.Results[0].Uid.ShouldBe(discovered[0].Uid);
      sink.Results[2].State.ShouldBe(TestNodeState.Passed);
    }

    private sealed class CollectingSink : ITestResultSink
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

    private sealed class OverloadFixture
    {
      [Input(2)]
      public static async Task Sum(int value)
      {
        value.ShouldBe(2);
        await Task.CompletedTask;
      }

      [Input("two")]
      public static async Task Sum(string value)
      {
        value.ShouldBe("two");
        await Task.CompletedTask;
      }
    }

    private sealed class SingleSumFixture
    {
      public static async Task Sum(int value)
      {
        _ = value;
        await Task.CompletedTask;
      }
    }

    private sealed class GenericOverloadFixture
    {
      public static Task Row(List<int> values)
      {
        _ = values;
        return Task.CompletedTask;
      }

      public static Task Row(List<string> values)
      {
        _ = values;
        return Task.CompletedTask;
      }
    }

    private sealed class FormatFixture
    {
      [Input(1234.5)]
      [Input("1")]
      [Input(1)]
      [Input(new[] { 1, 2 })]
      public static async Task Show(object? value)
      {
        value.ShouldNotBeNull();
        await Task.CompletedTask;
      }
    }

    private sealed class NullInputFixture
    {
      public static bool SawNull;

      [Input(null)]
      public static async Task OneNull(string? value)
      {
        SawNull = value is null;
        if (value is not null)
        {
          throw new InvalidOperationException("expected one null argument");
        }

        await Task.CompletedTask;
      }
    }

    private sealed class OrderFixture
    {
      [Input("b")]
      [Input("a")]
      public static async Task Letters(string value)
      {
        if (value is not "a" and not "b")
        {
          throw new InvalidOperationException(value);
        }

        await Task.CompletedTask;
      }

      [Input(SampleColor.Blue)]
      [Input(SampleColor.Red)]
      public static async Task Paint(SampleColor color)
      {
        if (color is not SampleColor.Blue and not SampleColor.Red)
        {
          throw new InvalidOperationException(color.ToString());
        }

        await Task.CompletedTask;
      }

      public enum SampleColor
      {
        Red = 2,
        Blue = 5
      }
    }
  }
}
