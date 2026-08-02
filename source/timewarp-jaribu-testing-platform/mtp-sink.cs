namespace TimeWarp.Jaribu.TestingPlatform;

/// <summary>
/// Sink implementation that publishes test results to Microsoft.Testing.Platform's IMessageBus.
/// Translates TestNodeInfo lifecycle events into TestNodeUpdateMessage publications.
/// </summary>
internal sealed class MtpSink : ITestResultSink
{
  private readonly IDataProducer DataProducer;
  private readonly IMessageBus MessageBus;
  private readonly SessionUid SessionUid;

  public MtpSink
  (
    IDataProducer dataProducer,
    IMessageBus messageBus,
    SessionUid sessionUid
  )
  {
    ArgumentNullException.ThrowIfNull(dataProducer);
    ArgumentNullException.ThrowIfNull(messageBus);
    DataProducer = dataProducer;
    MessageBus = messageBus;
    SessionUid = sessionUid;
  }

  public Task OnTestDiscoveredAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return PublishNodeAsync(node);
  }

  public Task OnTestStartedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    // Always publish InProgress on start so skip/fail short-circuits are not
    // double-counted as terminal states (start + complete both Skipped → #22).
    TestNodeInfo inProgress = new(
      Uid: node.Uid,
      DisplayName: node.DisplayName,
      State: TestNodeState.InProgress,
      Duration: null,
      Exception: null,
      Message: null,
      Parameters: node.Parameters
    );
    return PublishNodeAsync(inProgress);
  }

  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return PublishNodeAsync(node);
  }

  public Task OnRunStartedAsync(string className, string? filterTag = null)
    => Task.CompletedTask;

  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results)
    => Task.CompletedTask;

  private async Task PublishNodeAsync(TestNodeInfo node)
  {
    TestNodeStateProperty stateProperty = MapStateToProperty(node);
    PropertyBag properties = new(stateProperty);

    if (node.Duration.HasValue)
    {
      DateTimeOffset endTime = DateTimeOffset.UtcNow;
      DateTimeOffset startTime = endTime - node.Duration.Value;
      properties.Add(new TimingProperty(new TimingInfo(startTime, endTime, node.Duration.Value)));
    }

    await MessageBus.PublishAsync
    (
      DataProducer,
      new TestNodeUpdateMessage
      (
        sessionUid: SessionUid,
        testNode: new TestNode
        {
          Uid = new TestNodeUid(node.Uid),
          DisplayName = node.DisplayName,
          Properties = properties
        }
      )
    ).ConfigureAwait(false);
  }

  private static TestNodeStateProperty MapStateToProperty(TestNodeInfo node)
    => node.State switch
    {
      TestNodeState.Discovered => DiscoveredTestNodeStateProperty.CachedInstance,
      TestNodeState.InProgress => InProgressTestNodeStateProperty.CachedInstance,
      TestNodeState.Passed => PassedTestNodeStateProperty.CachedInstance,
      TestNodeState.Skipped => new SkippedTestNodeStateProperty(node.Message),
      TestNodeState.Failed => new FailedTestNodeStateProperty
      (
        node.Exception ?? new InvalidOperationException("Test failed without exception details")
      ),
      TestNodeState.Timeout => new TimeoutTestNodeStateProperty
      (
        node.Exception ?? new TimeoutException("Test timed out")
      ),
      TestNodeState.Error => new ErrorTestNodeStateProperty
      (
        node.Exception ?? new InvalidOperationException("Test error without exception details")
      ),
      TestNodeState.Cancelled => new ErrorTestNodeStateProperty
      (
        new OperationCanceledException(node.Message ?? "Test cancelled")
      ),
      _ => new ErrorTestNodeStateProperty
      (
        new InvalidOperationException($"Unknown test state: {node.State}")
      )
    };
}
