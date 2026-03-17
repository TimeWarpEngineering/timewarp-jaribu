# Clean up repository audit findings and enforce conventions

## Description

Clean up repository to match conventions used in other TimeWarp projects (timewarp-terminal, timewarp-nuru, timewarp-ganda). Remove LocalNuGetCache, update packages, and fix all `ganda repo audit` findings.

## Checklist

### LocalNuGetCache Removal
- [ ] Remove `<LocalNuGetCache>` property from `Directory.Build.props`
- [ ] Remove `<RestorePackagesPath>` from `Directory.Build.props`
- [ ] Add `LocalNuGetCache/` to `.gitignore`
- [ ] Delete `LocalNuGetCache/` directory

### NuGet Package Updates
- [ ] Run `dotnet list --outdated` to identify outdated packages
- [ ] Update outdated packages in `Directory.Packages.props`
- [ ] Remove any unused package references

### Repository Audit
- [ ] Run `ganda repo audit` and capture all findings
- [ ] Fix all audit check failures
- [ ] Re-run `ganda repo audit` to verify all 9 checks pass

### Build Verification
- [ ] Run `dotnet build` - verify 0 warnings, 0 errors
- [ ] Run tests - verify all pass

## Notes

This follows the same cleanup pattern done in timewarp-amuru (task 074).

Key files to modify:
- `Directory.Build.props` - Remove LocalNuGetCache settings
- `Directory.Packages.props` - Update package versions
- `.gitignore` - Add LocalNuGetCache pattern

The LocalNuGetCache pattern is inconsistent with other TimeWarp projects which use the global NuGet cache (`~/.nuget/packages/`).
