namespace TimeWarp.Jaribu.TestingPlatform;

/// <summary>
/// Implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Bridges Jaribu's test runner with M.T.P. for test discovery and execution.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer
{
  private readonly IExtension Extension;

  public JaribuTestFramework
  (
    IExtension extension,
    IServiceProvider _
  )
  {
    Extension = extension;
  }

  public string Uid => Extension.Uid;
  public string Version => Extension.Version;
  public string DisplayName => Extension.DisplayName;
  public string Description => Extension.Description;

  public Type[] DataTypesProduced =>
  [
    typeof(TestNodeUpdateMessage)
  ];

  public Task<bool> IsEnabledAsync() => Extension.IsEnabledAsync();

  public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
  {
    TestRunner.BeginTestSession();
    return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
  }

  public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
  {
    try
    {
      await TestRunner.EndTestSessionAsync().ConfigureAwait(false);
      return new CloseTestSessionResult { IsSuccess = true };
    }
#pragma warning disable CA1031 // Host boundary: any dispose failure must become IsSuccess=false
    catch (Exception ex)
#pragma warning restore CA1031
    {
      return new CloseTestSessionResult
      {
        IsSuccess = false,
        ErrorMessage = $"Session fixture dispose failed: {ex.Message}"
      };
    }
  }

  public async Task ExecuteRequestAsync(ExecuteRequestContext context)
  {
    try
    {
      bool isDiscovery = context.Request is DiscoverTestExecutionRequest;
      ITestExecutionFilter? filter = GetFilter(context);
      MtpSink sink = new(this, context.MessageBus, context.Request.Session.SessionUid);

      foreach (Type testClass in TestRunner.RegisteredTestClasses)
      {
        if (isDiscovery)
        {
          foreach (System.Reflection.MethodInfo method in TestRunner.DiscoverTests(testClass))
          {
            string testNodeUid = $"{testClass.FullName}.{method.Name}";
            if (filter is not null && !MatchesFilter(testNodeUid, filter))
              continue;

            TestNodeInfo node = new
            (
              Uid: testNodeUid,
              DisplayName: method.Name,
              State: TestNodeState.Discovered
            );
            await sink.OnTestDiscoveredAsync(node).ConfigureAwait(false);
          }
        }
        else
        {
          await TestRunner.RunTestsAsync(testClass, sink).ConfigureAwait(false);
        }
      }
    }
    catch (Exception ex)
    {
      await ReportUnhandledExceptionAsync(context, ex).ConfigureAwait(false);
      throw;
    }
    finally
    {
      context.Complete();
    }
  }

  private async Task ReportUnhandledExceptionAsync(ExecuteRequestContext context, Exception ex)
  {
    await context.MessageBus.PublishAsync
    (
      this,
      new TestNodeUpdateMessage
      (
        sessionUid: context.Request.Session.SessionUid,
        testNode: new TestNode
        {
          Uid = new TestNodeUid($"Jaribu.Error.{Guid.NewGuid()}"),
          DisplayName = $"Unhandled: {ex.GetType().Name}",
          Properties = new PropertyBag(new ErrorTestNodeStateProperty(ex))
        }
      )
    ).ConfigureAwait(false);
  }

  private static ITestExecutionFilter? GetFilter(ExecuteRequestContext context)
    => context.Request switch
    {
      RunTestExecutionRequest r => r.Filter,
      DiscoverTestExecutionRequest d => d.Filter,
      _ => null
    };

#pragma warning disable TPEXP // TreeNodeFilter is experimental
  private static bool MatchesFilter(string testNodeUid, ITestExecutionFilter filter)
    => filter switch
    {
      TestNodeUidListFilter uidFilter =>
        uidFilter.TestNodeUids.Any(u => u.Value == testNodeUid),
      TreeNodeFilter treeFilter =>
        treeFilter.MatchesFilter(testNodeUid, new PropertyBag()),
      _ => true
    };
#pragma warning restore TPEXP
}
