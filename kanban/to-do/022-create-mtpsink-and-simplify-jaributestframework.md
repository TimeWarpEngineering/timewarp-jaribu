# Create MtpSink and Simplify JaribuTestFramework

## Summary

Create an MTP-specific sink implementation that translates TestNodeInfo to MTP's TestNodeUpdateMessage, then simplify JaribuTestFramework to use this sink instead of inline translation logic.

Parent Epic: #018
Depends On: #019, #020, #021

## Todo List

- [ ] Create `Source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs`
- [ ] Implement state mapping: `TestNodeState` → `TestNodeStateProperty` subclasses
- [ ] Implement `PublishNodeAsync` to create and publish `TestNodeUpdateMessage`
- [ ] Refactor `JaribuTestFramework.ExecuteRequestAsync` to use `MtpSink`
- [ ] Remove duplicate `MapStatusToProperty` method from JaribuTestFramework
- [ ] Remove duplicate `PublishTestNode` method from JaribuTestFramework
- [ ] Verify `dotnet test` still discovers and runs tests
- [ ] Verify VS/Rider test explorer integration still works

## Notes

### MtpSink State Mapping

```csharp
private static TestNodeStateProperty MapStateToProperty(TestNodeInfo node)
  => node.State switch
  {
    TestNodeState.Discovered => DiscoveredTestNodeStateProperty.CachedInstance,
    TestNodeState.InProgress => InProgressTestNodeStateProperty.CachedInstance,
    TestNodeState.Passed => PassedTestNodeStateProperty.CachedInstance,
    TestNodeState.Skipped => new SkippedTestNodeStateProperty(node.Message),
    TestNodeState.Failed => new FailedTestNodeStateProperty(node.Exception ?? ...),
    TestNodeState.Timeout => new TimeoutTestNodeStateProperty(node.Exception ?? ...),
    TestNodeState.Error => new ErrorTestNodeStateProperty(node.Exception ?? ...),
    TestNodeState.Cancelled => new CancelledTestNodeStateProperty(node.Message),
    _ => ...
  };
```

### Simplified JaribuTestFramework

Before: ~160 lines with inline translation
After: ~80 lines delegating to MtpSink

## Results

_Added after completion_
