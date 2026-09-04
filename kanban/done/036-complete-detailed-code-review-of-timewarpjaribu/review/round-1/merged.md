# Round 1 — merged findings
**Date:** 2026-09-04
**Sources:** core-runner, fixtures, sinks, mtp, tests-infra, security
**Pinned SHA:** `e5ef3209e54b6eb0102075e8593c37b9ce571b56`

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 5 | 0 | 0 |
| suggestion | 9 | 1 | 0 |
| nit | 2 | 1 | 0 |

Evaluate: product bugs/suggestions filed as children **036-001** / **036-002** / **036-003** (still `open` here until those land). Same-branch docs nits **M15** and **M16** are `fixed`. No `wontfix`.

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.cs:167
- Description: Every `[Input]` case reuses the method-level `Uid` (`{FullName}.{method.Name}`) while only `DisplayName` / `Parameters` differ (`test-runner.cs:167`, `772–788`). `DiscoverTests` returns methods only (`test-runner.cs:137–148`). MTP discovery publishes one `Discovered` node per method with that same Uid (`jaribu-test-framework.cs:95–115`) — no Input expansion. Run then emits N start/complete pairs under one Uid. Discovery/run counts diverge (live: `--filter-class Parameterized` listed 6, ran 7; `--filter-method MultipleInputs` ran 2 for one discovered method). Under MTP, repeated publishes to one `TestNodeUid` last-write-wins, so an earlier failing Input can be hidden by a later passing Input. `dotnet run` + `TerminalSink` still counts each result in `TestRunStats`, so dual-path outcomes diverge.
- Suggestion: Give each Input a stable distinct Uid (ordinal or formatted args) in both discovery and run, and expand Inputs on the discovery path so listed nodes match run-produced nodes one-to-one.
- Source: core-runner, mtp
- Disposition notes: Filed as child **036-001**.

### M2 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.cs:689
- Description: When `RunTestsAsyncCore` owns a session-of-one, `EndSessionAsync` is awaited in `finally` with no host-boundary catch (`test-runner.cs:689–694`). `RunAllTests` catches dispose failures, prints, and folds them into exit code 1 without masking a body exception (`test-runner.cs:852–871` — the **031** fix). Process-entry `RunTests<T>()` delegates to `RunTestsAsync` and only maps `stats.Success` to 0/1 (`test-runner.cs:823–829`), so a session-fixture dispose failure becomes an unhandled exception instead of exit code 1.
- Suggestion: Apply the same host-boundary dispose handling to `ownsSession` teardown / `RunTests<T>`: catch dispose failure, surface it, fail the run without replacing an in-flight body exception.
- Source: core-runner
- Disposition notes: Filed as child **036-002**.

### M3 — Severity: bug — Status: open
- File: source/timewarp-jaribu/test-runner.cs:239
- Description: Per-test `CleanUp` runs in a `finally` inside `RunSingleTestAsync` (`test-runner.cs:195–243`). If the body already failed or took the `Timeout` early-`return` path (`test-runner.cs:206–217`), a throwing `CleanUp` replaces that outcome: C# abandons the pending exception / timed-out `TestNodeInfo` and the outer `catch` reports only the cleanup failure (`test-runner.cs:245–269`).
- Suggestion: Capture the body outcome before `CleanUp`; run cleanup with its own try/catch; if both fail, prefer the body exception (or aggregate) and still surface cleanup failure without dropping timeout/fail state.
- Source: core-runner
- Disposition notes: Filed as child **036-002**.

### M4 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/single-file-tests/api/test-runner.multi-class-registration.cs:26-28
- Description: Dual-mode drift. Single-file mode runs via `#if !JARIBU_MULTI` → `RunTests<MultiClassRegistration_Given_>()`. In multi-mode, CI and MTP discover only `TestRunner.RegisteredTestClasses`, populated by `[ModuleInitializer]`. This class has no initializer (Design region `16–20` explains `ClearRegisteredTests()` would mutate the live `RunAllTests` list). `ci-runner/Directory.Build.props:14` still includes the file among “CI-safe test files”, and mtp-runner pulls `api/*.cs`. The multi-class registration tests pass under `dotnet run` on the file alone and are omitted from CI/`dotnet test` with no failure signal.
- Suggestion: Register for multi-mode but drive assertions through a private/local list (or snapshot — see M7) so `ClearRegisteredTests()` cannot mutate the suite under iteration; or add a dedicated CI/MTP entry that calls `RunTestsAsync<MultiClassRegistration_Given_>`.
- Source: tests-infra
- Disposition notes: Filed as child **036-003**. Optional depend on 036-002 M7 snapshot.

