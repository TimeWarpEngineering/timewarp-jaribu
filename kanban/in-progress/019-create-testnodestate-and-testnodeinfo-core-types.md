# Create TestNodeState and TestNodeInfo Core Types

## Summary

Create the foundational types that align Jaribu with MTP's TestNode concept. These types will be used throughout the core library and replace the existing duplicate result types.

Parent Epic: #018

## Todo List

- [x] Create `Source/TimeWarp.Jaribu/TestNodeState.cs` - enum with states: Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled
- [x] Create `Source/TimeWarp.Jaribu/TestNodeInfo.cs` - record with: Uid, DisplayName, State, Duration?, Exception?, Message?, Parameters?
- [x] Create `Source/TimeWarp.Jaribu/TestRunStats.cs` - record with: ClassName, StartTime, Duration, PassedCount, FailedCount, SkippedCount
- [x] Ensure all types follow coding conventions (2-space indent, PascalCase, explicit types, file-scoped namespaces)
- [x] Verify build succeeds

## Notes

### TestNodeState Mapping to MTP

| TestNodeState | MTP Property |
|---------------|--------------|
| Discovered | DiscoveredTestNodeStateProperty |
| InProgress | InProgressTestNodeStateProperty |
| Passed | PassedTestNodeStateProperty |
| Failed | FailedTestNodeStateProperty |
| Skipped | SkippedTestNodeStateProperty |
| Timeout | TimeoutTestNodeStateProperty |
| Error | ErrorTestNodeStateProperty |
| Cancelled | CancelledTestNodeStateProperty |

### TestNodeInfo Design

```csharp
public record TestNodeInfo
(
  string Uid,           // "Namespace.Class.Method"
  string DisplayName,   // "MethodName" or "MethodName(param1, param2)"
  TestNodeState State,
  TimeSpan? Duration = null,
  Exception? Exception = null,
  string? Message = null,
  IReadOnlyList<object?>? Parameters = null
);
```

## Results

Created three new foundational types in `Source/TimeWarp.Jaribu/`:

- **TestNodeState.cs** - Enum with 8 states aligned to MTP's TestNodeStateProperty types: Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled
- **TestNodeInfo.cs** - Positional record with Uid, DisplayName, State, and optional Duration, Exception, Message, Parameters
- **TestRunStats.cs** - Positional record with ClassName, StartTime, Duration, PassedCount, FailedCount, SkippedCount plus computed TotalTests and Success properties

Build verified: 0 warnings, 0 errors.
