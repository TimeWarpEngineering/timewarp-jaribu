# Update TimeWarp.Terminal from 1.0.0-beta.2 to 1.0.0-beta.4

## Description

Update the TimeWarp.Terminal NuGet package dependency from version 1.0.0-beta.2 to 1.0.0-beta.4 across all projects in the solution.

## Checklist

- [ ] Identify all projects referencing TimeWarp.Terminal
- [ ] Update version in Directory.Packages.props
- [ ] Update version in all project files if not using Central Package Versioning
- [ ] Run `dotnet restore` to verify new version is resolved
- [ ] Build the solution to ensure compatibility
- [ ] Run tests to verify no regressions

## Notes

This is a minor version update within the same beta channel. Review the release notes for 1.0.0-beta.3 and 1.0.0-beta.4 to identify any breaking changes or new features that may require attention.
