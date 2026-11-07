# Analysis: complete code review of timewarp-jaribu

Task 038. Read-only review at commit `797911e` (master, 2026-11-07). No code was changed. No builds or tests were run; findings come from reading the code and from `ganda repo audit`.

## Executive Summary

The library core is well designed around session fixtures, once-hooks, and stable per-input node identity, and the build is strict. The main risks are in the MTP adapter: no cancellation support, a tree-node filter that cannot match what IDEs and `dotnet test` send, packed props that turn transitive consumers into test executables, and a validation project that asserts nothing. In the core, async assertion failures are reported as Error rather than Failed, timeouts abandon the running test, and mis-signed tests or hooks are silently dropped so a green run can mean nothing ran. Tooling has a release path with no tag-to-version guard, a stray unrelated script at repo root, and a readme whose sample code does not compile.

## Scope

- `source/timewarp-jaribu/` (core, 16 files, about 2,200 lines)
- `source/timewarp-jaribu-testing-platform/` and `tests/timewarp-jaribu-mtp-validation/`
- `tools/dev-cli/`, `.githooks/`, `.github/workflows/`, `msbuild/`, build props, `tests/timewarp-jaribu/`, `readme.md`, `agent.md`, `claude.md`, `skills/`, `assets/`, `work-thread-report.cs`

## Methodology

Three parallel reviewers, one per area, each reading every file in scope fully and reporting with a fixed finding shape (severity, file:line, what, why, candidate task). `ganda repo audit` was run read-only. Findings were merged without re-ranking across areas; ids are prefixed CORE, MTP, TOOL.

## Audit baseline (`ganda repo audit`, read-only)

| Check | Result |
|---|---|
| `editorconfig-charset` | `charset = "utf-8"` is quoted; expected unquoted under `[*]` |
| `glossary` | `glossary.md` is missing |
| `required-gitignore-entries` | Missing `bin/dev`, `bin/dev.dbg`, `bin/dev.exe` |
| `routine-journals-gitignore` | Missing `stacked-task-set*.resume.json` |
| `vscode-settings-gitignore` | `!.vscode/settings.json` re-include keeps it tracked |
| `source-file-line-cap` | `test-runner.cs` 1135, `test-runner.session-fixture.cs` 883, `test-runner.setup-once.cs` 814, `work-thread-report.cs` 515 (cap 500, no auto-fix) |
| `memsearch-scaffold` (warning) | `.githooks/pre-push.cs` outdated |
| `kanban-landed` | task 038 kitchen not landed; expected while in flight |

Everything except the line caps, glossary, and stale hook is `--fix`-able and should be applied as the baseline by whichever task touches the repo first.

## Triage table (candidate tasks)

Severity is the reviewer's. Cross-area duplicates are noted so they can be merged into one task.

| Id | Severity | Candidate task |
|---|---|---|
| CORE-1 | Medium | Classify assertion failures as Failed regardless of sync or async throw, and keep Error for infrastructure faults. |
| CORE-2 | Medium | Replace the delay-based timeout with a `CancellationTokenSource` and `TimeProvider`, observe the abandoned task, and cancel the timer on completion. |
| CORE-3 | Medium | Validate `TimeoutAttribute` input and add a timeout policy for hooks and fixture creation. |
| CORE-4 | Medium | Make per-test Setup and CleanUp resolution fail fast with the same validation as once-hooks, resolved once per class. |
| CORE-5 | Medium | Warn or fail on public static methods with near-miss test signatures, and treat zero executed tests as a failure by default. |
| CORE-6 | Medium | Track fixture creation order and dispose in reverse creation order. |
| CORE-7 | Medium | Make begin-if-inactive atomic and make unbalanced `EndSessionAsync` calls observable. |
| CORE-8 | Medium | Route runner diagnostics and the grand-total summary through an injectable `ITerminal` or sink. |
| CORE-9 | Low | Extract a single tag-match helper and a single skipped-node factory. |
| CORE-10 | Low | Emit Skipped nodes for filtered classes, or document the difference. |
| CORE-11 | Low | Add `CancellationToken` support to the runner and produce Cancelled, or document it as host-only. |
| CORE-12 | Low | COREormat arguments invariantly, apply `FormatTestName` to the method name only, and handle `[Input(null)]` explicitly. |
| CORE-13 | Low | Add XML docs to undocumented public members and add consistent null guards in sinks and attributes. |
| CORE-14 | Low | Split `test-runner.cs` into partial files for registration, single-test execution, hooks, class run, and summary output. |
| CORE-15 | Low | Cache hook and attribute metadata per class and await `Task<T>` directly in `SessionFixture`. |
| CORE-16 | Low | Clean up the registration surface, class-name derivation, and obsolete parameters. |
| CORE-17 | Low | Add tests for the uncovered edge cases listed. |
| MTP-1 | High | Thread `CancellationToken` through `TestRunner.RunTestsAsync` and the MTP adapter. |
| MTP-2 | Medium | Guard the failure publish and decide whether to rethrow or only report. |
| MTP-3 | Medium | Expose a single core filter helper used by both discovery and run, and add a test that compares them. |
| MTP-4 | Medium | Carry real start and end timestamps on `TestNodeInfo` and use them in the sink. |
| MTP-5 | Medium | Check whether `CancelledTestNodeStateProperty` exists in MTP 2.3.3 and map to it, or document the limitation. |
| MTP-6 | High | Build the MTP tree path and real property bag for each case before calling TreeNodeFilter, and add a test that uses --treenode-filter. |
| MTP-7 | Medium | Declare the supported MTP capabilities (tree-node filter, graceful stop) and document why each unsupported one is omitted. |
| MTP-8 | Medium | Pack the props only to `build/` or guard the settings with a opt-in property, and add a pack test for a transitive consumer. |
| MTP-9 | Medium | Switch on the request type explicitly and report an error node or throw for unsupported requests. |
| MTP-10 | Medium | Catch `ArgumentException` in source-location lookup, support embedded portable PDBs, and log lookup misses at debug level. |
| MTP-11 | Medium | Add an out-of-process harness that runs the validation project with each filter and compares reported node states to an expected table. |
| MTP-12 | Medium | Reject or disambiguate overloaded test methods in discovery and use a deterministic uid for the unhandled-error node. |
| MTP-13 | Low | Normalize the environment fallback with the same whitespace check and trim it. |
| MTP-14 | Low | Add XML docs and Purpose regions to the hook, extension, and capabilities, and rename the discarded parameter. |
| MTP-15 | Low | Give each extension its own uid, read the informational version, and move test-only InternalsVisibleTo entries behind a build condition. |
| MTP-16 | Low | Clear the location caches at session close or key them by assembly MVID. |
| MTP-17 | Low | Clean up the minor items, and report a distinct diagnostic message when the core supplies no exception for a failing state. |
| TOOL-1 | High | Split session-fixture and setup-once test files by responsibility to get under the 500-line cap. |
| TOOL-2 | High | Remove `work-thread-report.cs` from the repo, or move it to a personal tooling repo. |
| TOOL-3 | High | Make `dev workflow` release fail unless the tag matches the version, and push only the two freshly packed packages. |
| TOOL-4 | Medium | Delete the three excluded endpoint files, or re-enable them and have `workflow` call the check-version logic. |
| TOOL-5 | Medium | Make `dev test` run both runners, and decide how to run the MTP validation project. |
| TOOL-6 | Medium | Harden the workflow with an OS matrix, a job timeout, concurrency, and `id-token: write` only on release. |
| TOOL-7 | Medium | Correct readme dependency, license, sample code and dead-link claims. |
| TOOL-8 | Medium | Add tests for `JARIBU_FILTER_TAG`, `TestHelpers`, and the `RunTests<T>` exit-code mapping. |
| TOOL-9 | Medium | Remove the `clearCache` parameter, or mark it obsolete and fix its docs. |
| TOOL-10 | Medium | Rewrite or delete `test-plan.md` to match the current suite. |
| TOOL-11 | Low | Fix the stale paths and version location in `agent.md`. |
| TOOL-12 | Low | Fix `.editorconfig` charset, stale Nuru comment and section glob. |
| TOOL-13 | Low | Make master-protection hooks fail closed with a message, and refresh `pre-push.cs`. |
| TOOL-14 | Low | Trim redundant build properties and unused test package references. |
| TOOL-15 | Low | Remove `verify-samples` and unused path properties, or add the samples they refer to. |
| TOOL-16 | Low | Extract the shared repo-root lookup in dev-cli commands. |
| TOOL-17 | Low | Add developer documentation, or remove the readme link to `documentation/`. |

