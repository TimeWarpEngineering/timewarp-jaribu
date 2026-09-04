# Round 1 — mtp
**Date:** 2026-09-04
**Scope reviewed:** JaribuTestFramework, MtpSink, MSBuild hook, discovery vs run, --filter-* at SHA e5ef320

## Summary

MTP adapter wiring is sound at this SHA: `TestingPlatformBuilderHook` registers CLI options + `JaribuTestFramework`, session begin/end mirrors core, MSBuild props attach (confirmed via generated `SelfRegisteredExtensions`), and `--filter-tag` / `--filter-class` / `--filter-method` match the skill table for method selection (tag → Skipped; class/method → omitted) with 031 class-level tag discovery omission still present and live-verified. Filter discovery/run counts match for tag/class/method on the validation project. One real discovery-vs-run defect remains: `[Input]` parameterized methods are discovered once but produce multiple MTP completions under a shared UID.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:103
- Description: Discovery emits one node per method (`Uid = $"{testClass.FullName}.{method.Name}"`) and does not expand `[Input]` cases. The run path publishes one started/completed pair per `[Input]` while reusing that same UID (`source/timewarp-jaribu/test-runner.cs:772-788`). MTP counts every terminal completion, so discovery and run totals diverge: live spot-check on `mtp-runner` with `--filter-class Parameterized` listed **6** tests but ran **total: 7**; `--filter-method MultipleInputs` ran **total: 2** for a single discovered method. Shared UIDs also prevent Test Explorer from treating each data row as a distinct node (last completion wins for that UID’s displayed state).
- Suggestion: Give each `[Input]` a stable distinct UID (and matching DisplayName) in both discovery and run—e.g. append a deterministic parameter suffix—and have `JaribuTestFramework` discovery enumerate `InputAttribute`s the same way `RunTestWithSinkAsync` does so `--list-tests` and run node-sets match.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:62
- Description: `PublishNodeAsync` only attaches a `TestNodeStateProperty` and optional `TimingProperty`. It never publishes `TestMethodIdentifierProperty` or `TestFileLocationProperty`, so IDE hosts that rely on those MTP properties for navigate-to-source / method identity get display names only. Parameters from `TestNodeInfo` are also dropped at the bus boundary (only carried indirectly via DisplayName when the runner formats them).
- Suggestion: When publishing discovered/completed nodes, add `TestMethodIdentifierProperty` (and `TestFileLocationProperty` when a declaring path/span is available). Optionally surface `TestMetadataProperty` for `[TestTag]` so tree/trait filters can use real traits instead of an empty `PropertyBag` in `MatchesFilter`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:78
- Description: Filter behavior is implemented and manual/live checks pass (`--filter-tag EdgeCases` discovery 5 = run 5; `--filter-method Skipped` discovery 2 = run 2 skipped; class-level tag omission still at `ClassOmittedByTagFilter` lines 145-149), but there is still no automated assertion for (a) `--filter-class` omit semantics under MTP, (b) CLI `--filter-tag` winning over `JARIBU_FILTER_TAG` in `ResolveFilterTag` (lines 152-161), or (c) `MtpSink.OnTestStartedAsync` always publishing `InProgress` (`mtp-sink.cs:38-47`) rather than a terminal state. Regressions would only show up under a real MTP host.
- Suggestion: Add focused MTP or InternalsVisibleTo tests covering filter-class omission, CLI-over-env tag precedence, and the InProgress-on-start contract for skip/fail short-circuits.
- Status: open

### Issue 4 — Severity: nit
- File: skills/tw-jaribu/SKILL.md:302
- Description: The skill filter table states `--filter-tag` non-match → **Skipped**. That matches method-level tags (`test-runner.cs:715-731`) but not class-level tags: both discovery (`jaribu-test-framework.cs:90-93`) and run omit the whole class when class tags exist and none match. `readme.md:326` already documents the omit rule; the skill row is the incomplete one.
- Suggestion: Align the skill row with README—method-level non-match → Skipped; class-level non-match → omitted from discovery and run.
- Status: open
