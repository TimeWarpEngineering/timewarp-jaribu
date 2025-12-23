# Epic: Microsoft.Testing.Platform Integration for TimeWarp.Jaribu

## Summary

Add side-by-side support for Microsoft.Testing.Platform (M.T.P.) alongside the existing Nuru-based runfile mode. This enables IDE Test Explorer integration, `dotnet test` compatibility, and standard CI pipeline support while preserving the lightweight runfile workflow.

## Architecture Overview

```
TimeWarp.Jaribu/
├── Core (shared)
│   ├── TestRunner.cs          # Discovery + execution logic
│   ├── TestResult.cs          # Structured results
│   ├── Attributes/            # [TestTag], [Skip], [Timeout], etc.
│   └── RegisterTests<T>()     # Registration API
│
├── Nuru Mode (current - unchanged)
│   ├── RunAllTests()          # Entry point for runfiles
│   └── Console output         # Tabular results
│
└── M.T.P. Mode (new)
    ├── JaribuTestFramework    # Implements ITestFramework
    ├── JaribuCapabilities     # Implements ITestFrameworkCapabilities
    └── MSBuild integration    # .props/.targets for dotnet test
```

## Benefits

- **IDE Integration**: Visual Studio and VS Code Test Explorer support
- **Standard Workflow**: `dotnet test` compatibility
- **CI Pipelines**: TRX reports, exit codes, standard tooling
- **Filtering**: `--filter` support for running subsets of tests
- **Discovery**: `--list-tests` for test enumeration
- **Backward Compatible**: Existing runfile mode unchanged

## Sub-Tasks

| Task ID | Title | Priority | Status |
|---------|-------|----------|--------|
| 011 | Refactor TestRunner Core for M.T.P. Compatibility | High | pending |
| 012 | Create TimeWarp.Jaribu.TestingPlatform Project | High | pending |
| 013 | Implement JaribuTestFramework (ITestFramework) | High | pending |
| 014 | Add MSBuild Integration (.props/.targets) | High | pending |
| 015 | Create M.T.P. Test Suite | Medium | pending |
| 016 | Validate IDE Integration | Medium | pending |
| 017 | Update Documentation | Low | pending |

## Todo List

- [ ] Task 011: Refactor TestRunner Core
- [ ] Task 012: Create TestingPlatform Project
- [ ] Task 013: Implement JaribuTestFramework
- [ ] Task 014: Add MSBuild Integration
- [ ] Task 015: Create M.T.P. Test Suite
- [ ] Task 016: Validate IDE Integration
- [ ] Task 017: Update Documentation

## Design Decisions

### Package Structure
**Decision**: Two separate packages
- `TimeWarp.Jaribu` - Core + Nuru mode (existing)
- `TimeWarp.Jaribu.TestingPlatform` - M.T.P. adapter (new)

**Rationale**: Clean separation of concerns. Users who only want runfile mode don't need M.T.P. dependencies.

### Core API Visibility
**Decision**: Make discovery/execution methods public in core
- `TestRunner.DiscoverTests(Type testClass)` - returns test methods
- `TestRunner.RunSingleTestAsync(Type testClass, MethodInfo method)` - runs one test
- `TestRunner.RegisteredTestClasses` - already exists

**Rationale**: `InternalsVisibleTo` adds complexity; these are legitimate public APIs.

### Naming
**Decision**: `TimeWarp.Jaribu.TestingPlatform`

**Rationale**: Clear, descriptive, follows .NET conventions (e.g., `Microsoft.Testing.Platform`).

## Notes

### Research Summary

M.T.P. is Microsoft's modern replacement for VSTest:
- Test projects compile to executables that run themselves
- No external test runner needed
- Native AOT compatible
- IDE integration via `--list-tests` and `--filter` CLI flags

### Key M.T.P. Interfaces

```csharp
ITestFramework
├── CreateTestSessionAsync()     // Session setup
├── ExecuteRequestAsync()        // Handle discovery/run requests
└── CloseTestSessionAsync()      // Session cleanup

IDataProducer
└── DataTypesProduced            // Returns typeof(TestNodeUpdateMessage)
```

### Test Node States

```csharp
DiscoveredTestNodeStateProperty  // Discovery mode
InProgressTestNodeStateProperty  // Test starting
PassedTestNodeStateProperty      // Success
FailedTestNodeStateProperty      // Failure with exception
SkippedTestNodeStateProperty     // Skipped with reason
TimeoutTestNodeStateProperty     // Timeout
ErrorTestNodeStateProperty       // Infrastructure error
```

### Reference Implementations
- TUnit: https://github.com/thomhurst/TUnit/tree/main/TUnit.Engine
- MSTest: https://github.com/microsoft/testfx

## Results

_Added after completion._