Overlaps to merge when creating tasks:

- Cancellation: MTP-1, CORE-11, MTP-5 are one piece of work (thread `CancellationToken` through runner and adapter, produce Cancelled).
- Filter duplication: MTP-3 and CORE-9 (single tag-match / filter helper shared by core and adapter).
- Silent no-op surface: CORE-5, CORE-4, TOOL-9, CORE-16 (near-miss signatures, ignored hooks, dead `clearCache`); CORE-5 also feeds task 039 (tautological test detection).
- Uid stability: MTP-12 and CORE-12 (overload collisions, attribute-order ordinal, argument formatting).
- Line cap: CORE-14 and TOOL-1 (split `test-runner.cs` and the two big test files).
- Dead files: TOOL-2, TOOL-4, TOOL-15 (stray script, excluded endpoints, `verify-samples`).
- Docs: TOOL-7, TOOL-10, TOOL-11, TOOL-17 (readme, test-plan, agent.md, documentation folder).
- Test coverage: CORE-17, TOOL-8, MTP-11 (core edge cases, env filter and helpers, MTP out-of-process harness).
- Perf: CORE-15 is the first optimization candidate once task 040 (benchmarks) has a baseline.

## Findings

## Core library (source/timewarp-jaribu)


### CORE-1: Async test failures are classified Error, not Failed
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:243-324`
- What: Only `TargetInvocationException` maps to Failed, and `Invoke` throws it only for synchronous throws. A failing `async Task` test or Setup surfaces through `await testTask` as a plain exception, which falls into the generic catch and becomes Error. Tests in `core/test-runner.cleanup-outcome.cs` lock in this split (async Error, sync Failed).
- Why it matters: Nearly every real test is async, so assertion failures report as "ERROR" instead of "FAILED", contradicting the `TestNodeState` docs.
- Candidate task: Classify assertion failures as Failed regardless of sync or async throw, and keep Error for infrastructure faults.

### CORE-2: Timeout abandons the test task and leaks the delay timer
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:273-292`
- What: The timeout uses `Task.WhenAny` with an uncancelled `Task.Delay`. On timeout the test body keeps running while per-test `CleanUp` and the next test start. A later fault in the abandoned task is never observed. The timer is not cancelled when the test wins.
- Why it matters: A timed-out test can race with cleanup and later tests sharing static state, and its exceptions are lost.
- Candidate task: Replace the delay-based timeout with a `CancellationTokenSource` and `TimeProvider`, observe the abandoned task, and cancel the timer on completion.

### CORE-3: TimeoutAttribute is unvalidated and does not cover hooks
- Severity: Medium
- File: `source/timewarp-jaribu/timeout-attribute.cs:10`, `source/timewarp-jaribu/test-runner.cs:273-277`
- What: `Milliseconds` accepts zero or negative values. A value below -1 makes `Task.Delay` throw after the test has started, orphaning it and reporting Error. `Setup`, `CleanUp`, `SetupOnce`, `CleanUpOnce` and `SessionFixture` creation have no timeout.
- Why it matters: A hung hook or fixture hangs the whole run.
- Candidate task: Validate `TimeoutAttribute` input and add a timeout policy for hooks and fixture creation.

### CORE-4: Per-test hooks are silently ignored on bad signatures, while once-hooks fail fast
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:339-365`, `test-runner.cs:161-173`
- What: `Setup` and `CleanUp` with the wrong signature are skipped without a message, and `DiscoverTests` also excludes them by name. Overloads throw `AmbiguousMatchException` from `GetMethod`, which becomes a per-test Error. `ResolveOnceHook` validates strictly.
- Why it matters: A misdeclared `CleanUp` quietly stops cleaning up, and the two hook families behave inconsistently.
- Candidate task: Make per-test Setup and CleanUp resolution fail fast with the same validation as once-hooks, resolved once per class.

### CORE-5: Tests with unsupported signatures are silently not discovered
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:165-172`
- What: Discovery keeps only methods returning exactly `Task`. `Task<T>`, `ValueTask` and `async void` vanish with no diagnostic. A class with zero discoverable tests reports Success. `RunAllTestsCore` returns exit code 0 when nothing is registered (line 1066).
- Why it matters: A mistyped or mis-signed test makes CI green while nothing runs.
- Candidate task: Warn or fail on public static methods with near-miss test signatures, and treat zero executed tests as a failure by default.

