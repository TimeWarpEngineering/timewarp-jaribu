# Create MtpSink and Simplify JaribuTestFramework

## Summary

Create an MTP-specific sink implementation that translates TestNodeInfo to MTP's TestNodeUpdateMessage, then simplify JaribuTestFramework to use this sink instead of inline translation logic.

Parent Epic: #018
Depends On: #019, #020, #021

## Todo List

- [ ] Create `source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs`
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

### Summary

Created MtpSink and simplified JaribuTestFramework by extracting all inline MTP translation logic into the new sink.

### Files Created

1. **MtpSink.cs** (108 lines) - `source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs`
   - Implements ITestResultSink for MTP IMessageBus integration
   - Translates all 8 TestNodeState values to MTP TestNodeStateProperty subtypes
   - Publishes TestNodeUpdateMessage for discovered, started, and completed events
   - Adds TimingProperty when duration is available
   - OnRunStartedAsync and OnRunCompletedAsync are no-ops (MTP doesn't need run-level events)

### Files Modified

2. **JaribuTestFramework.cs** - Simplified from 198 to 124 lines (-74 lines)
   - Removed inline MapStateToProperty method (moved to MtpSink)
   - Removed inline PublishTestNode method (replaced by MtpSink.PublishNodeAsync)
   - ExecuteRequestAsync now creates MtpSink and delegates:
     - Discovery: iterates methods, calls sink.OnTestDiscoveredAsync
     - Execution: delegates to TestRunner.RunTestsAsync(testClass, sink)

3. **TimeWarp.Jaribu.TestingPlatform.csproj** - Added 2 Using directives for MtpSink field types

### Decisions Made

- MtpSink handles all 8 TestNodeState values (more comprehensive than original 4-state mapping)
- Cancelled state maps to ErrorTestNodeStateProperty with OperationCanceledException
- MtpSink is internal sealed (not part of public API)

### Build Status

- ✅ TimeWarp.Jaribu: 0 errors, 0 warnings
- ✅ TimeWarp.Jaribu.TestingPlatform: 0 errors, 0 warnings
