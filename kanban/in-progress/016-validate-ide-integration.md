# Validate IDE Integration

## Summary

Validate that the Microsoft.Testing.Platform integration works correctly with Visual Studio and VS Code Test Explorers. This includes test discovery, execution, filtering, and result display.

**Parent Epic**: 010 - Microsoft.Testing.Platform Integration

## Todo List

### Visual Studio
- [ ] Open solution in Visual Studio
- [ ] Verify Test Explorer shows Jaribu tests
- [ ] Run all tests from Test Explorer
- [ ] Run single test from Test Explorer
- [ ] Run tests by right-clicking test class
- [ ] Verify pass/fail icons display correctly
- [ ] Verify failure details show exception
- [ ] Verify timing information displays
- [ ] Test "Run Tests" from Solution Explorer context menu

### VS Code
- [ ] Open workspace in VS Code
- [ ] Install C# Dev Kit extension (if not installed)
- [ ] Verify Test Explorer panel shows tests
- [ ] Run all tests from Test Explorer
- [ ] Run single test from Test Explorer
- [ ] Verify CodeLens "Run Test" links work (if enabled)
- [ ] Verify pass/fail status displays correctly
- [ ] Verify failure details show in output

### JetBrains Rider (Optional)
- [ ] Open solution in Rider
- [ ] Verify Unit Tests panel shows tests
- [ ] Run tests from various entry points
- [ ] Verify results display correctly

### Debugging
- [ ] Set breakpoint in test method
- [ ] Debug test from IDE
- [ ] Verify breakpoint is hit
- [ ] Step through test code

### Edge Cases
- [ ] Verify discovery after adding new test
- [ ] Verify discovery after renaming test
- [ ] Verify behavior with large test suites
- [ ] Verify behavior with parallel tests (if implemented)

## Notes

### Visual Studio Requirements

- Visual Studio 2022 17.8+ (for M.T.P. support)
- .NET 9 SDK installed
- Test Explorer window open (Test > Test Explorer)

### VS Code Requirements

- VS Code with C# Dev Kit extension
- .NET 9 SDK installed
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

#### Debugging Not Working

1. Check `Debug` configuration is selected
2. Ensure PDB files are generated
3. Try "Debug Tests" instead of "Run Tests"

### Expected Test Explorer Display

```
📁 TimeWarp.Jaribu.TestingPlatform.Tests
  📁 BasicTests
    ✅ PassingTest (23ms)
    ❌ FailingTest (15ms)
    ⏭️ SkippedTest
    ⏱️ TimeoutTest (100ms)
  📁 MultiClassTests
    ✅ TestFromSecondClass
```

### What to Document

After validation, note:
- Any IDE-specific quirks or workarounds
- Minimum IDE versions required
- Performance observations (discovery time, etc.)
- Any missing features compared to other frameworks

## Results

**Status: VS Code integration verified! ✅**

### Automated Validation (CLI-based)

The following was verified via command-line, which is what IDEs call under the hood:

```bash
$ dotnet run --project Tests/TimeWarp.Jaribu.MtpValidation/
.NET Testing Platform v1.5.3 [linux-x64 - net10.0]

Test run summary: Failed! (expected - includes intentional failures)
  total: 16
  failed: 5
  succeeded: 9
  skipped: 2
  duration: 216ms
```

**Verified working:**
- [x] Test discovery works (16 tests found)
- [x] Test execution works
- [x] Pass/fail/skip/timeout states reported correctly
- [x] Exception details included in output
- [x] Timing information reported
- [x] Exit codes correct (non-zero for failures)

### Manual Testing Required

The following require human interaction with IDEs:

**Visual Studio (Windows):**
- [ ] Open solution and verify Test Explorer shows tests
- [ ] Run/debug from Test Explorer
- [ ] Verify pass/fail icons
- [ ] Test context menu options

**VS Code (Cross-platform):** ✅ VERIFIED
- [x] Install C# Dev Kit if needed
- [x] Verify Test Explorer panel shows tests
- [x] Run from Test Explorer
- [ ] Verify CodeLens works (if enabled)

**JetBrains Rider (Optional):**
- [ ] Unit Tests panel shows tests
- [ ] Run/debug works

### Prerequisites for IDE Testing

1. Ensure `IsTestProject=true` is set (it is)
2. Ensure `IsTestingPlatformApplication=true` is set (it is)
3. Build the project first: `dotnet build Tests/TimeWarp.Jaribu.MtpValidation/`
4. Open IDE and navigate to Test Explorer

### Known Limitations

- `--list-tests` runs tests instead of listing (discovery mode enhancement needed)
- `--filter` support incomplete (filters not applied)

These are framework enhancements for a future iteration.