### CORE-6: Session fixtures dispose in registry order, not reverse creation order
- Severity: Medium
- File: `source/timewarp-jaribu/session-fixture.cs:275-301`
- What: `EndSessionAsync` iterates the dictionary and disposes in that order. Dependent fixtures can be torn down in the wrong order, and dictionary enumeration order is not a documented contract.
- Why it matters: Dependent fixtures can fail disposal or leak resources.
- Candidate task: Track fixture creation order and dispose in reverse creation order.

### CORE-7: Global static session state is not safe for concurrent or unbalanced use
- Severity: Medium
- File: `source/timewarp-jaribu/session-fixture.cs:41-44`, `source/timewarp-jaribu/test-runner.cs:719-723`
- What: `Nesting` and `Registry` are process-wide. The runner checks `IsSessionActive` then calls `BeginSession` as two separate locked steps, so concurrent runs can both claim ownership. `EndSessionAsync` silently returns when nesting is already zero, hiding unbalanced calls. `Clear()` resets nesting for everyone.
- Why it matters: Parallel or misbalanced hosts get wrong disposal timing with no signal.
- Candidate task: Make begin-if-inactive atomic and make unbalanced `EndSessionAsync` calls observable.

### CORE-8: Dispose and diagnostic output bypasses the injected sink via the static Terminal
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:448`, `test-runner.cs:1046`, `test-runner.cs:1064`, `test-runner.cs:1087`, `source/timewarp-jaribu/test-helpers.cs:22-34`
- What: The runner and `TestHelpers` write through the static `Terminal` and `TimeWarpTerminal.Default`, while each `TerminalSink` creates its own terminal. `BannedSymbols.txt` asks for injected `ITerminal`. Session-dispose failures and the grand total never reach a caller's sink or captured terminal.
- Why it matters: Output is split across terminals and cannot be asserted in tests.
- Candidate task: Route runner diagnostics and the grand-total summary through an injectable `ITerminal` or sink.

### CORE-9: Tag matching and skip-node construction are duplicated
- Severity: Low
- File: `source/timewarp-jaribu/test-runner.cs:762-763`, `test-runner.cs:902-903`, `source/timewarp-jaribu/test-case-identity.cs:170-172`, `test-runner.cs:912-924` and `939-951`
- What: The "has tags and none match" predicate exists three times. The two skipped-node builders and the filtered-class stats block (lines 766-779) repeat near-identical code.
- Why it matters: Skip and filter rules can drift apart.
- Candidate task: Extract a single tag-match helper and a single skipped-node factory.

### CORE-10: The class-filtered path reports no skipped nodes
- Severity: Low
- File: `source/timewarp-jaribu/test-runner.cs:760-780`
- What: When class tags do not match, the run returns zero counts and an empty result list. A method-level mismatch instead reports Skipped nodes.
- Why it matters: Class-level and method-level filtering are reported inconsistently.
- Candidate task: Emit Skipped nodes for filtered classes, or document the difference.

### CORE-11: `TestNodeState.Cancelled` is never produced and there is no cancellation support
- Severity: Low
- File: `source/timewarp-jaribu/test-node-state.cs:23`, `source/timewarp-jaribu/test-run-stats.cs:47`, `source/timewarp-jaribu/terminal-sink.cs:89`
- What: Nothing in the library sets Cancelled, and no public API takes a `CancellationToken`. Sink handling and stats treatment of Cancelled are dead code unless an external MTP adapter sets it.
- Why it matters: The state and stats rules cannot be exercised by the library itself.
- Candidate task: Add `CancellationToken` support to the runner and produce Cancelled, or document it as host-only.

### CORE-12: Display names and Uids mangle or depend on formatting and reflection order
- Severity: Low
- File: `source/timewarp-jaribu/test-case-identity.cs:177`, `test-case-identity.cs:91`, `test-case-identity.cs:101`, `source/timewarp-jaribu/test-helpers.cs:15-16`
- What: Argument text uses culture-sensitive `ToString()`; arrays print as type names; `1` and `"1"` look identical; the `CaseUid` fallback builds ids from that text despite the design note. `FormatTestName` splits every capital, so `Method(Foo, Bar)` becomes `Method( Foo,  Bar)` and acronyms split into single letters. The Uid ordinal relies on `GetCustomAttributes` order, which is not guaranteed to be declaration order. `[Input(null)]` binds as a null array and is treated as zero arguments.
- Why it matters: Names can be wrong or unstable, and a one-null-argument row cannot be expressed.
- Candidate task: Format arguments invariantly, apply `FormatTestName` to the method name only, and handle `[Input(null)]` explicitly.

### CORE-13: Public API lacks XML docs and has inconsistent argument checks
- Severity: Low
- File: `source/timewarp-jaribu/timeout-attribute.cs:5-13`, `source/timewarp-jaribu/null-sink.cs:15-19`, `source/timewarp-jaribu/terminal-sink.cs:66-113`
- What: `TimeoutAttribute` has no docs. `NullSink` and `TerminalSink` interface methods have none and no `<inheritdoc/>`. `OnRunCompletedAsync` checks `stats` but not `results`; `OnRunStartedAsync` checks nothing. `SkipAttribute`, `TestTagAttribute` and `InputAttribute` do not validate arguments.
- Why it matters: IntelliSense is missing for part of the public surface and null misuse yields late NREs.
- Candidate task: Add XML docs to undocumented public members and add consistent null guards in sinks and attributes.

### CORE-14: `test-runner.cs` is over the 500-line limit and mixes responsibilities
- Severity: Low
- File: `source/timewarp-jaribu/test-runner.cs` (1135 lines)
- What: It holds registration, discovery, single-test execution, hook resolution, run orchestration, sink fan-out and grand-total rendering. `session-fixture.cs` is 426 lines; all other files are under 200.
- Why it matters: It breaks the repo's 500-line rule and is hard to review or change safely.
- Candidate task: Split `test-runner.cs` into partial files for registration, single-test execution, hooks, class run, and summary output.

### CORE-15: Reflection and allocation overhead in hot paths
- Severity: Low
- File: `source/timewarp-jaribu/test-runner.cs:231`, `test-runner.cs:236`, `test-runner.cs:273`, `test-runner.cs:341`, `source/timewarp-jaribu/session-fixture.cs:397-404`
- What: Attributes and Setup/CleanUp methods are re-resolved per test. `parameters.ToList()` runs on every `Complete` call. The fixture factory result is read via `GetProperty("Result")` although `GetAsync<T>` knows `T` and could await `Task<T>` directly.
- Why it matters: Avoidable per-test cost, and the reflection path is more fragile.
- Candidate task: Cache hook and attribute metadata per class and await `Task<T>` directly in `SessionFixture`.

### CORE-16: Smaller API and robustness points
- Severity: Low
- File: `source/timewarp-jaribu/test-runner.cs:85`, `test-runner.cs:94`, `test-runner.cs:757`, `test-runner.cs:755`, `test-runner.cs:544`, `test-runner.cs:1003-1006`, `source/timewarp-jaribu/session-fixture.cs:101`
- What: `RegisteredTestClasses` exposes the live non-thread-safe list. The class name uses `Replace("Tests", "")`, stripping every occurrence. `JARIBU_FILTER_TAG` is read implicitly. The once-hook validation message names `testClass` rather than the declaring type. The `clearCache` parameter is accepted and ignored. A sticky fixture failure rethrows the same exception instance, so the stack grows on each rethrow.
- Why it matters: Minor surprises and misleading messages.
- Candidate task: Clean up the registration surface, class-name derivation, and obsolete parameters.

### CORE-17: Test coverage gaps
- Severity: Low
- File: `tests/timewarp-jaribu/single-file-tests/`
- What: No tests found for `TestHelpers.FormatTestName` or `NullSink`; negative or zero `[Timeout]`; near-miss signatures or overloads of per-test `Setup`/`CleanUp`; argument formatting under a non-invariant culture; `[Input(null)]`; or concurrent `SessionFixture` begin/end.
- Why it matters: These behaviors are unguarded against regressions.
- Candidate task: Add tests for the uncovered edge cases listed.

No TODO, FIXME or HACK comments exist in the scope.

## Strengths
- Session fixture lifecycle is carefully designed: generation counting guards the create/end race, failures are sticky per session, dispose errors aggregate, and it is well commented.
- Cleanup failures never mask a body outcome; `FoldCleanupFailure` records both messages.
- Stable per-`[Input]` node identity is shared by discovery and execution through `TestCaseIdentity` and `ReportedTestCase`.
- Once-hooks validate signatures up front and report a bad signature against the same node list discovery shows.
- The sink abstraction is small and clean, `TerminalSink` has clear terminal ownership, and `TestRunStats` documents which states count as failing.

## Coverage
Fully read: `timewarp-jaribu.csproj`, `test-runner.cs`, `session-fixture.cs`, `test-case-identity.cs`, `terminal-sink.cs`, `test-helpers.cs`, `test-run-stats.cs`, `i-test-result-sink.cs`, `null-sink.cs`, `reported-test-case.cs`, `test-node-info.cs`, `test-node-state.cs`, `input-attribute.cs`, `skip-attribute.cs`, `test-tag-attribute.cs`, `timeout-attribute.cs`. Read `BannedSymbols.txt`; only skimmed `Directory.Build.props`. Not read: `readme.md`, `agent.md`, `claude.md`, `Directory.Packages.props`, `msbuild/`. Tests folder checked only by grep and selected excerpts.

## MTP adapter (source/timewarp-jaribu-testing-platform, tests/timewarp-jaribu-mtp-validation)


### MTP-1: Cancellation token never observed
- Severity: High
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:74-161`
- What: `ExecuteRequestAsync` never reads `context.CancellationToken`. The core `TestRunner` has no `CancellationToken` parameter anywhere, so the adapter cannot pass one down. The class loop at line 90 and the run call at line 140 ignore it. `CreateTestSessionContext.CancellationToken` and `CloseTestSessionContext.CancellationToken` are also unused.
- Why it matters: Ctrl+C, `--timeout` and IDE stop requests will not interrupt a long run until the current class finishes.
- Candidate task: Thread `CancellationToken` through `TestRunner.RunTestsAsync` and the MTP adapter.

