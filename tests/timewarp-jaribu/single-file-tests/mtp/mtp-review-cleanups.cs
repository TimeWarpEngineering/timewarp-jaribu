#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-jaribu/timewarp-jaribu.csproj
#:project $(SourceDirectory)timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj

#region Purpose
// MTP checks for sequence parameter text, missing-exception messages, real timing, PDB misses, and extension identity.
#endregion

#region Design
// MtpSink, MethodSourceLocation, and the two extensions are internal.
// InternalsVisibleTo names run-ci-tests, timewarp-jaribu-tests-mtp, and this file.
// Timing is asserted from the timestamps on the node. A duration with no timestamps publishes none.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace Mtp_
{
  using System.Reflection;
  using System.Reflection.Emit;
  using System.Runtime.CompilerServices;
  using Microsoft.Testing.Platform.Extensions;
  using Microsoft.Testing.Platform.Extensions.Messages;
  using Microsoft.Testing.Platform.Messages;
  using Microsoft.Testing.Platform.TestHost;
  using TimeWarp.Jaribu.TestingPlatform;

  [TestTag("Jaribu")]
  public class ReviewCleanups_Given_
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ReviewCleanups_Given_>();

    public static async Task FormatParameter_Should_PrintSequenceElementsAndLeaveStringsUnquoted()
    {
      List<int> numbers = [1, 2];
      List<string> words = ["a", "b"];
      MtpNodeMetadata.FormatParameter(numbers).ShouldBe("[1,2]");
      MtpNodeMetadata.FormatParameter(words).ShouldBe("[a,b]");
      MtpNodeMetadata.FormatParameter("fail").ShouldBe("fail");
      MtpNodeMetadata.FormatParameter(null).ShouldBe("null");
      await Task.CompletedTask;
    }

    public static async Task MissingException_Should_SayTheCoreSuppliedNone()
    {
      await AssertMissingAsync(TestNodeState.Failed);
      await AssertMissingAsync(TestNodeState.Timeout);
      await AssertMissingAsync(TestNodeState.Error);
    }

    public static async Task Timing_Should_UseNodeTimestamps()
    {
      DateTimeOffset start = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
      DateTimeOffset end = start.AddMilliseconds(12);
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));

      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.Timed",
        "Timed",
        TestNodeState.Passed,
        Duration: TimeSpan.FromMilliseconds(12),
        StartTime: start,
        EndTime: end));

      TimingInfo timing = bus.Messages.Single().TestNode.Properties.Single<TimingProperty>().GlobalTiming;
      timing.StartTime.ShouldBe(start);
      timing.EndTime.ShouldBe(end);
      timing.Duration.ShouldBe(TimeSpan.FromMilliseconds(12));
    }

    public static async Task DurationWithoutTimestamps_Should_PublishNoTiming()
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));

      await sink.OnTestCompletedAsync(new TestNodeInfo(
        "Probe.DurationOnly",
        "DurationOnly",
        TestNodeState.Passed,
        Duration: TimeSpan.FromMilliseconds(12)));

      bus.Messages.Single().TestNode.Properties.OfType<TimingProperty>().ShouldBeEmpty();
    }

    public static async Task DynamicMethod_Should_MissWithoutThrowing()
    {
      if (!RuntimeFeature.IsDynamicCodeSupported)
      {
        MethodInfo fallback = typeof(ReviewCleanups_Given_).GetMethod(nameof(DynamicMethod_Should_MissWithoutThrowing))!;
        Should.NotThrow(() => MethodSourceLocation.TryCreate(fallback));
        await Task.CompletedTask;
        return;
      }

      DynamicMethod method = new("DynamicProbe", typeof(void), Type.EmptyTypes);
      MethodSourceLocation.TryCreate(method).ShouldBeNull();
      await Task.CompletedTask;
    }

    public static async Task Extensions_Should_UseDistinctUidsAndInformationalVersion()
    {
      JaribuExtension framework = new();
      JaribuCommandLineOptionsProvider commandLine = new();
      framework.Uid.ShouldBe("TimeWarp.Jaribu");
      commandLine.Uid.ShouldBe("TimeWarp.Jaribu.CommandLine");
      framework.Version.ShouldBe(commandLine.Version);

      string? informational = typeof(JaribuExtension).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;
      string expected = informational ?? string.Empty;
      int plus = expected.IndexOf('+', StringComparison.Ordinal);
      if (plus > 0)
      {
        expected = expected[..plus];
      }

      framework.Version.ShouldBe(expected);
      framework.Version.ShouldContain("beta");
      await Task.CompletedTask;
    }

    private static async Task AssertMissingAsync(TestNodeState state)
    {
      RecordingBus bus = new();
      MtpSink sink = new(new FakeProducer(), bus, new SessionUid("session"));
      await sink.OnTestCompletedAsync(new TestNodeInfo(
        $"Probe.{state}",
        state.ToString(),
        state,
        Message: "detail"));

      Exception exception = ExceptionOf(bus.Messages.Single().TestNode, state);
      exception.ShouldBeOfType<InvalidOperationException>();
      exception.Message.ShouldContain("supplied no exception");
      exception.Message.ShouldContain("detail");
      exception.ShouldNotBeOfType<TimeoutException>();
    }

    private static Exception ExceptionOf(TestNode node, TestNodeState state)
    {
      TestNodeStateProperty property = node.Properties.Single<TestNodeStateProperty>();
      return state switch
      {
        TestNodeState.Failed => property.ShouldBeOfType<FailedTestNodeStateProperty>().Exception!,
        TestNodeState.Timeout => property.ShouldBeOfType<TimeoutTestNodeStateProperty>().Exception!,
        TestNodeState.Error => property.ShouldBeOfType<ErrorTestNodeStateProperty>().Exception!,
        _ => throw new InvalidOperationException($"Unexpected state {state}.")
      };
    }

    private sealed class RecordingBus : IMessageBus
    {
      public List<TestNodeUpdateMessage> Messages { get; } = [];

      public Task PublishAsync(IDataProducer dataProducer, IData data)
      {
        if (data is TestNodeUpdateMessage message)
        {
          Messages.Add(message);
        }

        return Task.CompletedTask;
      }
    }

    private sealed class FakeProducer : IDataProducer
    {
      public string Uid => "mtp-review-cleanups";

      public string Version => "1";

      public string DisplayName => "mtp-review-cleanups";

      public string Description => "records MTP messages";

      public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

      public Task<bool> IsEnabledAsync() => Task.FromResult(true);
    }
  }
}
