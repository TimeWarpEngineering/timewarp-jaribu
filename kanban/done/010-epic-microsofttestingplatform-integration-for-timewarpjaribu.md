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
| 011 | Refactor TestRunner Core for M.T.P. Compatibility | High | ✅ done |
| 012 | Create TimeWarp.Jaribu.TestingPlatform Project | High | ✅ done |
| 013 | Implement JaribuTestFramework (ITestFramework) | High | ✅ done |
| 014 | Add MSBuild Integration (.props/.targets) | High | ✅ done |
| 015 | Create M.T.P. Test Suite | Medium | ✅ done |
| 016 | Validate IDE Integration | Medium | 🔄 in-progress (manual) |
| 017 | Update Documentation | Low | ✅ done |

## Todo List

- [x] Task 011: Refactor TestRunner Core
- [x] Task 012: Create TestingPlatform Project
- [x] Task 013: Implement JaribuTestFramework
- [x] Task 014: Add MSBuild Integration
- [x] Task 015: Create M.T.P. Test Suite
- [ ] Task 016: Validate IDE Integration (requires manual testing)
- [x] Task 017: Update Documentation

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

**Completed: 2025-12-23**

### What Was Delivered

1. **New Package**: `TimeWarp.Jaribu.TestingPlatform`
   - Implements `ITestFramework` for M.T.P. integration
   - MSBuild props for automatic configuration
   - Works with `dotnet test` command

2. **Core Enhancements**:
   - `TestRunner.DiscoverTests(Type)` - public API for test discovery
   - `TestRunner.RunSingleTestAsync(Type, MethodInfo)` - public API for single test execution
   - `MtpTestResult` record with full test metadata
   - `TestStatus` enum (Passed, Failed, Skipped, Timeout, Error)

3. **Test Suite**: 16 tests validating all functionality
   - 9 passing, 5 intentional failures, 2 skipped
   - Covers all test states and edge cases

4. **Documentation**: Updated README with both execution modes

### Validation Results

```bash
$ dotnet test Tests/TimeWarp.Jaribu.MtpValidation/
  Failed! - Failed: 5, Passed: 9, Skipped: 2, Total: 16

$ dotnet run --project Tests/TimeWarp.Jaribu.MtpValidation/
  .NET Testing Platform v1.5.3
  Test run summary: Failed! (expected)
    total: 16, failed: 5, succeeded: 9, skipped: 2
```

### Known Limitations

- `--list-tests` runs tests instead of just listing (discovery mode enhancement needed)
- `--filter` support incomplete (future enhancement)

### Key Discovery: Dual-Mode Test Files

**.NET 10 CS9314 Error**: Files with `#!` shebang fail to compile in regular csproj:
```
error CS9314: '#!' directives can be only used in scripts or file-based programs
```

**Solution**: Add compiler feature flag to M.T.P. props:
```xml
<Features>$(Features);FileBasedProgram</Features>
```

This enables **dual-mode test files** - same `.cs` file works as:
- **Runfile**: `dotnet jaribu-03-tag-filtering.cs` (single file)
- **M.T.P.**: `dotnet test mtp-tests/` (all tests, IDE integration)

### Cleanup Completed (Task 016)

Obsolete code removed:
- `CleanAttribute`, `ClearRunfileCacheAttribute` - no longer needed
- `TestRunner.RunClean()`, `TestHelpers.ClearRunfileCache()` - replaced by `dotnet clean`
- `ci-tests/` orchestrator - replaced by M.T.P. mode

### Commits

- `40c162a` feat: add M.T.P.-compatible APIs to TestRunner
- `9b29801` feat: create TimeWarp.Jaribu.TestingPlatform project
- `a4b1d7e` feat: implement JaribuTestFramework for M.T.P.
- `e1a5e38` feat: enhance MSBuild props and add validation tests
- `f8a3c21` feat: expand M.T.P. test suite with comprehensive tests
- `7d2e4b3` docs: update README with M.T.P. mode documentation
- (pending) refactor: remove cache clearing, enable dual-mode test files
