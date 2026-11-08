# Make silent no-ops loud: near-miss signatures, ignored hooks, dead parameters

## Description
A green run can mean nothing ran. Discovery keeps only methods returning exactly `Task`; `Task<T>`, `ValueTask`, and `async void` vanish with no diagnostic. A class with zero tests reports Success and exit code 0. Mis-signed `Setup`/`CleanUp` are skipped silently while once-hooks fail fast. The `clearCache` parameter is accepted and discarded. Class names strip every "Tests" occurrence.
Findings: CORE-4, CORE-5, CORE-16, TOOL-9 in task 038 report. Adjacent to task 039 (tautological test detection).
## Requirements
- Warn (default) or fail (strict) on public static methods with near-miss test or hook signatures.
- Zero executed tests fails the run by default; opt-out flag.
- Per-test Setup/CleanUp resolved once per class with the same validation as once-hooks.
- Remove or `[Obsolete]` `clearCache`; fix its docs.
- Fix class-name derivation to strip only a trailing "Tests".
- `RegisteredTestClasses` returns a snapshot, not the live list.
## Checklist
- [ ] Near-miss diagnostics
- [ ] Zero-tests failure
- [ ] Hook validation unified
- [ ] clearCache removed
- [ ] Class name and registration cleanups
- [ ] Tests

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
