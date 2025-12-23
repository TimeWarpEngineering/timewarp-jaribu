using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TimeWarp.Jaribu.TestingPlatform;

/// <summary>
/// Implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Bridges Jaribu's test runner with M.T.P. for test discovery and execution.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;

    public JaribuTestFramework(IExtension extension, IServiceProvider serviceProvider)
    {
        _extension = extension;
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;

    public Type[] DataTypesProduced =>
    [
        typeof(TestNodeUpdateMessage)
    ];

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        try
        {
            bool isDiscovery = context.Request is DiscoverTestExecutionRequest;
            ITestExecutionFilter? filter = GetFilter(context);

            foreach (Type testClass in TestRunner.RegisteredTestClasses)
            {
                IEnumerable<System.Reflection.MethodInfo> testMethods = TestRunner.DiscoverTests(testClass);

                foreach (System.Reflection.MethodInfo method in testMethods)
                {
                    string testNodeUid = $"{testClass.FullName}.{method.Name}";

                    if (filter != null && !MatchesFilter(testNodeUid, filter))
                        continue;

                    if (isDiscovery)
                    {
                        await PublishTestNode(context, testNodeUid, method.Name,
                            DiscoveredTestNodeStateProperty.CachedInstance).ConfigureAwait(false);
                    }
                    else
                    {
                        // Report in-progress
                        await PublishTestNode(context, testNodeUid, method.Name,
                            InProgressTestNodeStateProperty.CachedInstance).ConfigureAwait(false);

                        // Execute test
                        MtpTestResult result = await TestRunner.RunSingleTestAsync(testClass, method).ConfigureAwait(false);

                        // Report result
                        TestNodeStateProperty stateProperty = MapStatusToProperty(result);
                        await PublishTestNode(context, testNodeUid, method.Name,
                            stateProperty, result.Duration).ConfigureAwait(false);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ReportUnhandledException(context, ex).ConfigureAwait(false);
            throw;
        }
        finally
        {
            context.Complete();
        }
    }

    private static TestNodeStateProperty MapStatusToProperty(MtpTestResult result)
        => result.Status switch
        {
            TestStatus.Passed => PassedTestNodeStateProperty.CachedInstance,
            TestStatus.Skipped => new SkippedTestNodeStateProperty(result.SkipReason),
            TestStatus.Failed => new FailedTestNodeStateProperty(
                result.Exception ?? new InvalidOperationException("Test failed without exception details")),
            TestStatus.Timeout => new TimeoutTestNodeStateProperty(
                result.Exception ?? new TimeoutException("Test timed out")),
            _ => new ErrorTestNodeStateProperty(
                result.Exception ?? new InvalidOperationException("Test error without exception details"))
        };

    private async Task PublishTestNode(
        ExecuteRequestContext context,
        string uid,
        string displayName,
        TestNodeStateProperty state,
        TimeSpan? duration = null)
    {
        var properties = new PropertyBag(state);

        if (duration.HasValue)
        {
            DateTimeOffset endTime = DateTimeOffset.UtcNow;
            DateTimeOffset startTime = endTime - duration.Value;
            properties.Add(new TimingProperty(new TimingInfo(startTime, endTime, duration.Value)));
        }

        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: context.Request.Session.SessionUid,
            testNode: new TestNode
            {
                Uid = new TestNodeUid(uid),
                DisplayName = displayName,
                Properties = properties
            })).ConfigureAwait(false);
    }

    private async Task ReportUnhandledException(ExecuteRequestContext context, Exception ex)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: context.Request.Session.SessionUid,
            testNode: new TestNode
            {
                Uid = new TestNodeUid($"Jaribu.Error.{Guid.NewGuid()}"),
                DisplayName = $"Unhandled: {ex.GetType().Name}",
                Properties = new PropertyBag(new ErrorTestNodeStateProperty(ex))
            })).ConfigureAwait(false);
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
