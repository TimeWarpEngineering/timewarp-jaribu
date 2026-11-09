# Low-priority cleanups from the task 038 review

## Goal (implementer restatement)

Close the nine leftover review items as one grab bag. Each item is done when the behavior below holds and the suite still passes.

- CORE-8: Session-dispose lines, the empty-registration warning, the multi-class grand total, and `TestHelpers` status lines go to an `ITerminal` the caller can supply. They do not write through the static `Terminal` or `TimeWarpTerminal.Default`.
- MTP-2: The unhandled-exception path publishes an Error node. A failure inside that publish does not replace the original exception. A successful publish does not also crash the framework.
- MTP-4: `TestNodeInfo` carries the start and end recorded around the run. The MTP sink uses those values for `TimingProperty` and does not invent a start from `UtcNow` minus duration.
- MTP-10: PDB lookup catches `ArgumentException` (and the other lookup failures) so a bad token cannot abort publication. An embedded portable PDB is read when there is no side-by-side file. A miss is traced.
- MTP-15: The test framework and the command-line provider have different extension uids. The version is the informational version. `InternalsVisibleTo` for the local test assemblies sits behind a build property.
- MTP-17: Parameter text for arrays and collections is the elements, not the type name. The setup-count comment matches the assertions. The timeout sample does not leave a five-second delay running. A failing state with no exception gets a message that says the core supplied none. The fully-qualified `MethodInfo` in the adapter is already gone.
- TOOL-13: `pre-commit` and `pre-push` refuse the git action with a message when the repo root cannot be found. Attest hooks report a failed `ganda repo attest` instead of ignoring it.
- TOOL-14: Drop the redundant central-package flag and the default warning level. The test projects no longer take a Nuru reference; that reference is already absent.
- TOOL-16: Dev-cli commands share one repo-root lookup.

Round-1 review acceptance: the five hook runfiles match the ganda scaffold again, and `memsearch-scaffold` passes. TOOL-13 moves to a ganda task that owns fail-closed hooks and records that a non-zero post-checkout exit becomes the exit status of `git checkout`, `git switch`, and `git worktree add`. The release pack builds the testing platform with `JaribuExposeInternals=false` and refuses a packed assembly that still grants internals. MTP-2 has one test where a successful Error publish returns, and one where a failed publish throws an `AggregateException` that keeps both exceptions. The version check follows the informational version and does not require a `beta` label. The Design comment says the original exception stays inside that aggregate. On a host that cannot emit a `DynamicMethod`, the PDB case is named as a smoke check.

## Description
Grab bag of small items that do not justify separate tasks.
Findings in task 038 report: CORE-8 (runner diagnostics bypass injected sink via static Terminal), MTP-2 (unhandled-exception handler publishes Error node then rethrows; guard the publish), MTP-4 (start timestamps synthesized as UtcNow minus duration; carry real timestamps on `TestNodeInfo`), MTP-10 (PDB lookup can throw `ArgumentException` out of node publication; support embedded PDBs; log misses), MTP-15 (framework and CLI provider share one extension uid; version read from assembly version; hard-coded `InternalsVisibleTo` for test assemblies), MTP-17 (fully-qualified `MethodInfo`, misleading test comment, `Task.Delay(5000)` without token, invented fallback exceptions), TOOL-13 (git hooks return 0 when repo root not found; attest failures swallowed), TOOL-14 (redundant `ManagePackageVersionsCentrally`, `WarningLevel`, unused Nuru reference in tests), TOOL-16 (repeated `FindRepoRoot` in dev-cli commands).
## Checklist
- [x] CORE-8
- [x] MTP-2
- [x] MTP-4
- [x] MTP-10
- [x] MTP-15
- [x] MTP-17
- [x] TOOL-13 — deferred to ganda task 575. This repo keeps the scaffold hook bytes.
- [x] TOOL-14
- [x] TOOL-16

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: 01a1be00-e376-7272-829b-f5034dc8d072 (2026-11-09). Closed the nine grab-bag items on this branch. `ganda repo audit` exits 0 with two advisory warnings: `memsearch-scaffold` (the git hooks now fail closed, so they no longer match the scaffold bytes) and `vscode-window-icon` (`.vscode/settings.json` is still absent; task 052 left it untracked). The two new runfiles are executable. `kanban-landed` is cleared by landing this kitchen.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1be00-e376-7272-829b-f5034dc8d072, max-turns 500, Done — 2026-11-09T02:43:28Z
- Review oracle round 1 (2026-11-09): reviewer general (claude, model claude-opus-5-5), effort 3, turn cap 200. The harness did not expose a session id. Result: 6 open findings (1 bug, 3 suggestion, 2 nit), so a fix pass is requested. Artifacts are under `review/round-1/`.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 7fb20890-6682-4e0a-a022-640b4d0abbd8, max-turns 200, artifacts general.md, merged.md — 2026-11-09T02:56:56Z
- Fix pass (review round 1): implement-fix-2 (grok, model grok-4.7) — 2026-11-09. Restored the hook scaffold so `memsearch-scaffold` passes. TOOL-13 is ganda task 575, published to that repo's to-do. The release pack sets `JaribuExposeInternals=false` and rejects a packed lib assembly that still grants internals.
- Fix pass resume: implement-fix-2 (grok, model grok-4.7) — 2026-11-09. The M1–M6 product changes were already on the branch. The Design region still said a published Error node rethrows; that sentence now says the handler returns. `ganda repo audit` passed, including `memsearch-scaffold`. `./bin/dev build` and `./bin/dev test` passed: the CI runner printed Passed 203, and the MTP run reported succeeded 203 and failed 0.