### M5 — Severity: bug — Status: open
- File: tests/timewarp-jaribu/multi-file-runners/mtp-runner/timewarp-jaribu-tests-mtp.csproj:12-15
- Description: MTP runner (and `multi-file-runners/Directory.Build.props:14-16`) wildcard-includes all single-file tests, including suites `ci-runner` omits because they register intentional failures as top-level tests: `test-runner.discovery.cs:74-77`, `test-runner.skip-exceptions.cs:42-64`, `test-runner.reporting-cleanup.cs:44-47`, `test-runner.parameterized.cs:75-80`, `test-runner.edges.cs:90-94` (`[Timeout(5000)]` + infinite delay). `agent.md:28-29` and `readme.md:547` document `dotnet test …/mtp-runner/` as the way to run tests; that command cannot exit 0 while those classes remain ModuleInitializer-registered.
- Suggestion: Point mtp-runner (and optionally the full multi-file runner) at the CI-safe include list; keep intentional-failure / adapter demos in `tests/timewarp-jaribu-mtp-validation/`; rewrite the five scenario files as nested-fixture meta-tests if their behavior should gate CI (M12).
- Source: tests-infra
- Disposition notes: Filed as child **036-003**.

### M6 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.cs:141
- Description: `DiscoverTests` accepts any `public static` method returning `Task`, including open generic method definitions. Those cannot be invoked without `MakeGenericMethod` and fail at `method.Invoke` (`test-runner.cs:198`). The edges suite ships such a method (`test-runner.edges.cs:32–36`).
- Suggestion: Exclude `method.IsGenericMethodDefinition` (and optionally require parameterless-or-`[Input]` arity) in `DiscoverTests`.
- Source: core-runner
- Disposition notes: Filed as child **036-002**.

### M7 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.cs:889
- Description: `RunAllTestsCore` enumerates `RegisteredTestClasses` live without snapshotting. Mutating registration during a multi-class run throws or observes a torn list. Ordinary ModuleInitializer registration is fine; the footgun remains (and is why M4 avoided `[ModuleInitializer]`).
- Suggestion: Snapshot with `RegisteredTestClasses.ToArray()` once at the start of `RunAllTestsCore`.
- Source: core-runner
- Disposition notes: Filed as child **036-002**.

### M8 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/test-runner.cs:671
- Description: Stats aggregation counts Passed / (Failed|Error|Timeout) / Skipped only. `TestNodeState.Cancelled` is a public enum member and is rendered by TerminalSink (`terminal-sink.cs:71`, `:119`, `:136`) but omitted from counts, so `TotalTests` undercounts vs `results.Count` and `Success` stays true (exit 0). MtpSink maps Cancelled to `ErrorTestNodeStateProperty` (`mtp-sink.cs:109`). TestRunner does not emit Cancelled today — latent dual-path drift.
- Suggestion: Fold Cancelled into the failing bucket (same as Timeout/Error), or add CancelledCount and treat any non-zero as `Success == false`.
- Source: sinks
- Disposition notes: Filed as child **036-002**.

### M9 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu/terminal-sink.cs:140
- Description: Message truncation uses `message.AsSpan(0, MaxMessageWidth - 3)` whenever `message.Length > MaxMessageWidth`. For `maxMessageWidth < 3` this throws `ArgumentOutOfRangeException`. The public constructor accepts any int (`terminal-sink.cs:20`) with default 50.
- Suggestion: Clamp width to at least 3 (or skip truncation when width is too small) in the constructor or at the truncation site.
- Source: sinks
- Disposition notes: Filed as child **036-002**.

### M10 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:62
- Description: `PublishNodeAsync` only attaches a `TestNodeStateProperty` and optional `TimingProperty`. It never publishes `TestMethodIdentifierProperty` or `TestFileLocationProperty`, so IDE hosts that rely on those MTP properties get display names only. Parameters from `TestNodeInfo` are dropped at the bus boundary.
- Suggestion: Add `TestMethodIdentifierProperty` (and `TestFileLocationProperty` when a path/span is available). Optionally surface `[TestTag]` as metadata so tree/trait filters can use real traits.
- Source: mtp
- Disposition notes: Filed as child **036-001**.

### M11 — Severity: suggestion — Status: open
- File: source/timewarp-jaribu-testing-platform/jaribu-test-framework.cs:78
- Description: Filter behavior is implemented and live-checked, but there is no automated assertion for `--filter-class` omit semantics under MTP, CLI `--filter-tag` winning over `JARIBU_FILTER_TAG` (`ResolveFilterTag` lines 152–161), or `MtpSink.OnTestStartedAsync` always publishing `InProgress` (`mtp-sink.cs:38-47`).
- Suggestion: Add focused MTP tests covering filter-class omission, CLI-over-env tag precedence, and the InProgress-on-start contract.
- Source: mtp
- Disposition notes: Filed as child **036-001**.

### M12 — Severity: suggestion — Status: open
- File: tests/timewarp-jaribu/multi-file-runners/ci-runner/Directory.Build.props:9-14
- Description: Coverage gap (intentional per `agent.md` until rewritten). On disk but not in CI: `core/test-runner.discovery.cs`, `core/test-runner.edges.cs`, `core/test-runner.parameterized.cs`, `core/test-runner.skip-exceptions.cs`, `output/test-runner.reporting-cleanup.cs`. Those files `RegisterTests` themselves and embed failing/timeout methods, so they cannot be CI-safe without a rewrite. Discovery, `[Input]`, `[Skip]`/exception unwrap, reporting summary, and edge/timeout behavior have no green CI gate.
- Suggestion: Rewrite each as nested-fixture meta-tests (same pattern as `test-runner.setup-once.cs`) and add them to the ci-runner include list once they report Success.
- Source: tests-infra
- Disposition notes: Filed as child **036-003**.