### MTP-2: Unhandled-exception handler rethrows after publishing an Error node
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:152-156`
- What: The catch block publishes an Error node and then rethrows. The publish itself can throw and replace the original exception. The synthetic uid is a fresh `Guid` per call, so the node is a one-off with no `Method` or timing, published with no InProgress or Discovered state before it.
- Why it matters: The host sees both an error node and a crashed framework, which gives duplicate and confusing failure reporting.
- Candidate task: Guard the failure publish and decide whether to rethrow or only report.

### MTP-3: Filter exceptions in discovery and run are classified inconsistently
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:98-149`
- What: Discovery applies the class, tag, method and uid filters itself. The run path delegates to `TestRunner.RunTestsAsync`. The class-tag omission is duplicated in the adapter as `ClassOmittedByTagFilter` (lines 163-168) and in core. The class tag is matched with `GetCustomAttributes<TestTagAttribute>()`, which defaults to inherited lookup, so core and adapter must agree on `inherit`.
- Why it matters: A rule changed in one place makes discovery list nodes that the run never produces, or the reverse.
- Candidate task: Expose a single core filter helper used by both discovery and run, and add a test that compares them.

### MTP-4: Per-node `PropertyBag` and TimingProperty are synthesized, not measured
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/mtp-sink.cs:84-89`
- What: Start time is computed as `UtcNow - Duration` at publish time, not recorded when the test started. Skipped, InProgress and Discovered nodes get no timing.
- Why it matters: IDE and TRX start timestamps are slightly wrong and non-monotonic under load.
- Candidate task: Carry real start and end timestamps on `TestNodeInfo` and use them in the sink.

### MTP-5: Cancelled state reported as Error with a synthetic exception
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/mtp-sink.cs:152-155`
- What: Cancelled maps to `ErrorTestNodeStateProperty` wrapping a new `OperationCanceledException`. The stack trace is the sink's, not the test's. The repo pins MTP 2.3.3.
- Why it matters: Cancelled tests show as errors with a misleading stack trace.
- Candidate task: Check whether `CancelledTestNodeStateProperty` exists in MTP 2.3.3 and map to it, or document the limitation.


