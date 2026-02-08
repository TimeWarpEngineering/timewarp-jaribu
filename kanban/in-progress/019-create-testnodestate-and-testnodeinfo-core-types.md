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

## Implementation Plan

### Phase 1: Create Core Types

Create three new files in `Source/TimeWarp.Jaribu/`:

1. **TestNodeState.cs** - Enum with 8 states:
   - Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled
   - Aligns with MTP's TestNodeStateProperty types

2. **TestNodeInfo.cs** - Positional record:
   - string Uid ("Namespace.Class.Method")
   - string DisplayName
   - TestNodeState State
   - TimeSpan? Duration
   - Exception? Exception
   - string? Message
   - IReadOnlyList<object?>? Parameters

3. **TestRunStats.cs** - Positional record:
   - string ClassName
   - DateTimeOffset StartTime
   - TimeSpan Duration
   - int PassedCount
   - int FailedCount
   - int SkippedCount
   - Plus computed: TotalTests, Success

### Coding Conventions
- 2-space indentation
- File-scoped namespaces
- Explicit types (no var)
- PascalCase for all public members

### Build Verification
- Must compile with 0 warnings, 0 errors
- Verify no naming conflicts with existing types

## Results

Created three new foundational types in `Source/TimeWarp.Jaribu/`:

- **TestNodeState.cs** - Enum with 8 states aligned to MTP's TestNodeStateProperty types: Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, Cancelled
- **TestNodeInfo.cs** - Positional record with Uid, DisplayName, State, and optional Duration, Exception, Message, Parameters
- **TestRunStats.cs** - Positional record with ClassName, StartTime, Duration, PassedCount, FailedCount, SkippedCount plus computed TotalTests and Success properties

Build verified: 0 warnings, 0 errors.
