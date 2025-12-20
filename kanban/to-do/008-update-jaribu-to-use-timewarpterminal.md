# Update Jaribu to use TimeWarp.Terminal

## Description

Update TimeWarp.Jaribu to use the new TimeWarp.Terminal project/namespace instead of the ITerminal interface that was previously in TimeWarp.Nuru. This is a dependency extraction - ITerminal and related terminal abstractions are being moved from Nuru to a dedicated TimeWarp.Terminal project.

## Checklist

- [ ] Change TimeWarp.Nuru from PackageReference to ProjectReference (until new version published)
  - Path: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-nuru/timewarp-nuru.csproj`
- [ ] Add ProjectReference to TimeWarp.Terminal (use local path until published)
  - Path: `/home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-terminal/timewarp-terminal.csproj`
- [ ] Update namespace imports from `TimeWarp.Nuru` to `TimeWarp.Terminal` for ITerminal usage
- [ ] Update any code referencing terminal-related types to use the new namespace
- [ ] Verify all tests pass with the new dependencies
- [ ] Once TimeWarp.Nuru and TimeWarp.Terminal are published to NuGet, convert ProjectReferences back to PackageReferences

## Notes

**Context:**
- ITerminal interface is being extracted from TimeWarp.Nuru into a separate TimeWarp.Terminal project
- This allows for better separation of concerns and independent versioning
- Using ProjectReferences temporarily until both TimeWarp.Nuru and TimeWarp.Terminal are published to NuGet
- The Nuru package also needs to be a ProjectReference since it depends on the Terminal changes

**Local Project Paths:**
```
TimeWarp.Nuru: /home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-nuru/timewarp-nuru.csproj
TimeWarp.Terminal: /home/steventcramer/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/Cramer-2025-08-30-dev/source/timewarp-terminal/timewarp-terminal.csproj
```

**Namespace Change:**
- Old: `TimeWarp.Nuru` (for ITerminal)
- New: `TimeWarp.Terminal`