## Results

`RunTests` and `RunAllTests` take an optional `ITerminal` before the cancellation token. One terminal receives class output, the empty-registration warning, session-dispose failures, and the grand total. `TestHelpers` status lines write to the terminal the caller passes. A failed Error-node publish is an `AggregateException` of the original exception and the publish failure. A successful publish does not rethrow. `TestNodeInfo` carries `StartTime` and `EndTime`, and `TimingProperty` is published only when both timestamps and a duration are present. PDB lookup treats `ArgumentException` and `NotSupportedException` as misses, prefers a side-by-side portable PDB, and otherwise reads an embedded portable PDB. Misses go to trace under `Jaribu.Pdb`. The command-line provider uid is `TimeWarp.Jaribu.CommandLine`. Both extensions report `ExtensionVersion.Current`, the informational version with a `+` suffix removed. `InternalsVisibleTo` is omitted when `JaribuExposeInternals` is `false`. The release pack passes that property, then reads each packed lib assembly and refuses the release when any `InternalsVisibleTo` grant remains. Parameter sequences print as `[a,b]`. A Failed, Timeout, or Error node with no exception says the core supplied none. The setup-count comment matches the assertions, and `TimeoutExceededTest` bounds the abandoned wait at 200ms. The duplicate central-package flag and `WarningLevel` are gone. Dev-cli commands share `RepoRoot`.

The five `.githooks` runfiles match the ganda scaffold again. `ganda repo audit --fix` restored them, and `memsearch-scaffold` passes. TOOL-13 (fail closed when the root is missing, and report a failed attest) is ganda task 575. A non-zero post-checkout exit becomes the exit status of `git checkout`, `git switch`, and `git worktree add`, and that note is in the ganda task.

The fully-qualified `MethodInfo` and the unused Nuru test reference were already gone. There is no fixture that builds an embedded PDB, so that path is covered by the reader and not by a round-trip test. On a host that cannot emit a `DynamicMethod`, `PdbLookup_Should_MissADynamicMethod_SmokeCheckWhenDynamicCodeIsUnsupported` is a smoke check that `TryCreate` does not throw. The MTP host still builds a `DynamicMethod` and expects a miss. An unsupported MTP request publishes one Error node and returns. A bus that throws produces an `AggregateException` whose inner exceptions are the original `NotSupportedException` and the publish failure.

### How to validate

Smoke: `dotnet build timewarp-jaribu.slnx --nologo` then `dotnet run tests/timewarp-jaribu/multi-file-runners/ci-runner/run-ci-tests.cs /p:ExperimentalFileBasedProgramEnableTransitiveDirectives=true --nologo` and `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj --nologo`. Also `dotnet build tools/dev-cli/dev.cs` and `dotnet build` of each `.githooks/*.cs` runfile.

Expect: the solution build reports 0 warnings and 0 errors. The CI runner prints `Passed: 203` and exits 0. The MTP project reports `succeeded: 203` and `failed: 0`. `dotnet build tools/dev-cli/dev.cs` succeeds. `ganda repo audit` exits 0, including `memsearch-scaffold`. A Release build with `-p:JaribuExposeInternals=false` writes a testing platform dll whose bytes do not contain `InternalsVisibleTo` or the local test assembly names.

### Review

Round 1 ran with one general reviewer at effort 3. The fix pass marked M1–M6 fixed on `review/round-1/merged.md`. Counts after that edit: bug 0 open / 1 fixed, suggestion 0 open / 3 fixed, nit 0 open / 2 fixed, none wontfix. Round 2 has not run, so there is no disposition yet. M1 restored the hook scaffold and filed ganda task 575. M2 passes `JaribuExposeInternals=false` on the release pack and checks the packed assemblies. M3 covers both MTP-2 paths. M4 drops the `beta` assertion. M5 rewords the aggregate comment, and the later Design sentence now says a successful publish returns. M6 names the no-dynamic-code path as a smoke check. Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Review

- Round 1: six findings, addressed in the fix pass (M1–M6 marked fixed). Round 2 has not run.