### MTP-6: Tree-node filter matches against a flat uid with an empty property bag
- Severity: High
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:231-232`
- What: `TreeNodeFilter.MatchesFilter` is called with the node uid (`Namespace.Type.Method[0]`) as the path and an empty `PropertyBag`. MTP tree filters expect a slash-separated path such as `/Assembly/Namespace/Type/Method` and evaluate property predicates against the bag.
- Why it matters: `--treenode-filter` (what IDEs and `dotnet test` send) will select nothing or the wrong nodes, and property filters can never match.
- Candidate task: Build the MTP tree path and real property bag for each case before calling TreeNodeFilter, and add a test that uses --treenode-filter.

### MTP-7: Test framework advertises no capabilities
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/jaribu-capabilities.cs:5`
- What: `Capabilities` returns an empty collection, so the framework declares no tree-node-filter, graceful-stop, or other optional capability. The class also has no XML docs or Purpose region.
- Why it matters: Hosts that gate features on capabilities (tree filter, graceful stop, TRX) may refuse or ignore those options.
- Candidate task: Declare the supported MTP capabilities (tree-node filter, graceful stop) and document why each unsupported one is omitted.

### MTP-8: Packed props force Exe/test-project settings on every transitive consumer
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj:38-39` and `build/timewarp.jaribu.testingplatform.props:7-21`
- What: The same props file is packed to both `build/` and `buildTransitive/`, and it unconditionally sets `OutputType=Exe`, `IsTestProject=true`, and `IsTestingPlatformApplication=true`. Any library that references a project using the adapter inherits those settings.
- Why it matters: A class library that transitively references the adapter silently becomes an executable test project.
- Candidate task: Pack the props only to `build/` or guard the settings with a opt-in property, and add a pack test for a transitive consumer.

### MTP-9: Unknown request types are treated as a run request
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:78,136-148,217-223`
- What: `isDiscovery` is true only for `DiscoverTestExecutionRequest`; every other request type, including ones this adapter does not understand, falls into the run branch and executes tests with a null filter.
- Why it matters: A new or unsupported request type would run the whole suite instead of failing clearly.
- Candidate task: Switch on the request type explicitly and report an error node or throw for unsupported requests.

### MTP-10: PDB lookup can throw out of node publication
- Severity: Medium
- File: `source/timewarp-jaribu-testing-platform/method-source-location.cs:59,78-93`
- What: `MetadataTokens.MethodDefinitionHandle(method.MetadataToken)` and the reader calls can raise `ArgumentException` (invalid or zero token, such as dynamic or generated methods), which the four narrow catches do not cover. The exception escapes `PublishNodeAsync` and aborts the run. Embedded PDBs are also never read, only a side-by-side `.pdb`, and the swallowed failures are not logged.
- Why it matters: An optional navigation property can fail the whole test run, and silent misses give no diagnostic.
- Candidate task: Catch `ArgumentException` in source-location lookup, support embedded portable PDBs, and log lookup misses at debug level.

### MTP-11: Validation project asserts nothing and skips most adapter behaviour
- Severity: Medium
- File: `tests/timewarp-jaribu-mtp-validation/` (all files)
- What: The project holds intentionally failing tests with no harness that checks expected outcomes per node. It does not exercise `--filter-tag`, `--filter-class`, `--filter-method`, `JARIBU_FILTER_TAG`, `--list-tests` discovery, uid-list or tree-node filters, Cancelled, Setup or CleanUp throwing, session fixtures and `CloseTestSession` failure, parameter metadata, source-location property, or the Error node from an unhandled exception.
- Why it matters: Adapter regressions such as F-6 and F-9 cannot be caught by this project.
- Candidate task: Add an out-of-process harness that runs the validation project with each filter and compares reported node states to an expected table.

### MTP-12: Uids collide for overloaded methods and an unfiltered Error node uses a random uid
- Severity: Medium
- File: `source/timewarp-jaribu/test-case-identity.cs:29-33` and `jaribu-test-framework.cs:209`
- What: `MethodUid` is `{Type.FullName}.{Method.Name}` with no parameter signature, so overloads that are both discovered share a uid. The unhandled-exception node uses `Jaribu.Error.{Guid}`, so it is unstable across runs and carries no method identifier.
- Why it matters: Duplicate uids merge or overwrite nodes in MTP and IDE trees, and random uids defeat rerun and baseline comparison.
- Candidate task: Reject or disambiguate overloaded test methods in discovery and use a deterministic uid for the unhandled-error node.

### MTP-13: Empty JARIBU_FILTER_TAG is treated as an active filter
- Severity: Low
- File: `source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:170-179`
- What: The CLI value is checked with `IsNullOrWhiteSpace` but the environment fallback is returned as is, so an empty or whitespace variable yields a non-null `filterTag`. The variable is also undocumented in the option help beyond a precedence note.
- Why it matters: A blank variable in CI can exclude tagged classes or skip all methods.
- Candidate task: Normalize the environment fallback with the same whitespace check and trim it.

### MTP-14: Public hook and internal types lack XML docs and conventions
- Severity: Low
- File: `source/timewarp-jaribu-testing-platform/testing-platform-builder-hook.cs:3-8`, `jaribu-extension.cs`, `jaribu-capabilities.cs`
- What: The public `TestingPlatformBuilderHook` and its `AddExtensions` method have no XML docs and no Purpose region, and the unused second parameter is named `_`. `JaribuExtension` and `JaribuCapabilities` also have no docs or regions.
- Why it matters: This is the only public API of the package and it violates the repo's underscore and doc conventions.
- Candidate task: Add XML docs and Purpose regions to the hook, extension, and capabilities, and rename the discarded parameter.

### MTP-15: Extension version, shared uid, and hard-coded InternalsVisibleTo
- Severity: Low
- File: `jaribu-extension.cs:5-7` and `timewarp-jaribu-testing-platform.csproj:22-27`
- What: The framework and the command-line provider share the single uid `TimeWarp.Jaribu`, and the version comes from the assembly version with a `1.0.0` fallback instead of the informational or package version. The production csproj also grants internals to specific test assemblies (`run-ci-tests`, `run-tests`, `mtp-node-identity`, `timewarp-jaribu-tests-mtp`).
- Why it matters: Two extensions with one uid can trip MTP duplicate checks, MTP shows a wrong version, and the grants tie the shipping package to local test names.
- Candidate task: Give each extension its own uid, read the informational version, and move test-only InternalsVisibleTo entries behind a build condition.

