# Microsoft.Testing.Platform Integration

## Summary

Integrate TimeWarp.Jaribu with Microsoft.Testing.Platform for Test Explorer support, `dotnet test` compatibility, and potentially source-generated test discovery. This is the comprehensive integration task - see task 006 for the simpler immediate solution.

## Todo List

### Research Phase
- [ ] Review Microsoft.Testing.Platform adapter requirements
- [ ] Study TUnit's implementation (built entirely on M.T.P.) as reference
- [ ] Evaluate source generators for compile-time test discovery
- [ ] Assess compatibility with runfile-based execution model
- [ ] Identify if M.T.P. can work with single-file .cs programs or only assemblies

### Implementation Phase (if feasible)
- [ ] Create Jaribu test framework adapter for M.T.P.
- [ ] Implement source generator for test registration (zero reflection)
- [ ] Add `Microsoft.Testing.Platform.MSBuild` integration
- [ ] Implement `--list-tests` and `--filter` command line options
- [ ] Add Test Explorer metadata for VS/VS Code integration

### Validation Phase
- [ ] Verify `dotnet test` compatibility
- [ ] Test Visual Studio Test Explorer integration
- [ ] Test VS Code Test Explorer integration
- [ ] Validate Native AOT compatibility
- [ ] Performance comparison with current approach

## Notes

### What is Microsoft.Testing.Platform?

A lightweight, portable alternative to VSTest built on these pillars:
- **Determinism**: No reflection or dynamic runtime features for test coordination
- **Runtime transparency**: No AppDomain or AssemblyLoadContext interference
- **Compile-time registration**: Extensions registered at compile-time
- **Zero dependencies**: Core is single assembly `Microsoft.Testing.Platform.dll`
- **Hostable**: Can be hosted in any .NET application
- **All .NET form factors**: Supports Native AOT
- **Single module deploy**: One compilation result for all extensibility points

### Supported Frameworks
- MSTest (via MSTest runner)
- NUnit (via NUnit runner)  
- xUnit.net (via xUnit.net runner)
- TUnit (built entirely on Microsoft.Testing.Platform) - **best reference for Jaribu**

### Key Integration Points
- Tests run as executables directly (no vstest.console needed)
- `dotnet test` compatibility via `Microsoft.Testing.Platform.MSBuild` package
- Visual Studio and VS Code Test Explorer integration
- Exit codes for CI integration
- `--list-tests`, `--filter`, and other standard options

### Potential Benefits for Jaribu
- IDE Test Explorer integration without additional tooling
- Standard `dotnet test` workflow support
- CI pipeline compatibility
- Structured test result reporting
- Native AOT support alignment
- Source-generated test discovery (zero reflection)

### TUnit as Reference

TUnit is built entirely on M.T.P. and uses:
- **Source generators** for compile-time test discovery
- **Dual-mode execution**: Source generation (default) or reflection mode
- **Async-first assertions**
- **Parallel by default**

Key insight: TUnit's philosophy aligns well with Jaribu's goals. Study their adapter implementation.

### Open Questions

1. **Runfile compatibility**: Can M.T.P. work with single-file .cs programs, or does it require compiled assemblies?
2. **Source generators in runfiles**: Do Roslyn source generators work correctly in the runfile compilation context?
3. **Effort vs. benefit**: Is full M.T.P. integration worth it for Jaribu's lightweight use case?

### Relationship to Task 006

Task 006 provides a simple `RegisterTests<T>()` + `RunAllTests()` API as an immediate solution. This task (005) is the comprehensive approach that may supersede 006's implementation, or 006's API could be kept as a convenience layer on top of M.T.P.

### Reference
- https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro
- https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-extensions
- https://tunit.dev/docs/guides/philosophy
- GitHub: https://github.com/microsoft/testfx/tree/main/src/Platform/Microsoft.Testing.Platform
- TUnit GitHub: https://github.com/thomhurst/TUnit
