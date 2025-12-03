# Tabular Test Results Output

## Summary

Add formatted tabular output for test results after a test run completes. Display results in a table with columns for Test, Status, Duration, and Message - with color-coded status indicators (green ✓ Pass, red X Fail, yellow ⚠ Skip) and a summary line showing totals.

## Todo List

- [x] Add `PrintResultsTable(TestRunSummary summary, ITerminal terminal)` method to `TestHelpers`
- [x] Use Nuru's `Table` widget (handles column widths automatically)
- [x] Use Nuru color extensions (`.Green()`, `.Red()`, `.Yellow()`) for status
- [x] Truncate long messages with "..." to fit reasonable width
- [x] Add summary line: `Total: X  Passed: Y  Failed: Z  Skipped: W`
- [x] Call `PrintResultsTable` at end of `RunTestsWithResults<T>()`
- [ ] Add test for tabular output formatting using `TestTerminal`

## Notes

Reference image shows output format:
```
┌──────────────────┬────────┬──────────┬─────────────────────────────────────┐
│ File             │ Status │ Duration │ Message                             │
├──────────────────┼────────┼──────────┼─────────────────────────────────────┤
│ envvars.cs       │ ✓ Pass │ 0.33s    │ Completed successfully              │
│ pinvoke.cs       │ X Fail │ 10.01s   │ Timeout (10s)                       │
│ spaceinvaders.cs │ X Fail │ 0.44s    │ Unhandled exception. System.IO...   │
└──────────────────┴────────┴──────────┴─────────────────────────────────────┘

Total: 15
Passed: 11
Failed: 4
```

Implementation considerations:
- Keep existing per-test console output during execution for real-time feedback
- Table serves as summary at the end
- Use box-drawing characters or simple ASCII for table borders
- Message column should show: "Completed successfully" for pass, skip reason for skipped, exception message (truncated) for failed
- Consider max message width of ~50-60 chars before truncation

Existing infrastructure to leverage:
- `TestRunSummary` record already contains all needed data
- `TestResult` has TestName, Outcome, Duration, FailureMessage
- `TestOutcome` enum: Passed, Failed, Skipped
- TimeWarp.Nuru 3.0.0-beta.12 added as dependency (task 003)
  - `Table` widget with `.AddColumn()`, `.AddRow()`, border styles
  - `ITerminal`/`TestTerminal` for testable output
  - Color extensions: `.Green()`, `.Red()`, `.Yellow()`, `.Bold()`
