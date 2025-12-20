# Update Jaribu to use TimeWarp.Terminal

## Description

Update TimeWarp.Jaribu to use the new TimeWarp.Terminal project/namespace instead of the ITerminal interface that was previously in TimeWarp.Nuru. This is a dependency extraction - ITerminal and related terminal abstractions are being moved from Nuru to a dedicated TimeWarp.Terminal project.

**Investigation confirmed:** Jaribu does NOT use any Nuru CLI framework features (routes, NuruApp, etc.) - only terminal types that are moving to TimeWarp.Terminal. Therefore, we can **remove** the Nuru dependency entirely and replace it with just TimeWarp.Terminal.

## Checklist

- [x] Replace TimeWarp.Nuru PackageReference with TimeWarp.Terminal ProjectReference in `Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj`
  - Path: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-terminal/timewarp-terminal.csproj`
- [x] Update namespace import in `Source/TimeWarp.Jaribu/TestHelpers.cs` from `TimeWarp.Nuru` to `TimeWarp.Terminal`
- [x] Verify build succeeds
- [x] Verify all tests pass
- [ ] Once TimeWarp.Terminal is published to NuGet, convert ProjectReference to PackageReference

## Notes

**Context:**
- ITerminal interface is being extracted from TimeWarp.Nuru into a separate TimeWarp.Terminal project
- Jaribu only uses terminal types (ITerminal, NuruTerminal, Table, color extensions) - none of the CLI routing framework
- This simplifies Jaribu's dependencies - no Nuru reference needed at all

**Types used from TimeWarp.Terminal:**
- `ITerminal`, `NuruTerminal`
- `Table`, `Alignment`, `BorderStyle`
- Color extensions: `.Green()`, `.Red()`, `.Yellow()`, `.Bold()`
- `WriteTable()` extension method

**Local Project Path:**
```
/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-terminal/timewarp-terminal.csproj
```

**Files to modify:**
1. `Source/TimeWarp.Jaribu/TimeWarp.Jaribu.csproj` - replace PackageReference
2. `Source/TimeWarp.Jaribu/TestHelpers.cs` - update using statement

**Namespace Change:**
- Old: `using TimeWarp.Nuru;`
- New: `using TimeWarp.Terminal;`

**Note:** Scripts and Tests directories still reference Nuru for their own purposes (CLI scripts) - that's separate from the Jaribu library itself.