### MTP-16: Static caches keep MethodInfo and PDB data for process lifetime
- Severity: Low
- File: `source/timewarp-jaribu-testing-platform/method-source-location.cs:28-29`
- What: `LocationCache` is keyed by `MethodInfo` and `MoveNextTokenByKickoffToken` by pdb path, and neither is ever cleared or bounded. A rebuilt assembly at the same path in a long-lived host (watch mode, IDE test host) returns stale data.
- Why it matters: Stale locations and unbounded growth in long-running hosts.
- Candidate task: Clear the location caches at session close or key them by assembly MVID.

### MTP-17: Smaller consistency issues in the adapter and validation project
- Severity: Low
- File: `jaribu-test-framework.cs:109`, `mtp-sink.cs:210-220`, `setup-cleanup-tests.cs:49-54`, `sample-tests.cs:97`
- What: `System.Reflection.MethodInfo` is fully qualified despite `using System.Reflection`. `FormatParameter` prints the type name for arrays and collections. The `SetupCalledMultipleTimesTest` comment says "at least 2" but the assertions check other things. `TimeoutExceededTest` awaits `Task.Delay(5000)` with no token, so the abandoned task runs on after the timeout. `Failed`, `Timeout`, and `Error` fall back to invented exceptions when none is supplied, which hides a core bug that loses the exception.
- Why it matters: Minor noise and misleading diagnostics.
- Candidate task: Clean up the minor items, and report a distinct diagnostic message when the core supplies no exception for a failing state.

## Strengths
- Discovery and run share `TestCaseIdentity`, so node uids for `[Input]` rows are deterministic and ordinal-based.
- `OnTestStartedAsync` always publishes InProgress, which avoids double-counted terminal states.
- The registered-class list is snapshotted before the loop, and `context.Complete()` sits in a finally block.
- Failure properties keep both the exception and the message, so cleanup failures are visible.
- Source-location lookup handles async and iterator methods through the state-machine kickoff map and caches results.

## Coverage
Read in full: `source/timewarp-jaribu-testing-platform/` jaribu-test-framework.cs (236 lines), mtp-sink.cs (221), method-source-location.cs (171), jaribu-command-line-options.cs (87), jaribu-capabilities.cs, jaribu-extension.cs, testing-platform-builder-hook.cs, the csproj, and `build/timewarp.jaribu.testingplatform.props`. Validation: all four .cs files and the csproj under `tests/timewarp-jaribu-mtp-validation/`. Also `source/timewarp-jaribu/test-case-identity.cs` (partial) and `Directory.Packages.props`. No file exceeds 500 lines and no TODO/FIXME comments exist in scope.

## Tooling, CI, tests, docs

| `routine-journals-gitignore` | Missing pattern `stacked-task-set*.resume.json` |
| `source-file-line-cap` | `test-runner.cs` is 1135 lines. `test-runner.session-fixture.cs` is 883. `test-runner.setup-once.cs` is 814. `work-thread-report.cs` is 515. The cap is 500 and there is no auto-fix. |
| `vscode-settings-gitignore` | `.vscode/settings.json is not kept out of git`. The re-include line `!.vscode/settings.json` keeps it tracked. |

Per your policy, I'm treating the `--fix`-able items as the intended baseline. The three that cannot be fixed that way are the line caps, `glossary.md` and the stale hook. F-1 and F-8 cover the first two.

## Findings

### TOOL-1: Three C# files exceed the 500-line cap, two of them framework tests
- Severity: High
- File: `tests/timewarp-jaribu/single-file-tests/core/test-runner.session-fixture.cs:1`, `tests/timewarp-jaribu/single-file-tests/core/test-runner.setup-once.cs:1`, `work-thread-report.cs:1`
- What: The audit marks these as blocking errors. The `test-runner.cs` cap violation is in `source/`, so it is out of my scope. The two test files each bundle many fixtures and meta-tests.
- Why it matters: The audit blocks the PR gate, and the large files are hard to review.
- Candidate task: Split session-fixture and setup-once test files by responsibility to get under the 500-line cap.

### TOOL-2: `work-thread-report.cs` is a stray, unrelated script
- Severity: High
- File: `work-thread-report.cs:1`
- What: It is a "GitHub Work Thread Analyzer" (`gh api` GraphQL across repos). Nothing in the repo references it, and CI never builds it. It uses the old `NuruAppBuilder`/`AddRoute` API, probably incompatible with Nuru 3.0.0-beta.75. It is 4-space indented, uses emoji output, and calls `Console.WriteLine` directly, which `BannedSymbols.txt` bans.
- Why it matters: It is dead code from an unrelated tool, and it contributes one of the audit's line-cap failures.
- Candidate task: Remove `work-thread-report.cs` from the repo, or move it to a personal tooling repo.

### TOOL-3: Release publishing has no tag-to-version guard and can push stale packages
- Severity: High
- File: `tools/dev-cli/endpoints/workflow-command.cs:369-475`
- What: The release path never checks that the release tag matches `<Version>`. The version is read by string-searching `source/Directory.Build.props`. A NuGet.org 5xx or 404 counts as "available". The push step globs every `*.nupkg` in `artifacts/packages`. `dotnet clean` does not empty that folder, and the push has no `--skip-duplicate`. Stale packages could be pushed, or a partial publish could fail midway.
- Why it matters: This is the path that publishes to NuGet.org.
- Candidate task: Make `dev workflow` release fail unless the tag matches the version, and push only the two freshly packed packages.

### TOOL-4: The excluded `check-version` command is dead code, and the workflow re-implements it
- Severity: Medium
- File: `tools/dev-cli/Directory.Build.props:19`, `tools/dev-cli/endpoints/check-version-command.cs:1`
- What: The Compile item excludes `clean-command.cs`, `check-version-command.cs` and `self-install-command.cs`. Those three files are never compiled, though `readme.md:550` and `dev.cs:13` imply `self-install` and `clean` exist. The services registered in `dev.cs:30-33` appear to come from `TimeWarp.Nuru.DevCli`. The release workflow ignores the check-version command and does its own version parsing instead.
- Why it matters: Dead code can drift, and it hides the fact that the tag-versus-version check is never used.
- Candidate task: Delete the three excluded endpoint files, or re-enable them and have `workflow` call the check-version logic.

