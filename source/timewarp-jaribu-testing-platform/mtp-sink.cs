namespace TimeWarp.Jaribu.TestingPlatform;

using System.Globalization;
using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;

#region Purpose
// Publishes Jaribu test nodes onto the Microsoft.Testing.Platform message bus.
#endregion

#region Design
// Failed, Timeout, and Error carry both the exception and TestNodeInfo.Message.
// A throwing CleanUp after a failing body keeps the body exception and puts the
// cleanup text only in Message. The explanation argument is what dotnet test shows,
// so dropping Message hides that cleanup failure.
// Cancelled maps to CancelledTestNodeStateProperty. MTP 2.3.3 still has that
// type and counts it toward the failed-test exit code. The type is obsolete
// (MTP0001): the platform text says to throw OperationCanceledException instead.
// Publishing the node is the report. The adapter does not also throw, because
// a throw would add a second Error node.
// Parameter keys use MtpTreeFilter.ParameterKeyPrefix. Parameter text and the
// metadata type name come from MtpNodeMetadata, the same helpers the filter uses.
// TimingProperty uses TestNodeInfo.StartTime and EndTime. A duration alone is
// not enough: the sink does not subtract it from the publish clock.
// Failed, Timeout, and Error with a null exception use one message that says
// the core supplied no exception. That message is the exception text.
#endregion

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

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestDiscoveredAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return PublishNodeAsync(node);
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestStartedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    // Always publish InProgress on start so skip/fail short-circuits are not
    // double-counted as terminal states (start + complete both Skipped → #22).
    TestNodeInfo inProgress = node with
    {
      State = TestNodeState.InProgress,
      Duration = null,
      Exception = null,
      Message = null
    };
    return PublishNodeAsync(inProgress);
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
  public Task OnTestCompletedAsync(TestNodeInfo node)
  {
    ArgumentNullException.ThrowIfNull(node);
    return PublishNodeAsync(node);
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException"><paramref name="className"/> is null.</exception>
  public Task OnRunStartedAsync(string className, string? filterTag = null)
  {
    ArgumentNullException.ThrowIfNull(className);
    return Task.CompletedTask;
  }

  /// <inheritdoc />
  /// <exception cref="ArgumentNullException">
  /// <paramref name="stats"/> or <paramref name="results"/> is null.
  /// </exception>
  public Task OnRunCompletedAsync(TestRunStats stats, IReadOnlyList<TestNodeInfo> results)
  {
    ArgumentNullException.ThrowIfNull(stats);
    ArgumentNullException.ThrowIfNull(results);
    return Task.CompletedTask;
  }

  private async Task PublishNodeAsync(TestNodeInfo node)
  {
    TestNodeStateProperty stateProperty = MapStateToProperty(node);
    PropertyBag properties = new(stateProperty);

    if (node.StartTime is DateTimeOffset startTime &&
        node.EndTime is DateTimeOffset endTime &&
        node.Duration is TimeSpan duration)
    {
      properties.Add(new TimingProperty(new TimingInfo(startTime, endTime, duration)));
    }

    if (node.Method is MethodInfo method)
    {
      properties.Add(CreateMethodIdentifier(method));
      TestFileLocationProperty? location = MethodSourceLocation.TryCreate(method);
      if (location is not null)
      {
        properties.Add(location);
      }
    }

    if (node.Parameters is { Count: > 0 } parameters)
    {
      for (int index = 0; index < parameters.Count; index++)
      {
        string key = string.Concat(
          MtpTreeFilter.ParameterKeyPrefix,
          index.ToString(CultureInfo.InvariantCulture));
        properties.Add(new TestMetadataProperty(key, MtpNodeMetadata.FormatParameter(parameters[index])));
      }
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
        node.Exception ?? MissingFailureException(node),
        node.Message
      ),
      TestNodeState.Timeout => new TimeoutTestNodeStateProperty
      (
        node.Exception ?? MissingFailureException(node),
        node.Message
      ),
      TestNodeState.Error => new ErrorTestNodeStateProperty
      (
        node.Exception ?? MissingFailureException(node),
        node.Message
      ),
      TestNodeState.Cancelled => CancelledProperty(node),
      _ => new ErrorTestNodeStateProperty
      (
        new InvalidOperationException($"Unknown test state: {node.State}")
      )
    };

  // MTP0001 marks this property obsolete and prefers a thrown OperationCanceledException.
  // The 2.3.3 host still reads the property for the summary and the exit code.
#pragma warning disable MTP0001 // Publishing Cancelled is the report. A throw would add a second Error node.
  private static InvalidOperationException MissingFailureException(TestNodeInfo node)
  {
    string detail = string.IsNullOrWhiteSpace(node.Message)
      ? $"Jaribu reported {node.State} for '{node.Uid}' and supplied no exception."
      : $"Jaribu reported {node.State} for '{node.Uid}' and supplied no exception. {node.Message}";
    return new InvalidOperationException(detail);
  }

  private static CancelledTestNodeStateProperty CancelledProperty(TestNodeInfo node)
  {
    return new CancelledTestNodeStateProperty(
      node.Exception ?? new OperationCanceledException(
        node.Message ?? $"Jaribu reported Cancelled for '{node.Uid}' and supplied no exception."),
      node.Message);
  }
#pragma warning restore MTP0001

  private static TestMethodIdentifierProperty CreateMethodIdentifier(MethodInfo method)
  {
    Type? declaringType = method.DeclaringType;
    string assemblyFullName = declaringType?.Assembly.FullName
      ?? method.Module.Assembly.FullName
      ?? string.Empty;
    string namespaceName = declaringType?.Namespace ?? string.Empty;
    string typeName = declaringType is null ? string.Empty : MtpNodeMetadata.MetadataTypeName(declaringType);
    ParameterInfo[] parameters = method.GetParameters();
    string[] parameterTypeFullNames = new string[parameters.Length];
    for (int index = 0; index < parameters.Length; index++)
    {
      Type parameterType = parameters[index].ParameterType;
      parameterTypeFullNames[index] = parameterType.FullName ?? parameterType.Name;
    }

    string returnTypeFullName = method.ReturnType.FullName ?? method.ReturnType.Name;
    return new TestMethodIdentifierProperty(
      assemblyFullName,
      namespaceName,
      typeName,
      method.Name,
      method.GetGenericArguments().Length,
      parameterTypeFullNames,
      returnTypeFullName);
  }
}
