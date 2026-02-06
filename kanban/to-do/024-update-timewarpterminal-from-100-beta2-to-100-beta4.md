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

## TimeWarp.Terminal Skill Reference

**Repository:** https://github.com/TimeWarpEngineering/timewarp-terminal  
**Package:** `TimeWarp.Terminal`

### Core Components

- **IConsole** - Basic testable I/O interface
- **ITerminal** - Rich output with colors, widgets, and hyperlinks
- **Terminal** static class - Quick utility, Console replacement
- **TestTerminal** / **TestConsole** - For unit testing

### Key Interfaces

| Interface | Methods |
|-----------|---------|
| **IConsole** | `Write`, `WriteLine`, `WriteErrorLine` (all return `IConsole`), `ReadLine` |
| **ITerminal** | Extends IConsole, adds `ReadKey`, `SetCursorPosition`, `GetCursorPosition`, `WindowWidth`, `IsInteractive`, `SupportsColor`, `SupportsHyperlinks`, `Clear` |

### Widgets Available

- **Panel** - Box with header and content
- **Table** - Tabular data with columns and alignment
- **Rule** - Section divider

### Common Pitfalls (to verify still work)

1. Don't use `new Table()`, `new Panel()`, `new Rule()` - constructors are internal
2. Don't mix Console and Terminal - pick one
3. Check `SupportsColor`/`SupportsHyperlinks` before using those features

### Testing Pattern

```csharp
using TestTerminal terminal = new("yes\n");
MyCommand command = new(terminal);
command.Execute();
Assert.Contains("expected text", terminal.Output);
```
