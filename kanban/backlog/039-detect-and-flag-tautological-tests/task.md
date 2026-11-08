# Detect and flag tautological tests

## Description

With agentic development, tests must be efficient and must actually prove something. Agents routinely produce tautological tests: tests that cannot fail, that assert a value against itself, that assert only on a constant, or that exercise no production code. Jaribu should detect these and flag them so they can be fixed or deleted.

Goal: a Jaribu feature (analyzer, runtime check, or both) that reports a test as **tautological** with a reason, surfaced in the run output and in the MTP results, and optionally failing the run under a strict flag.

## Requirements

- Define the detection rules. Starting list:
  - Test method has no assertion call (no `Should*` / `Assert*` / throwing path reachable).
  - Assertion compares an expression to itself or to a literal that it was just assigned from (`x.ShouldBe(x)`, `var a = 5; a.ShouldBe(5)` with no production call in between).
  - Test body calls no code outside the test project / test file.
  - Test always passes when the system under test is replaced by a stub that throws (mutation-style smoke check, opt-in because it is expensive).
  - Test is `Skip`ped or has an empty body.
- Decide where each rule lives: Roslyn analyzer (compile-time, cheap) vs runtime instrumentation (catches dynamic cases). Prefer analyzer for the static rules; document the split.
- Report shape: test id, rule id, one-line reason, file:line. Emit as a warning by default; `--strict-tautology` (or MSBuild property) turns warnings into failures.
- Add a `[KnownTautology("reason")]` or similar escape hatch for legitimate cases (smoke tests, compile-only checks).
- Tests for the detector itself, including negative cases that must NOT be flagged.
- Document the rules in `documentation/` and the readme.

## Checklist

- [ ] Write the rule catalog with examples of each tautology and each false-positive to avoid
- [ ] Spike: inventory existing tests in `tests/` against the rules by hand to calibrate
- [ ] Implement static rules as a Roslyn analyzer package
- [ ] Implement runtime no-assertion detection in the runner
- [ ] Surface findings in console output and MTP node properties
- [ ] Add strict mode flag and escape-hatch attribute
- [ ] Tests for the detector (positive and negative)
- [ ] Documentation

## Notes

- Related: task 038 (full code review) found that tests with unsupported signatures and classes with zero tests silently pass; those are adjacent "test that proves nothing" problems and may be folded into or depend on this work.
- Keep detection cheap by default so it does not slow the inner loop. Expensive checks (mutation smoke) are opt-in.
- No time estimates.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07)
