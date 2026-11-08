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
## Goal (implementer restatement)

A green run must mean the tests the author wrote actually ran. Discovery that keeps only methods returning exactly `Task` hides `Task<T>`, `ValueTask`, `ValueTask<T>`, `async void`, and open generic `Task` methods. A class or a batch with nothing to run still exits 0. A mis-signed `Setup` or `CleanUp` is skipped, while the once-hooks already fail the class. `clearCache` is accepted and ignored. The reported class name deletes every `Tests` substring, and `RegisteredTestClasses` hands callers the live list.

Acceptance:

- A public static near-miss test is a warning on `TestRunStats.SignatureWarnings`. `strictSignatures: true` turns each warning into an Error node and fails the run. Ordinary `void` helpers stay silent.
- `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` are resolved once per class run. The signature rule is the same for all four: one `public static Task Name()` with no parameters and no type parameters. A near-miss hook fails the class as Error and the message names the declaring type. Bodies do not run.
- No discoverable tests, and `RunAllTests` with no registered classes, fails unless `allowEmpty: true`. A tag or method filter that selects nothing is not an empty class.
- `RunTests` and `RunAllTests` do not take `clearCache`. Docs stop saying the runner clears the runfile cache.
- The reported class name drops only a trailing `Tests`, and only when the name is longer than that suffix.
- `RegisteredTestClasses` returns a copy. A later register or clear does not change a list the caller already holds.

## Checklist
- [x] Near-miss diagnostics
- [x] Zero-tests failure
- [x] Hook validation unified
- [x] clearCache removed
- [x] Class name and registration cleanups
- [x] Tests

## Results

A public static near-miss (`Task<T>`, `ValueTask`, `ValueTask<T>`, `async void`, or an open generic `Task`) is a warning on `TestRunStats.SignatureWarnings`. `strictSignatures: true` records each as an Error. Ordinary `void` stays silent. The terminal sink prints each warning.

`Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` are resolved once per class. A bad signature is an Error that names the declaring type, and the body does not run. An empty class with a bad hook is still an Error when `allowEmpty` is set. A case filter that selects nothing stays empty.

A class with no discoverable tests, and `RunAllTests` with nothing registered, fails unless `allowEmpty: true`. A tag or method filter that selects nothing does not. `clearCache` is gone. `CancellationToken` is the last parameter. The reported class name strips one trailing `Tests` only when characters remain. `RegisteredTestClasses` returns a snapshot.

`ganda repo audit --fix` set the local VS Code window title. That settings file is gitignored, so it is not in the commit. Source stays `1.0.0-beta.16`, one prerelease ahead of the published `1.0.0-beta.15`.

### How to validate

Smoke: `./bin/dev test` and `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj`

Expect: the CI runner exits 0 and `SilentNoOps_Given_` passes 12. The MTP runner exits 0 (145 passed on this walk). `ganda repo audit` passes. `./bin/dev check-version` reports source `1.0.0-beta.16` is one prerelease ahead of published `1.0.0-beta.15`.

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 3691322 (2026-11-08)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b959-6bb5-7ae1-bc05-0093d7aad662, max-turns 500, Done — 2026-11-08T04:53:54Z
