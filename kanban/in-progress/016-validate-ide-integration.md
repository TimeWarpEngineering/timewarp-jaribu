# Validate IDE Integration

## Summary

Validate that the Microsoft.Testing.Platform integration works correctly with Visual Studio and VS Code Test Explorers. This includes test discovery, execution, filtering, and result display.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### VS Code
- [x] Open workspace in VS Code
- [x] Install C# Dev Kit extension (if not installed)
- [x] Verify Test Explorer panel shows tests
- [x] Run all tests from Test Explorer
- [x] Run single test from Test Explorer
- [ ] Verify CodeLens "Run Test" links work (if enabled)
- [x] Verify pass/fail status displays correctly
- [x] Verify failure details show in output

### Visual Studio (Optional - Windows only)
- [ ] Open solution in Visual Studio
- [ ] Verify Test Explorer shows Jaribu tests
- [ ] Run all tests from Test Explorer
- [ ] Run single test from Test Explorer
- [ ] Verify pass/fail icons display correctly

### JetBrains Rider (Optional)
- [ ] Open solution in Rider
- [ ] Verify Unit Tests panel shows tests
- [ ] Run tests from various entry points

### Cleanup Tasks (discovered during validation)
- [x] Fix CS9314 shebang error with `<Features>FileBasedProgram</Features>`
- [ ] Add `FileBasedProgram` to M.T.P. props for automatic support
- [ ] Remove obsolete cache clearing code (RunClean, CleanAttribute, etc.)
- [ ] Remove ci-tests orchestrator (replaced by M.T.P.)
- [ ] Update mtp-tests to include all jaribu-*.cs files
- [ ] Verify dual-mode execution works

## Notes

### Key Discovery: CS9314 Shebang Error in .NET 10

**.NET 10 introduced stricter validation** of `#!` shebang directives. Files with `#!` fail to compile in regular csproj with:
```
error CS9314: '#!' directives can be only used in scripts or file-based programs
```

**The fix**: Add the `FileBasedProgram` compiler feature flag:
```xml
<Features>$(Features);FileBasedProgram</Features>
```

This should be added to the M.T.P. props file so consumers automatically get shebang support for dual-mode test files.

### Dual-Mode Test Files

With the `FileBasedProgram` feature, the same test file can work in both modes:

| Mode | Command | Use Case |
|------|---------|----------|
| **Runfile** | `dotnet jaribu-03-tag-filtering.cs` | Run single test file |
| **M.T.P.** | `dotnet test Tests/.../mtp-tests/` | Run all tests, IDE integration |

### Obsolete Code to Remove

The following are no longer needed and should be removed:

1. **Cache clearing code**:
   - `CleanAttribute.cs`
   - `ClearRunfileCacheAttribute.cs`
   - `TestRunner.RunClean()` method
   - `TestHelpers.ClearRunfileCache()` method
   - `jaribu-05-cache-clearing.cs` test file

2. **ci-tests orchestrator**:
   - `ci-tests/run-ci-tests.cs`
   - `ci-tests/Directory.Build.props`
   
   Replaced by M.T.P. mode: `dotnet test Tests/.../mtp-tests/`

### VS Code Requirements

- VS Code with C# Dev Kit extension
- .NET 10 SDK installed
- Test Explorer panel visible

### Troubleshooting

#### Tests Not Appearing

1. Check `IsTestProject=true` in project
2. Verify `IsTestingPlatformApplication=true`
3. Check Output > Tests for errors
4. Try rebuilding the project

#### "No tests found" Message

1. Ensure `[ModuleInitializer]` is present
2. Ensure `RegisterTests<T>()` is called
3. Check test methods are `public static async Task`
4. Verify methods don't start with "Setup" or "CleanUp"

#### Shebang Error (CS9314)

Add to your csproj or import M.T.P. props that includes:
```xml
<Features>$(Features);FileBasedProgram</Features>
```

## Results

**Status: VS Code integration verified! ✅**

### VS Code Test Explorer
- Tests discovered and displayed correctly
- Run/debug from Test Explorer works
- Pass/fail status displays correctly
- Failure details show in output

### CLI Validation

```bash
$ dotnet test Tests/TimeWarp.Jaribu.Tests/mtp-tests/
  Failed! - Failed: 1, Passed: 14, Skipped: 0, Total: 15, Duration: 585ms
```

The one failure (`RunCleanSkipsSelfCleaning`) is expected - it tests runfile-specific behavior that doesn't apply in M.T.P. mode. This test will be removed as part of the cache clearing cleanup.

### Dual-Mode Verification

| Mode | Status | Command |
|------|--------|---------|
| Runfile (single) | ✅ Works | `dotnet jaribu-03-tag-filtering.cs` |
| Runfile (all) | ✅ Works | `dotnet run-ci-tests.cs` (to be removed) |
| M.T.P. (all) | ✅ Works | `dotnet test mtp-tests/` |
| VS Code | ✅ Works | Test Explorer |

### Key Learnings

1. **`<Features>FileBasedProgram</Features>`** is required for csproj to accept `#!` shebang
2. **Runfiles auto-pickup Directory.Build.props** - no need for `#:project` in test files
3. **Dual-mode execution** enables both quick single-file runs and full IDE integration
4. **Cache clearing is obsolete** - `dotnet clean` handles it, orchestrator pattern not needed

### Remaining Cleanup

See "Cleanup Tasks" in Todo List above. These should be done before marking this task complete.