### TOOL-5: CI runs an overlapping but different test set than `dev test`
- Severity: Medium
- File: `tools/dev-cli/endpoints/test-command.cs:36-49`, `tools/dev-cli/endpoints/workflow-command.cs:337-338`
- What: `dev test` runs only the ci-runner runfile. The workflow also runs the MTP runner through `dotnet test`. A local pass can therefore miss an MTP failure. Neither path runs `tests/timewarp-jaribu-mtp-validation`, which is built but never executed. `readme.md:550` says `./bin/dev test` is "the CI-safe subset" without mentioning the gap.
- Why it matters: Developers get a false "tests pass" signal, and the MTP adapter validation has no automated run at all.
- Candidate task: Make `dev test` run both runners, and decide how to run the MTP validation project.

### TOOL-6: Workflow uses weak CI practices
- Severity: Medium
- File: `.github/workflows/workflow.yml:42-57`
- What:
  - Only `ubuntu-latest` runs. A library with Windows and macOS path notes and `Terminal` output has no OS matrix.
  - `id-token: write` is granted to every run, including PR runs.
  - There is no `timeout-minutes`, `concurrency` group or NuGet cache.
  - The API key is passed as a command-line argument (line 16), so it is visible in the process list. The runner masks it in logs, though.
  - The path filter skips docs-only PRs. A required status check would then wait forever.
  - `upload-artifact` is on v4 while checkout and setup-dotnet are on v6 and v5.
- Why it matters: These are small gaps, but each is a real CI hygiene or secrets-handling weakness.
- Candidate task: Harden the workflow with an OS matrix, a job timeout, concurrency, and `id-token: write` only on release.

### TOOL-7: `readme.md` contradicts the code and repo in several places
- Severity: Medium
- File: `readme.md:18`, `readme.md:82`, `readme.md:79`, `readme.md:556`, `readme.md:554`, `readme.md:544`
- What:
  - Line 18 says "Only Shouldly" is a dependency. The package also references TimeWarp.Amuru and TimeWarp.Terminal (`source/timewarp-jaribu/timewarp-jaribu.csproj`).
  - Line 82 uses `using static TimeWarp.Jaribu.TestHelpers;`, but `RunAllTests` and `RegisterTests` are on `TestRunner`. The sample would not compile as written.
  - Line 79 uses the shebang `#!/usr/bin/env dotnet run`, while the rest of the repo uses `#!/usr/bin/dotnet --`.
  - Line 556 says "MIT License", but the `license` file is the Unlicense, as is `PackageLicenseExpression`.
  - Line 554 links `CONTRIBUTING.md`, which does not exist.
  - Line 544 points to `documentation/`, which holds only `.gitkeep`.
  - Line 256 shows `Version="*"`.
  - Code samples use 4-space indent, against the 2-space convention.
- Why it matters: These are the first things a new user hits, and the sample code fails.
- Candidate task: Correct readme dependency, license, sample code and dead-link claims.

### TOOL-8: Several framework features have no test
- Severity: Medium
- File: `tests/timewarp-jaribu/single-file-tests/` (whole suite)
- What: I grepped the suite and found no test for these.
  - `JARIBU_FILTER_TAG`: it appears only in `test-plan.md`.
  - `TestHelpers` (`FormatTestName`, `TestPassed`, `TestFailed`, `TestSkipped`): no references.
  - The `clearCache` parameter: the source discards it (`test-runner.cs:1005-1006, 1021-1022`) and the docs still describe it.
  - `RunTests<T>` exit-code mapping: only an indirect case.
  - Direct `TestCaseIdentity` unit tests: only indirect coverage through `input-identity`.
- Why it matters: An env-var-driven filter and the public helper API can regress unnoticed.
- Candidate task: Add tests for `JARIBU_FILTER_TAG`, `TestHelpers`, and the `RunTests<T>` exit-code mapping.

### TOOL-9: `clearCache` is a documented no-op in public API
- Severity: Medium
- File: `source/timewarp-jaribu/test-runner.cs:1000-1022` (read for context), `tests/timewarp-jaribu/test-plan.md:27`
- What: `RunTests` and `RunAllTests` accept `clearCache` and discard it. The XML doc still says "Whether to clear .NET runfile cache before running tests." `test-plan.md` lists cache clearing as an in-scope objective.
- Why it matters: The parameter promises behavior the code does not deliver.
- Candidate task: Remove the `clearCache` parameter, or mark it obsolete and fix its docs.

### TOOL-10: `test-plan.md` is stale
- Severity: Medium
- File: `tests/timewarp-jaribu/test-plan.md:15`, `:106`, `:137`, `:80`
- What: It refers to `tests/TimeWarp.Jaribu.Tests/`, to files `jaribu-08`/`09`/`10` and to "existing Nuru tests". It carries an unresolved "debate" note on skip semantics. It lists ValueTask as "future", while the edges test now asserts ValueTask is not run. Its "Next Steps" and the 90% coverage target are not followed.
- Why it matters: It misdescribes the suite's layout and goals.
- Candidate task: Rewrite or delete `test-plan.md` to match the current suite.

### TOOL-11: `agent.md` has stale paths and details
- Severity: Low
- File: `agent.md:61`, `agent.md:123`
- What: It links `source/TimeWarp.Jaribu.TestingPlatform/MtpSink.cs`, but the real path is `source/timewarp-jaribu-testing-platform/mtp-sink.cs`. It says `<Version>` is in the root `Directory.Build.props`, but it lives in `source/Directory.Build.props`. It also says "net10.0 (preview)" and shows no `dev` command for `dotnet test`.
- Why it matters: Agents and contributors follow broken links.
- Candidate task: Fix the stale paths and version location in `agent.md`.

### TOOL-12: `.editorconfig` has a copy-paste comment and invalid charset
- Severity: Low
- File: `.editorconfig:35`, `.editorconfig:440`, `.editorconfig:442-443`
- What: `charset = "utf-8"` is quoted, which is the audit failure above. The comment "This IS the TimeWarp.Nuru repo" is wrong for this repo, and `nuru.severity = off` is used even though dev-cli references Nuru. The section list at line 35 ends with `gitignore,}`, which has a trailing empty entry. The `IDE0005` entry duplicates the `NoWarn` in the root props.
- Why it matters: The comment misleads, and the audit marks the file as failing.
- Candidate task: Fix `.editorconfig` charset, stale Nuru comment and section glob.

