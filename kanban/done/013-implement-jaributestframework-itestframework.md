# Implement JaribuTestFramework (ITestFramework)

## Summary

Implement the core `JaribuTestFramework` class that bridges Jaribu's test runner with Microsoft.Testing.Platform. This class handles test discovery, execution, and result reporting via the M.T.P. message bus.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### Core Implementation
- [ ] Implement `ITestFramework` interface
- [ ] Implement `IDataProducer` interface
- [ ] Implement `CreateTestSessionAsync` (session setup)
- [ ] Implement `CloseTestSessionAsync` (session cleanup)

### ExecuteRequestAsync
- [ ] Handle `DiscoverTestExecutionRequest` (--list-tests)
- [ ] Handle `RunTestExecutionRequest` (test execution)
- [ ] Iterate over `RegisteredTestClasses`
- [ ] Call `TestRunner.DiscoverTests()` for each class
- [ ] Call `TestRunner.RunSingleTestAsync()` for execution

### Result Publishing
- [ ] Publish `TestNodeUpdateMessage` for each test
- [ ] Map `TestResult.Status` to M.T.P. state properties
- [ ] Include timing information (`TimingProperty`)
- [ ] Include exception details for failures

### Filtering
- [ ] Handle `TestNodeUidListFilter` (specific test selection)
- [ ] Handle `TreeNodeFilter` (--filter expressions)
- [ ] Generate proper `TestNodeUid` format

### Error Handling
- [ ] Handle infrastructure exceptions
- [ ] Report unhandled exceptions as `ErrorTestNodeStateProperty`
- [ ] Ensure `context.Complete()` is always called

## Notes

### ITestFramework Interface

```csharp
public interface ITestFramework : IExtension
{
    Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context);
    Task ExecuteRequestAsync(ExecuteRequestContext context);
    Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context);
}
```

### Request Types

```csharp
// Discovery mode (--list-tests)
context.Request is DiscoverTestExecutionRequest

// Execution mode (run tests)
context.Request is RunTestExecutionRequest
```

### TestNodeUid Format

Use fully qualified name for stable, unique identification:
```
Namespace.ClassName.MethodName
```

For parameterized tests (future):
```
Namespace.ClassName.MethodName(param1,param2)
```

### State Property Mapping

| Jaribu Status | M.T.P. Property |
|---------------|-----------------|
| Passed | `PassedTestNodeStateProperty.CachedInstance` |
| Failed | `new FailedTestNodeStateProperty(exception)` |
| Skipped | `new SkippedTestNodeStateProperty(reason)` |
| Timeout | `new TimeoutTestNodeStateProperty(exception)` |
| Error | `new ErrorTestNodeStateProperty(exception)` |

### Full Implementation

```csharp
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TimeWarp.Jaribu.TestingPlatform;

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
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
        => Task.FromResult(new CreateTestSessionResult { IsSuccess = true });

    public async Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        try
        {
            var isDiscovery = context.Request is DiscoverTestExecutionRequest;
            var filter = GetFilter(context);

            foreach (var testClass in TestRunner.RegisteredTestClasses)
            {
                var testMethods = TestRunner.DiscoverTests(testClass);
                
                foreach (var method in testMethods)
                {
                    var testNodeUid = $"{testClass.FullName}.{method.Name}";
                    
                    if (filter != null && !MatchesFilter(testNodeUid, filter))
                        continue;

                    if (isDiscovery)
                    {
                        await PublishTestNode(context, testNodeUid, method.Name,
                            DiscoveredTestNodeStateProperty.CachedInstance);
                    }
                    else
                    {
                        // Report in-progress
                        await PublishTestNode(context, testNodeUid, method.Name,
                            InProgressTestNodeStateProperty.CachedInstance);

                        // Execute test
                        var result = await TestRunner.RunSingleTestAsync(testClass, method);

                        // Report result
                        var stateProperty = MapStatusToProperty(result);
                        await PublishTestNode(context, testNodeUid, method.Name, 
                            stateProperty, result.Duration);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            await ReportUnhandledException(context, ex);
            throw;
        }
        finally
        {
            context.Complete();
        }
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
        => Task.FromResult(new CloseTestSessionResult { IsSuccess = true });

    private TestNodeStateProperty MapStatusToProperty(TestResult result)
        => result.Status switch
        {
            TestStatus.Passed => PassedTestNodeStateProperty.CachedInstance,
            TestStatus.Skipped => new SkippedTestNodeStateProperty(result.SkipReason),
            TestStatus.Failed => new FailedTestNodeStateProperty(result.Exception),
            TestStatus.Timeout => new TimeoutTestNodeStateProperty(result.Exception),
            _ => new ErrorTestNodeStateProperty(result.Exception)
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
            var endTime = DateTimeOffset.UtcNow;
            var startTime = endTime - duration.Value;
            properties.Add(new TimingProperty(new TimingInfo(startTime, endTime, duration.Value)));
        }

        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: context.Request.Session.SessionUid,
            testNode: new TestNode
            {
                Uid = uid,
                DisplayName = displayName,
                Properties = properties
            }));
    }

    private async Task ReportUnhandledException(ExecuteRequestContext context, Exception ex)
    {
        await context.MessageBus.PublishAsync(this, new TestNodeUpdateMessage(
            sessionUid: context.Request.Session.SessionUid,
            testNode: new TestNode
            {
                Uid = $"Jaribu.Error.{Guid.NewGuid()}",
                DisplayName = $"Unhandled: {ex.GetType().Name}",
                Properties = new PropertyBag(new ErrorTestNodeStateProperty(ex))
            }));
    }

    private ITestExecutionFilter? GetFilter(ExecuteRequestContext context)
        => context.Request switch
        {
            RunTestExecutionRequest r => r.Filter,
            DiscoverTestExecutionRequest d => d.Filter,
            _ => null
        };

    private bool MatchesFilter(string testNodeUid, ITestExecutionFilter filter)
        => filter switch
        {
            TestNodeUidListFilter uidFilter => 
                uidFilter.TestNodeUids.Any(u => u.Value == testNodeUid),
            TreeNodeFilter treeFilter => 
                treeFilter.MatchesFilter(testNodeUid, new PropertyBag()),
            _ => true
        };
}
```

## Results

_Added after completion._
