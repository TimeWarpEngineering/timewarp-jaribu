# Add TimeWarp.Nuru Dependency to Jaribu

## Summary

Add TimeWarp.Nuru package reference to TimeWarp.Jaribu to enable use of `Table` widget, `ITerminal`, and color extensions for formatted test output.

## Todo List

- [ ] Update TimeWarp.Nuru version in `Directory.Packages.props` to latest
- [ ] Add `<PackageReference Include="TimeWarp.Nuru" />` to `TimeWarp.Jaribu.csproj`
- [ ] Verify build succeeds
- [ ] Run tests to confirm no regressions

## Notes

Nuru is already referenced in:
- `Directory.Packages.props` (version 2.1.0-beta.12 - outdated)
- `Tests/Directory.Build.props`
- `Scripts/Directory.Build.props`

Jaribu only has `Shouldly` as a dependency. Adding Nuru enables:
- `Table` widget for tabular test results (task 002)
- `ITerminal`/`TestTerminal` for testable output
- Color extensions (`.Green()`, `.Red()`, `.Yellow()`)
- `Panel` and `Rule` widgets for future formatting

Future consideration: Nuru could split out `Nuru.Terminal` as a lightweight standalone package separate from CLI routing/parsing. This would reduce dependency footprint for projects only needing terminal output features.
