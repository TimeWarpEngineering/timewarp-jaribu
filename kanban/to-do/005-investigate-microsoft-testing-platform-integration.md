# Investigate Microsoft.Testing.Platform Integration

## Summary

Research and evaluate integration possibilities between TimeWarp.Jaribu and Microsoft.Testing.Platform, the lightweight and portable alternative to VSTest for running tests.

## Todo List

- [ ] Review Microsoft.Testing.Platform architecture and extensibility points
- [ ] Evaluate compatibility with Jaribu's runfile-based test execution model
- [ ] Investigate how to register Jaribu as a test framework with the platform
- [ ] Research the compile-time extension registration mechanism
- [ ] Assess benefits: Test Explorer integration (VS/VS Code), CI pipeline support, `dotnet test` compatibility
- [ ] Identify gaps or limitations for runfile-based testing scenarios
- [ ] Document findings and recommend integration approach (or reasons to defer)

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
- TUnit (built entirely on Microsoft.Testing.Platform)

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

### Reference
- https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro
- GitHub: https://github.com/microsoft/testfx/tree/main/src/Platform/Microsoft.Testing.Platform
