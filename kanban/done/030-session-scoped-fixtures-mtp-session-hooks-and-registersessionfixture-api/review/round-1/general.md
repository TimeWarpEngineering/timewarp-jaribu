# Round 1 — general
**Date:** 2026-08-02
**Scope reviewed:** task 030 session fixtures + #22 + #23 (commits 0835c82..48a338d)

## Summary

Session fixture design matches the plan: explicit `RegisterSessionFixture` / `SessionFixture.GetAsync`, register-time CreateAsync validation, double-reg fail-fast, lazy create, nesting-aware dispose, and mode parity (MTP begin/end, `RunAllTests` wrap, lone `RunTestsAsync` session-of-one). #22 (`MtpSink` always InProgress on start) and #23 (CLI filter options, tag CLI-over-env, selection omit vs tag Skipped, uid filter on run path) are implemented correctly in the MTP adapter. Session-fixture meta-tests and method-name selection tests are solid; gaps are create-failure stickiness across classes and missing automated coverage for #22 / methodPredicate / filter-class.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-jaribu/session-fixture.cs:100
- Description: Failed `CreateAsync` does not record a sticky failure. `Instance` stays null, so a later `GetAsync` (e.g. next class in the same multi-class MTP/`RunAllTests` session) re-invokes `CreateAsync`. For expensive fixtures that partially boot then throw, that can double side effects (orphan hosts/ports) and change failure mode from “once per session” to “once per GetAsync caller.” Single-class path is fine because SetupOnce caches the hook exception.
- Suggestion: On create failure, store the exception on `FixtureEntry` under the create gate and rethrow it on subsequent `GetAsync` for the rest of the session (clear the failure when instances are cleared at session end). Add a multi-class meta-test that asserts `CreateCount == 1` after two classes both call GetAsync on a failing fixture.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:33
- Description: Plan/task require suite coverage for the #22 skip double-count fix. There is no automated test that `OnTestStartedAsync` publishes InProgress rather than the terminal Skipped/Failed state passed in the node. Regression of the pre-fix “start+complete both terminal Skipped” behavior would only show up under real MTP hosts.
- Suggestion: Add a focused unit/meta-test (InternalsVisibleTo or a thin test seam) that drives `MtpSink.OnTestStartedAsync` with a Skipped node and asserts the published MTP state is InProgress, plus complete still Skipped.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:77
- Description: #23 core selection for `methodNameContains` is covered in `test-runner.tag-filtering.cs`, but there are no tests for (a) `methodPredicate` omit semantics used for MTP uid/tree filters on the run path, or (b) filter-class omit-at-class-level behavior. CLI-over-env for `--filter-tag` is implemented in `ResolveFilterTag` but not asserted.
- Suggestion: Add core tests: `RunTestsAsync(..., methodPredicate: m => m.Name == "Alpha")` omit semantics; and a small test of CLI-vs-env precedence if a testable seam exists (or document that MTP integration is manual-only).
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.cs:541
- Description: `AllSkipSessionFixtureUser.SetupOnceCount` is a static counter never reset in `AllSkip_Should_NotCreate` (or a `Reset()` helper). Harmless today because the class is only used once and SetupOnce should not run, but it is fragile if the suite is re-entered or another test shares the type.
- Suggestion: Reset `SetupOnceCount` at the start of the test (mirror `CountingSessionFixture.Reset()`).
- Status: open
