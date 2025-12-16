# Replace Folder Deletion with dotnet clean for Runfile Cache Clearing

## Summary

Replace the existing `[ClearRunfileCache]` attribute's manual folder deletion approach with the new `dotnet clean <runfile>` command. Create a new `[Clean]` attribute and refactor `[ClearRunfileCache]` to share the same underlying clean method.

## Todo List

- [x] Create new `[Clean]` attribute that executes `dotnet clean <runfile>`
- [x] Implement shared clean method that both attributes will use
- [x] Refactor existing `[ClearRunfileCache]` attribute to use the new shared clean method
- [x] Remove manual folder deletion logic in favor of `dotnet clean`
- [x] Update tests to verify both attributes work correctly
- [x] Verify backward compatibility for existing `[ClearRunfileCache]` usage

## Notes

The `dotnet clean <runfile>` command is the official .NET 10 way to clear the compiled runfile cache. This is more reliable and maintainable than manually deleting folders, as it uses the SDK's built-in knowledge of cache locations.

Both `[Clean]` and `[ClearRunfileCache]` should share a common underlying implementation to avoid code duplication. The `[Clean]` attribute is the new preferred name aligned with the `dotnet clean` command, while `[ClearRunfileCache]` maintains backward compatibility with existing code.

### Implementation Details

**Key insight:** A runfile cannot clean itself while executing - that would corrupt the running process. The `[Clean]` and `[ClearRunfileCache]` attributes are **markers for orchestrators** that spawn test runfiles as subprocesses.

**What was implemented:**

1. **`CleanAttribute.cs`** - New marker attribute with documentation explaining it's for orchestrators
2. **`ClearRunfileCacheAttribute.cs`** - Updated documentation for backward compatibility, same semantics as `[Clean]`
3. **`TestRunner.RunClean(string? path)`** - Public method that:
   - Executes `dotnet clean <runfile>` using `TimeWarp.Amuru.Shell.Builder`
   - Gracefully skips self-cleaning with helpful warning message
   - Handles non-existent paths gracefully
4. **`run-all-tests.cs`** - Added `--clean` flag that runs `dotnet clean` on each test file before executing it
5. **`jaribu-05-cache-clearing.cs`** - Updated tests to verify attribute existence and `RunClean` behavior

**Usage patterns:**

```bash
# Manual clean before running a test
dotnet clean mytest.cs && dotnet mytest.cs

# Use orchestrator with --clean flag
dotnet Tests/Scripts/run-all-tests.cs --clean
```

**Per AGENTS.md requirements:**
- Uses `TimeWarp.Amuru.Shell.Builder()` for process execution, NOT `System.Diagnostics.Process.Start`