### TOOL-13: Several `.githooks` risks
- Severity: Low
- File: `.githooks/pre-commit.cs:11-15`, `.githooks/pre-push.cs:16-20`
- What:
  - `Git.FindRoot() is null` returns 0, so the hook silently allows the action instead of failing closed.
  - A runfile hook needs `dotnet` and a NuGet restore. In an offline or broken SDK state the hook fails with a build error, which is noisy and may encourage `--no-verify`.
  - `post-checkout` and `post-commit` swallow `ganda repo attest` failures on purpose, so attestation can be missing without warning.
  - The extensionless names are symlinks to the `.cs` files, and the audit calls `pre-push.cs` outdated.
- Why it matters: The master-protection hooks can fail open without any message.
- Candidate task: Make master-protection hooks fail closed with a message, and refresh `pre-push.cs`.

### TOOL-14: Build props and package pins have issues
- Severity: Low
- File: `Directory.Build.props:48`, `Directory.Packages.props`, `tests/Directory.Build.props:12`
- What:
  - `ManagePackageVersionsCentrally` is set in both `Directory.Build.props` and `Directory.Packages.props`. The first is redundant.
  - `WarningLevel` 5 is the C# compiler default and `AnalysisMode`/`AnalysisLevel` are both set, which is partly redundant.
  - The test props `NoWarn` list is long and project-wide.
  - Package pins are exact, with preview pins for `TimeWarp.Nuru` beta.75, `TimeWarp.Amuru.Tools` beta.2 and `TimeWarp.SourceGenerators` beta.11. I did not check them against NuGet for newer versions.
  - `TimeWarp.Amuru.Tools` is referenced by dev-cli and the hooks only.
  - Every test project references `TimeWarp.Nuru`, but the tests do not obviously use it.
  - Strengths: `TreatWarningsAsErrors`, nullable, `AnalysisMode` All and banned-API analyzers are on.
- Why it matters: Redundant settings add noise, and a blanket Nuru reference adds dependency weight to tests.
- Candidate task: Trim redundant build properties and unused test package references.

### TOOL-15: `verify-samples` always fails
- Severity: Low
- File: `tools/dev-cli/endpoints/verify-samples-command.cs:30-35`
- What: The `samples/` directory does not exist and nothing calls the command, so it exits 1 on any run. `msbuild/repository.props:8-10` defines `SamplesDirectory`, `BenchmarksDirectory` and `RunfilesDirectory`, which also do not exist. The workflow-command repo-root lookup (`AppContext.BaseDirectory/../../../..`) is fragile, while other commands use `Git.FindRoot()`.
- Why it matters: The command and the path props describe things that are not there.
- Candidate task: Remove `verify-samples` and unused path properties, or add the samples they refer to.

### TOOL-16: `dev.cs` has encoding-looking banner and repeated boilerplate
- Severity: Low
- File: `tools/dev-cli/dev.cs:2-16`
- What: The banner lines render as `═` (valid UTF-8) but print as garbled `�` in some tools; the file's first byte check shows valid UTF-8, so this is a display issue, not corruption. Each command repeats the same `FindRepoRoot` method. There are no TODO/FIXME comments anywhere in scope.
- Why it matters: Duplicated boilerplate raises maintenance cost.
- Candidate task: Extract the shared repo-root lookup in dev-cli commands.

### TOOL-17: `claude.md` and the `documentation/` folder are essentially empty
- Severity: Low
- File: `claude.md:1`, `documentation/.gitkeep`
- What: `claude.md` is a one-line `@agent.md` import. The `documentation/` folder is empty, though the readme links to it as "developer documentation".
- Why it matters: The documented docs location has no content.
- Candidate task: Add developer documentation, or remove the readme link to `documentation/`.

## Strengths
- The CI-safe compile list lives in one props file (`ci-safe-tests.props`) shared by the ci-runner, mtp-runner and run-tests, so all runners compile the same set.
- The 15 test files cover discovery, tags, skip, parameterization, Setup/CleanUp, SetupOnce, session fixtures, timeouts, sinks and MTP node identity, with intentional failures isolated in nested fixtures.
- The build is strict: warnings as errors, nullable, analyzers, banned APIs, and central package management.
- Release publishing uses OIDC trusted publishing with a dispatch-only probe mode, rather than a stored API key.
- Dev-cli commands are small and consistent, and they carry Purpose and Design regions.
- The `agent.md` and skill docs accurately describe the SetupOnce and session-fixture behavior I compared against the readme.

## Coverage
I read these files in full or in the relevant parts:
- **Root config and workflow:** `.github/workflows/workflow.yml`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `BannedSymbols.txt`, `timewarp-jaribu.slnx`, `msbuild/repository.props`. Also `.editorconfig` (head, plus the `[ganda.audit]` section), `.gitignore` (the vscode and bin rules) and `.vscode/*`.
- **Hooks:** `.githooks/` (all five hooks and `Directory.Build.props`).
- **Dev CLI:** all of `tools/dev-cli/` (`dev.cs`, `global-usings.cs`, the seven endpoints, `Directory.Build.props`).
- **Tests:** `tests/Directory.Build.props`, the three nested props files, `ci-safe-tests.props`, the mtp-runner csproj, both runfile runners and `test-plan.md`. I sampled `test-runner.edges.cs`, and I grepped across the other single-file tests for coverage rather than reading each in full.
- **Docs:** `readme.md`, `agent.md`, `claude.md`, `skills/tw-jaribu/SKILL.md` (sections), `work-thread-report.cs` (first 60 lines).
- **Assets:** `assets/` holds `logo.png`, `timewarp-jaribu-avatar.svg` and five mascot PNGs under `assets/mascot/`. `documentation/` holds only `.gitkeep`.
- **Not read:** `.envrc` (a permission check blocked the read) and `source/` (out of scope). For source I only grepped the public API and `test-runner.cs:155-200, 1000-1022`.

## Recommendations

1. Fix the MTP tree-node filter (MTP-6) and cancellation (MTP-1 group) first; they affect every IDE and `dotnet test` user.
2. Fix the packed props leak (MTP-8) before the next package release.
3. Add the release tag guard (TOOL-3) before the next release.
4. Make silent no-ops loud (CORE-5 group) alongside task 039.
5. Apply `ganda repo audit --fix`, remove `work-thread-report.cs`, and split the three over-cap files to clear the audit.
6. Correct the readme sample and license claims (TOOL-7).
7. Defer perf items (CORE-15) until task 040 has a baseline.

## References

- Task 039: Detect and flag tautological tests
- Task 040: Benchmark Jaribu against TimeWarp.Fixie, TUnit, and xUnit
- Microsoft.Testing.Platform 2.3.3 (pinned in `Directory.Packages.props`)
