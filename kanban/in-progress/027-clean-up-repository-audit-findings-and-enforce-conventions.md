# Clean up repository audit findings and enforce conventions

## Description

Clean up repository to match conventions used in other TimeWarp projects (timewarp-terminal, timewarp-nuru, timewarp-ganda). Remove LocalNuGetCache, update packages, and fix all `ganda repo audit` findings.

## Checklist

### LocalNuGetCache Removal
- [x] Remove `<LocalNuGetCache>` property from `Directory.Build.props`
- [x] Remove `<RestorePackagesPath>` from `Directory.Build.props`
- [x] Add `LocalNuGetCache/` to `.gitignore` (already present)
- [x] Delete `LocalNuGetCache/` directory

### NuGet Package Updates
- [x] Run `dotnet list --outdated` to identify outdated packages
- [x] Update outdated packages in `Directory.Packages.props` (all packages already up to date)
- [x] Remove orphaned package references (removed 30 unused packages)

### MSBuild Structure
- [x] Move `IsPackable=false` to root `Directory.Build.props`
- [x] Move `GeneratePackageOnBuild=true` to `source/Directory.Build.props`
- [x] Move package metadata to `source/Directory.Build.props`
- [x] Rename `Source/` to `source/` (kebab-case)

### Repository Audit
- [x] Run `ganda repo audit` and capture all findings
- [x] Fix all audit check failures
- [x] Re-run `ganda repo audit` to verify all 10 checks pass

### Build Verification
- [x] Run `dotnet build` - verify 0 warnings, 0 errors ✅
- [ ] Run tests - blocked by MTP/.NET 10 SDK compatibility issue

## Notes

This follows the same cleanup pattern done in timewarp-amuru (task 074).

Key files to modify:
- `Directory.Build.props` - Remove LocalNuGetCache settings
- `Directory.Packages.props` - Update package versions
- `.gitignore` - Add LocalNuGetCache pattern

The LocalNuGetCache pattern is inconsistent with other TimeWarp projects which use the global NuGet cache (`~/.nuget/packages/`).

## Additional Changes

- Renamed `README.md` to `readme.md` (kebab-case)
- Replaced `System.Console` with `TimeWarp.Terminal.Terminal` in tests
- Removed `<Using Include="System.Console" Static="true" />` from Tests/Directory.Build.props
- Added `<Using Include="TimeWarp.Terminal.Terminal" Static="true" />` instead
