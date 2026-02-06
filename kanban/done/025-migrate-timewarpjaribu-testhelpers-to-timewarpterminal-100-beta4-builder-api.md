# Migrate TimeWarp.Jaribu TestHelpers to TimeWarp.Terminal 1.0.0-beta.4 builder API

## Description

The Table API in TimeWarp.Terminal 1.0.0-beta.4 has breaking changes. TestHelpers.cs uses the old API with parameterless Table constructor and direct AddRow calls. Need to migrate to builder pattern using TableBuilder.

## Checklist

- [ ] Review TestHelpers.cs for current Table API usage
- [ ] Migrate Table constructor calls to TableBuilder pattern
- [ ] Replace AddRow method calls with builder pattern
- [ ] Verify build succeeds after migration
- [ ] Run tests to verify no regressions

## Notes

Build errors:
- /Source/TimeWarp.Jaribu/TestHelpers.cs:52 - Table no longer has parameterless constructor
- /Source/TimeWarp.Jaribu/TestHelpers.cs:86 - AddRow method no longer exists on Table
- /Source/TimeWarp.Jaribu/TestHelpers.cs:118 - Table constructor issue
- /Source/TimeWarp.Jaribu/TestHelpers.cs:139 - AddRow method issue
