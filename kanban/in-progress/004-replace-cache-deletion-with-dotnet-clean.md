# Replace Folder Deletion with dotnet clean for Runfile Cache Clearing

## Summary

Replace the existing `[ClearRunfileCache]` attribute's manual folder deletion approach with the new `dotnet clean <runfile>` command. Create a new `[Clean]` attribute and refactor `[ClearRunfileCache]` to share the same underlying clean method.

## Todo List

- [ ] Create new `[Clean]` attribute that executes `dotnet clean <runfile>`
- [ ] Implement shared clean method that both attributes will use
- [ ] Refactor existing `[ClearRunfileCache]` attribute to use the new shared clean method
- [ ] Remove manual folder deletion logic in favor of `dotnet clean`
- [ ] Update tests to verify both attributes work correctly
- [ ] Verify backward compatibility for existing `[ClearRunfileCache]` usage

## Notes

The `dotnet clean <runfile>` command is the official .NET 10 way to clear the compiled runfile cache. This is more reliable and maintainable than manually deleting folders, as it uses the SDK's built-in knowledge of cache locations.

Both `[Clean]` and `[ClearRunfileCache]` should share a common underlying implementation to avoid code duplication. The `[Clean]` attribute is the new preferred name aligned with the `dotnet clean` command, while `[ClearRunfileCache]` maintains backward compatibility with existing code.

### Implementation Details (discovered during analysis)

**Current implementation location:**
- `ClearRunfileCacheAttribute.cs` - Simple marker attribute with `Enabled` property (lines 1-12)
- `TestRunner.cs` - Contains `ClearRunfileCache()` method (lines 393-435) that:
  - Manually deletes folders in `~/.local/share/dotnet/runfile/`
  - Skips the currently executing test's directory
  - Uses `Directory.Delete(cacheDir, recursive: true)`

**Key changes needed:**
1. Create `CleanAttribute.cs` - New attribute aligned with `dotnet clean` naming
2. Add shared `RunClean()` method in `TestRunner.cs` that uses `TimeWarp.Amuru.Shell.Builder` to execute `dotnet clean <runfile>`
3. Update `ClearRunfileCacheAttribute` to be a simple alias/wrapper for the new `Clean` functionality
4. Remove the manual `Directory.Delete` logic from `ClearRunfileCache()` method

**Per AGENTS.md requirements:**
- Must use `TimeWarp.Amuru.Shell.Builder()` for process execution, NOT `System.Diagnostics.Process.Start`
