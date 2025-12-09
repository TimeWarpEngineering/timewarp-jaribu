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
