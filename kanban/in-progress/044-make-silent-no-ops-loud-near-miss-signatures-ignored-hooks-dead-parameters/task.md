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

Round 1 left four gaps. Strict mode throws when two near-miss methods share a name. The MTP runner drops the warnings and has no way to set the two flags. A filtered run invents an empty-class error for a class the user did not select. `RunSingleTestAsync` throws its null checks before the task starts, and the prepared path repeats them.

Acceptance:

- A public static near-miss test is a warning on `TestRunStats.SignatureWarnings`. `strictSignatures: true` turns each warning into an Error node and fails the run. Ordinary `void` helpers stay silent. Overloaded near-miss names become one Error node each. The node carries the method that was found, and its id includes the near-miss index. The run does not throw `AmbiguousMatchException`.
- `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` are resolved once per class run. The signature rule is the same for all four: one `public static Task Name()` with no parameters and no type parameters. A near-miss hook fails the class as Error and the message names the declaring type. Bodies do not run.
- No discoverable tests, and `RunAllTests` with no registered classes, fails unless `allowEmpty: true`. A tag or method filter that selects nothing is not an empty class. A method filter, a method predicate, or a case predicate also skips the empty-class `NoTests` node and the empty-class `HookSignature` node. Those nodes do not bind a user method of the same name.
- `RunTests` and `RunAllTests` do not take `clearCache`. Docs stop saying the runner clears the runfile cache.
- The reported class name drops only a trailing `Tests`, and only when the name is longer than that suffix.
- `RegisteredTestClasses` returns a copy. A later register or clear does not change a list the caller already holds.
- Under `dotnet test`, each signature warning is written to the MTP output device when that class produced nodes, or when no selection filter is active. `--strict-signatures` and `--allow-empty` reach `RunTestsAsync`. A run of an empty class still fails, and `--allow-empty` drops that error. A uid or tree filter does not fail on an empty class that was not selected.
- `RunSingleTestAsync` documents that a null class or method throws `ArgumentNullException` before the returned task starts. `RunPreparedTestAsync` does not repeat those checks. The design comment about the input ordinal reads as one sentence.

Round 2 left one gap. Strict mode still emits a near-miss Error for a class that a method predicate or a case predicate did not select. A tree-filtered `dotnet test` with `--strict-signatures` then fails on `SignatureWarningTests.ReturnsTaskOfInt`, which discovery never listed. The non-strict warning for that same unselected class is already hidden.

Acceptance for this pass:

- When `methodPredicate` or `casePredicate` is set and the class produces no nodes, strict mode does not emit near-miss Error nodes. The warning text stays on `TestRunStats.SignatureWarnings`. The run succeeds when nothing else failed. A `methodNameContains` filter still reports those Error nodes.
- When that predicate selects a real test in the same class, strict mode still emits the near-miss Error nodes.
- `dotnet test -- --treenode-filter <BasicTests/PassingTest> --strict-signatures` on the validation project exits 0. The summary is one succeeded test, and the output does not mention `ReturnsTaskOfInt`.

## Checklist
- [x] Near-miss diagnostics
- [x] Zero-tests failure
- [x] Hook validation unified
- [x] clearCache removed
- [x] Class name and registration cleanups
- [x] Tests
- [x] Overloaded near-miss strict mode
- [x] MTP warnings and flags
- [x] Selection filter skips empty-run nodes
- [x] RunSingleTestAsync null-guard cleanup
- [x] Strict near-miss nodes honor method and case predicates

## Results

A public static near-miss (`Task<T>`, `ValueTask`, `ValueTask<T>`, `async void`, or an open generic `Task`) is a warning on `TestRunStats.SignatureWarnings`. `strictSignatures: true` records each as an Error. Ordinary `void` stays silent. The terminal sink prints each warning. Two near-miss methods may share a name. Each strict Error carries that method, and its id includes the near-miss index.

A method predicate or case predicate that selects no nodes omits those strict Error nodes. The warning text stays on `SignatureWarnings`, and the run succeeds when nothing else failed. A `methodNameContains` filter still reports the Error nodes. Under `dotnet test`, a tree filter plus `--strict-signatures` does not fail an unselected class, and the warning for that class stays hidden.

`Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce` are resolved once per class. A bad signature is an Error that names the declaring type, and the body does not run. An empty class with a bad hook is still an Error when `allowEmpty` is set. A case filter that selects nothing stays empty.

A class with no discoverable tests, and `RunAllTests` with nothing registered, fails unless `allowEmpty: true`. A tag or method filter that selects nothing does not. `clearCache` is gone. `CancellationToken` is the last parameter. The reported class name strips one trailing `Tests` only when characters remain. `RegisteredTestClasses` returns a snapshot.

`ganda repo audit --fix` set the local VS Code window title. That settings file is gitignored, so it is not in the commit. Source stays `1.0.0-beta.16`, one prerelease ahead of the published `1.0.0-beta.15`.

### How to validate

Smoke: `./bin/dev test`

Expect: the CI runner exits 0. This walk passed 149, including `SilentNoOps_Given_` 15 and the MTP identity class 8 (`SignatureWarnings_Should_ShowAndHonorMtpFlags` covers the tree filter plus `--strict-signatures`). `ganda repo audit` passes (39 passed, 0 failed). `./bin/dev check-version` reports source `1.0.0-beta.16` is one prerelease ahead of published `1.0.0-beta.15`.

### Review

