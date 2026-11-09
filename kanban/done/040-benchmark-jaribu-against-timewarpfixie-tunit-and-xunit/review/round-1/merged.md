# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 3 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tools/dev-cli/endpoints/benchmark-measure.cs:196
- Description: The Jaribu runfile host is built and launched in Debug (no `-c` on `dotnet run run.cs`; the runfile caches show only `bin/debug`). Every other host is built and launched in Release, so the committed `jaribu-runfile` numbers are not like-for-like.
- Suggestion: Pass `-c Release` for the cache fill and timed launches, or document a deliberate Debug choice and add a Release row. Re-measure, re-commit the baseline and `latest.md`, and update `## Results`.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: tools/dev-cli/endpoints/benchmark-measure.cs:163
- Description: Time-to-first-test reads the signal file only when no line was captured. The readme (line 18) defines TTFT as the earliest of the two, and readme line 50 gives a third definition.
- Suggestion: Take the minimum when both are present, and align readme lines 18 and 50 with the report preamble.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: kanban/in-progress/040-benchmark-jaribu-against-timewarpfixie-tunit-and-xunit/task.md:58
- Description: The hot-spot list omits the quadratic scaling with methods per class: in-process 1k→10k costs about 56× the time and 95× the allocations, while fixtures with 10-method classes cost 5 ms. It also omits the visible cause, `ResolveHook` → `GetMethods` twice per test. The marginal per-test metric assumes linear cost.
- Suggestion: Name the scaling and the `ResolveHook` cause for task 054, and add a caveat to the readme Marginal row.
- Source: general
- Disposition notes:

### M4 — Severity: suggestion — Status: open
- File: .github/workflows/benchmark.yml:24
- Description: The baseline was measured with SDK 11.0.100-rc.3. The workflow uses 10.0.x, with no SDK pin, and the report does not record the test runtime.
- Suggestion: Record the runtime in the report header, and either align the SDK band or document that results compare only within one band.
- Source: general
- Disposition notes:

### M5 — Severity: suggestion — Status: open
- File: benchmarks/Directory.Build.props:9
- Description: Analyzers, code style, BannedApi, and `TimeWarp.Build.Tasks` are off for hand-written benchmark sources too, not just generated `*.g.cs` (which `generated_code = true` already covers).
- Suggestion: Scope the relaxation to generated files, or justify the wider carve-out in a comment.
- Source: general
- Disposition notes:

### M6 — Severity: nit — Status: open
- File: benchmarks/generator/emit-tunit.cs:230
- Description: The TUnit fixture check uses a process-wide `ClassSetups` counter, so it does not prove that each class's `[Before(Class)]` ran. The other hosts check per class.
- Suggestion: Add a per-class flag.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