### M13 — Severity: suggestion — Status: open
- File: tools/dev-cli/endpoints/workflow-command.cs:104-106
- Description: Release pipeline is `clean -> build -> check-version -> pack` with no test step. PR/merge CI runs ci-runner; a `release` event can pack and push without re-running tests. `agent.md:114-117` describes build → ci-runner → publish on release.
- Suggestion: Invoke the same ci-runner step (and optionally a CI-safe MTP project) before pack/push in `RunReleaseWorkflowAsync`.
- Source: tests-infra
- Disposition notes: Filed as child **036-003**.

### M14 — Severity: suggestion — Status: open
- File: .github/workflows/workflow.yml:7-14
- Description: Push/PR `paths` filters list `source/**`, `tests/**`, `tools/**`, `.github/workflows/**`, `Directory.Build.props`, and `Directory.Packages.props`, but not root `BannedSymbols.txt` (wired from `Directory.Build.props:59-60`) or `msbuild/**` (imported at `Directory.Build.props:5`). Edits to banned APIs or repository path props can land on `master` without triggering CI.
- Suggestion: Add `BannedSymbols.txt` and `msbuild/**` to both push and pull_request path filters.
- Source: tests-infra
- Disposition notes: Filed as child **036-003**.

### M15 — Severity: suggestion — Status: fixed
- File: readme.md:232-233
- Description: Docs contradict the current tree. “Real-world example” still cites `tests/TimeWarp.Jaribu.Tests/jaribu-*.cs` and `tests/TimeWarp.Jaribu.Tests/ci-tests/`; actual layout is `tests/timewarp-jaribu/single-file-tests/` and `tests/timewarp-jaribu/multi-file-runners/ci-runner/`. Building-from-Source (`readme.md:547`) points at `dotnet test …/mtp-runner/`, which cannot pass today (M5).
- Suggestion: Update those paths to the current ci-runner / single-file-tests layout; point the source-build test command at a runner expected to pass (`./bin/dev test` / ci-runner) until M5 lands.
- Source: tests-infra
- Disposition notes: Same-branch docs fix on 036 — `readme.md` real-world example + Building-from-Source now point at `single-file-tests/` and `./bin/dev test` (ci-runner).

### M16 — Severity: nit — Status: fixed
- File: skills/tw-jaribu/SKILL.md:302
- Description: The skill filter table states `--filter-tag` non-match → **Skipped**. That matches method-level tags (`test-runner.cs:715-731`) but not class-level tags: both discovery (`jaribu-test-framework.cs:90-93`) and run omit the whole class when class tags exist and none match. `readme.md:326` already documents the omit rule.
- Suggestion: Align the skill row with README — method-level non-match → Skipped; class-level non-match → omitted from discovery and run.
- Source: mtp
- Disposition notes: Same-branch docs fix on 036 — skill filter table now matches README class-omit vs method-Skipped.

### M17 — Severity: nit — Status: open
- File: source/timewarp-jaribu/terminal-sink.cs:35
- Description: `Dispose` disposes the injected `ITerminal` whenever it implements `IDisposable`. Ownership transfer is stated on `Dispose` but not on the injecting constructor (`terminal-sink.cs:20`). Call sites that both `using` a `TestTerminal` and `using` a `TerminalSink(terminal)` rely on double-dispose tolerance.
- Suggestion: Document constructor ownership (sink owns terminal) or add an `ownsTerminal` flag / only dispose terminals created by the parameterless constructor.
- Source: sinks
- Disposition notes: Filed as child **036-002**.

### M18 — Severity: nit — Status: open
- File: tools/dev-cli/endpoints/verify-samples-command.cs:22-27
- Description: `verify-samples` is a stub (`TODO: Implement…`) that always prints success. `msbuild/repository.props:8` defines `SamplesDirectory`, but there is no `samples/` directory. Not on the CI workflow path today.
- Suggestion: Implement against real samples, or make the command no-op/exit non-zero when no samples exist instead of claiming verification succeeded.
- Source: tests-infra
- Disposition notes: Filed as child **036-003**.

## Duplicates / conflicts

- core-runner Issue 1 and mtp Issue 1 collapsed into **M1** (strongest severity `bug`; both cite UID reuse + no Input expansion on discovery).
- No conflicts. Fixtures and security raised zero issues; **031** items were re-checked and not re-opened (generation guard, Clear live-instance throw, `RunAllTests` dispose → exit 1, discovery class-tag omission, dead catch removed). Timeout abandonment (`test-runner.cs:204–217`) remains the documented caveat, not a new finding.