- Round 1 used effort 3 with the general reviewer. Disposition is pending, and there is no `review/disposition.md` yet.
- Counts are 2 open bugs (M1, M2), 1 open suggestion (M3), 1 open nit (M4), and nothing fixed or wontfix.
- M1: with `strictSignatures: true`, overloaded near-miss names throw `AmbiguousMatchException`. M2: under MTP, near-miss warnings are dropped and neither `strictSignatures` nor `allowEmpty` can be set. M3: the `NoTests` node ignores selection filters. M4: the null guards in `RunSingleTestAsync` changed and a comment wraps mid-sentence.
- The reviewer reran `./bin/dev test`: exit 0, 145 passed.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.
- Next: a fix pass on this id, then re-review in `review/round-2/`.
- Round 2 used effort 1 with the general reviewer. Disposition is still pending, and there is no `review/disposition.md` yet.
- Round 2 counts: 2 bugs fixed (M1, M2), 1 suggestion fixed (M3), and 1 nit fixed (M4). One new suggestion is open: M5.
- M5: strict near-miss Error nodes ignore uid and tree selection. Under `dotnet test --treenode-filter <one test> --strict-signatures`, the run fails because of an unselected class. That is the M3 scenario on the strict path.
- The round-2 reviewer ran `test-runner.silent-no-ops.cs` (14 passed) and the MTP runner with `--filter-class Mtp_` (8 passed), and reproduced M5 on the validation project.
- The fix-pass product changes were still uncommitted in the claim worktree during round 2, and the round-2 checklist items above are not ticked.
- Paths: `review/round-2/general.md`, `review/round-2/merged.md`.
- Next: a fix pass on this id for M5, then re-review in `review/round-3/`.
- Implement-fix-3 closed M5. A method predicate or case predicate that selects no nodes omits the strict near-miss Error nodes. `methodNameContains` still reports them. The tree-filter plus `--strict-signatures` case is in `SignatureWarnings_Should_ShowAndHonorMtpFlags`.
- Round 3 used effort 2 with the general reviewer and covered the delta since round 2 (`d3ffe95` against `079be23`). It confirmed M5 fixed, re-checked M1 to M4 on the committed tree, and raised nothing new.
- Final counts: bug 2 fixed, suggestion 2 fixed (M3, M5), nit 1 fixed. Nothing is open or wontfix.
- **Disposition: clean.** 3 rounds, final open count 0, no escalations.
- The round-3 reviewer ran `test-runner.silent-no-ops.cs` (15 passed) and `dotnet test -- --filter-class Mtp_` in the MTP runner (8 passed). On the validation project, the round-2 M5 repro (`--treenode-filter .../BasicTests/PassingTest --strict-signatures`) now exits 0 with 1 succeeded. An unfiltered `--strict-signatures` run still fails on `ReturnsTaskOfInt`.
- Paths: `review/review-framework.md`, `review/round-3/general.md`, `review/round-3/merged.md`, `review/disposition.md`.

## Learnings

- Misread: the task requires that "zero executed tests" fail the run. The restatement narrowed that to "no discoverable tests". A class whose discovered tests are all `[Skip]` still executes nothing and exits 0. The narrowing is defensible, because skip nodes are reported and are deliberate, but the restatement does not say it chose to narrow. why: review round 1, 2026-11-08

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 3691322 (2026-11-08)
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b959-6bb5-7ae1-bc05-0093d7aad662, max-turns 500, Done — 2026-11-08T04:53:54Z
- Review oracle round 1: Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work`, effort 3, roster general, turn cap 200. 4 findings open (M1 and M2 are bugs). Blocked for a fix pass — 2026-11-08
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session e9826e5e-6929-4bfb-9b9c-afc45a4b1dac, max-turns 200, artifacts general.md, merged.md — 2026-11-08T04:59:16Z
- Review oracle round 2: Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work`, effort 1, roster general, turn cap 80. M1 through M4 are fixed and the new M5 (suggestion) is open. Blocked for a fix pass — 2026-11-08
- Implement-fix-3: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b99e-50a5-7cc0-915d-f8159718f5f8, GANDA_SESSION 3691322. M5 closed — 2026-11-08
- Review oracle round 3: Claude Opus 5.5 (claude-opus-5-5), headless `ganda task work`, effort 2, roster general, turn cap 120. M5 is fixed, there are no new findings, and the disposition is clean. Done — 2026-11-08

## Review

- Round 1: Blocked — open M1, M2, M3, M4
- Round 2: Blocked — open M5
- Round 3: Done

- CI red on PR #40 (run 42452007606), noted by ce5c4952-6364-4ae8-824b-a0243ec9ccbe 2026-11-08: `SignatureWarnings_Should_ShowAndHonorMtpFlags` in `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs` fails with `ShouldAssertException ... should contain (case insensitive) "Near-miss" but did not`. On CI the MTP validation run it launches prints "Test run summary: Passed! total: 1 succeeded: 1" and no signature warning line. Locally it passed. Suspects: the warning is written through a terminal path that MTP suppresses or redirects when stdout is not a TTY (GitHub runner), the warning goes to stderr while the test captures stdout only, or the near-miss fixture is filtered out by the invocation used on CI. Reproduce with output redirected to a file (`dotnet test ... > out.txt 2>&1`) or under `CI=true`, make the warning reach a stream the test captures (or assert via node properties / exit code instead of console text), then confirm on CI.
