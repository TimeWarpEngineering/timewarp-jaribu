# Bump Amuru to 3.0.0-beta.9 and move dotnet and gh Shell calls to the typed builders

## Description

Amuru 3.0.0-beta.9 is on NuGet. It carries the three things this repo asked for on
2026-11-17: `TRACEPARENT` propagated to child processes from `Activity.Current` (Amuru 149),
typed `DotNet.MsBuild` / `NuGet` / `Version` builders (151), typed `Gh` builders with a span
per call (152), and the AMURU0001 analyzer (150) that reports `Shell.Builder("dotnet")` and
`Shell.Builder("gh")` as a compile error wherever a typed builder exists.

Jaribu pins `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` at 3.0.0-beta.5. Bumping will fail
the build at every raw dotnet or gh call that has a typed builder. Known sites on master
today (expect AMURU0001 at most of them):

dotnet (14):
- `source/timewarp-jaribu/runfile-coverage.static.cs:256`
- `tools/dev-cli/endpoints/ci-test-suite.cs:64`
- `tools/dev-cli/endpoints/benchmark-suite.cs:226`
- `tools/dev-cli/endpoints/benchmark-in-process.cs:68`
- `tools/dev-cli/endpoints/benchmark-measure.cs:276`
- `tools/dev-cli/endpoints/workflow-command.cs:359`
- `tests/.../mtp/testing-platform-props.transitive-consumer.cs:350`
- `tests/.../mtp/mtp-validation-harness.cs:238`
- `tests/.../mtp/mtp-node-identity.cs:428`
- `tests/.../core/test-runner.tautology.cs:377`
- `tests/.../core/test-runner.telemetry.cs:256`
- `tests/.../core/test-runner.runfile-coverage.launch.cs:77, 130, 321`

gh (3): find with `grep -rn 'Shell.Builder("gh")'`. Only the verbs the `Gh` builders cover
are reported (run list/download, api, auth token/status, issue, pr, release); others stay.

## Requirements

- `Directory.Packages.props`: `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` to 3.0.0-beta.9.
- Every AMURU0001 site moves to the typed builder. No `#pragma warning disable AMURU0001`,
  no `.editorconfig` severity change. AMURU0002 (dotnet verb with no builder) is a warning;
  if one appears, file a task in timewarp-amuru naming the verb and leave the call as is.
- `tools/dev-cli/endpoints/trace-parent-propagation.cs`: Amuru now sets `TRACEPARENT` from
  `Activity.Current` itself. Delete the hand-rolled propagation and its call sites in
  `ci-test-suite.cs` if beta.9 covers them; keep it only where a builder still bypasses Amuru.
  Verify by running `dev test` with `OTEL_EXPORTER_OTLP_ENDPOINT` set and confirming in
  SigNoz that `dotnet-cli` and `TimeWarp.Jaribu` spans parent to the `dev test` span.
- Runner coverage relaunch (`runfile-coverage.static.cs`) must keep its exact argument
  order; compare the child command line before and after with `--verbose` or an echo.
- `dev build`, `dev test` (all three suites), and the benchmark endpoints run green locally.
- Bump `<Version>` in `source/Directory.Build.props` to 1.0.0-beta.18 (consumers of
  beta.17 get the Amuru floor change via this release).

## Goal (implementer restatement)

Move Jaribu onto TimeWarp.Amuru / Amuru.Tools 3.0.0-beta.9 and make the build clean under
the new AMURU0001 analyzer by rewriting every reported `Shell.Builder("dotnet")` /
`Shell.Builder("gh")` call onto the typed `DotNet.*` / `Gh.*` builders (no pragma, no
.editorconfig downgrade). Drop the hand-rolled TRACEPARENT propagation in the dev CLI where
Amuru now does it from `Activity.Current`, or keep it with a comment where a call bypasses
Amuru. Bump the Jaribu package version to 1.0.0-beta.18.

Acceptance:
- Both pins at 3.0.0-beta.9; `dev build` passes with zero AMURU0001.
- Each former raw dotnet/gh site with a typed builder uses it; AMURU0002 sites left as-is with
  an amuru task filed.
- Coverage relaunch in `runfile-coverage.static.cs` keeps the same child argument order.
- Hand TRACEPARENT code removed or justified.
- `<Version>` is 1.0.0-beta.18.
- Touched test files pass when run alone.

Fix pass for review round 1 (M1, M2, M3): on Amuru 3.0.0-beta.10 `DotNet.Build` and `DotNet.Test`
can emit the same argv as five calls still on the base builder. Move those five to the typed
builders with the argv unchanged, fix the stale reason in the `runfile-coverage.static.cs` Design
comment, and add the typed verb builder term to `glossary.md`. Done when `./bin/dev build` passes and
the touched test files pass alone.

## Checklist

- [x] Pins at beta.9 (now beta.10), build clean with zero AMURU0001 (library, tests, dev CLI)
- [x] dotnet sites on typed builders; gh sites on `Gh` builders
- [x] Hand TRACEPARENT code removed or justified in a comment
- [x] Span parenting confirmed in SigNoz for `dev test`
- [x] Version bumped to 1.0.0-beta.18

## Depends on

- None. Amuru 3.0.0-beta.9 is published.

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Blocked — 2026-11-18T05:37:14Z
- Implement oracle (resume): implementer-claude (claude-opus-5-5), 2026-11-18 — unblocked by
  TimeWarp.Nuru.DevCli 4.0.0-beta.16; finished dev CLI gates and SigNoz check.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session 15c48a64-29ad-4af8-94a1-63cad29dc44f, max-turns 500, Done — 2026-11-18T10:57:11Z
- Review oracle: general reviewer, grok, session 01a1ee4d-d3cb-7f53-a7b2-3ea0e7d05a75, effort 2, round 1 open (M1, M2, M3) — 2026-11-18
- Review oracle: review by reviewer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1ee4d-d3cb-7f53-a7b2-3ea0e7d05a75, max-turns 120, artifacts general.md, merged.md — 2026-11-18T11:13:39Z
- Review oracle round 2: general reviewer, grok, session 01a1ee61-93fb-7ce2-a79f-2acbd030c188, effort 2, clean (M1, M2, M3 fixed) — 2026-11-18

## Results

### Changes

- `Directory.Packages.props`: TimeWarp.Amuru and TimeWarp.Amuru.Tools 3.0.0-beta.5 -> 3.0.0-beta.9,
  then 3.0.0-beta.10 on resume (the audit's `package-pin-currency` asked for the newer release).
- `source/Directory.Build.props`: Version 1.0.0-beta.17 -> 1.0.0-beta.18.
- `source/timewarp-jaribu/timewarp-jaribu.csproj`: adds `TimeWarp.Amuru.Tools` (the library's
  coverage build now uses `DotNet.Builder()`, which lives in Tools; Tools adds only NuGet.Versioning).
- Typed builder where it expresses the call exactly:
  - `test-runner.tautology.cs`: `DotNet.Build().WithProject().WithNoIncremental().WithNoLogo()`
    (`--no-incremental` is the CLI's forward of `-target:Rebuild`).
  - `workflow-command.cs`: `DotNet.MsBuild(project).WithGetProperty("Version").WithNoLogo()`
    (same argv: `msbuild <proj> -getProperty:Version -nologo`).
  - `release-promotion.cs`: `Gh.Run().List()`, `Gh.Api(endpoint)`, `Gh.Run().Download(id)`.
  - `ci-test-suite.cs` ci-runner: already `DotNet.Run()`; wrapper removed.
- Base `DotNet.Builder().WithArguments(<same args>)` where the typed builder lacks an option
  the call needs (timeout, `--artifacts-path`, MTP extension args, `run <file> --nologo`):
  `runfile-coverage.static.cs`, `ci-test-suite.cs` (MTP), `benchmark-suite.cs`,
  `benchmark-in-process.cs`, `benchmark-measure.cs`, `mtp-validation-harness.cs`,
  `mtp-node-identity.cs`, `testing-platform-props.transitive-consumer.cs`,
  `test-runner.telemetry.cs`, `test-runner.runfile-coverage.launch.cs` (3 sites).
  Gaps filed as timewarp-amuru 153.
- `tools/dev-cli/endpoints/trace-parent-propagation.cs` deleted: Amuru beta.9
  `TraceContextPropagation` sets TRACEPARENT/TRACESTATE on every child from
  `Activity.Current` unless the caller set the key (the telemetry test still sets
  TRACEPARENT explicitly, and an explicit key wins).
- No AMURU0002 hit.
- Coverage build argv (`runfile-coverage.static.cs`), before and after:
  `dotnet build <entry> --artifacts-path <dir> --nologo --verbosity minimal`. Unchanged
  because the same argument list is passed to `DotNet.Builder()`.

### Resume (2026-11-18): dev CLI unblocked

- Merged `origin/master` (task 078 had added a `package-pin-currency.hold` for both Amuru pins at
  beta.5, pointing at task 079). This task does the 079 work, so the hold and its comment are
  removed from `.editorconfig`. Task 079 (to-do) is a duplicate of this one and can be closed.
- `TimeWarp.Nuru` and `TimeWarp.Nuru.DevCli` 4.0.0-beta.15 -> 4.0.0-beta.16. Beta.16 ships the
  DevCli content on the typed builders (timewarp-nuru 598), so the dev CLI compiles under Amuru
  beta.9. The earlier block is resolved.
- `benchmarks/generator/generate-workloads.cs`: the generated `jaribu-runfile/<workload>/run.cs`
  now carries a `#region Purpose` block. Without it the benchmark runfile failed TWA0004.
- `tools/dev-cli/telemetry.md`: says Amuru sets `TRACEPARENT` on `dev test` children from the
  command span (the hand-rolled copy is gone).
- SigNoz check (`dev test` with `OTEL_EXPORTER_OTLP_ENDPOINT=https://otlp.timewarp.work`,
  `OTEL_EXPORTER_OTLP_PROTOCOL=http/protobuf`): trace `85891258fa4ad7a151353cd3e6591101`. The
  `timewarp-jaribu-dev` `test` span `43ff5998b9a9526e` is the direct parent of the dotnet-cli
  `app run`, `app`, and `app test` spans and of every `TimeWarp.Jaribu` `jaribu.class` span.

Follow-up (not in this task): `dev benchmark` in-process step fails in BenchmarkDotNet 0.15.8
validation (`GetRuntimeVersion not implemented for NotRecognized`), because BDN 0.15.8 does not know
the .NET 11 runtime. This dates from the net11.0 retarget (eaebc52), not from Amuru. Amuru launches
the process fine; BDN then fails. Fix: BenchmarkDotNet 0.16.x (0.16.0-preview.2 is the newest) or
a net10.0 in-process host. That change needs its own task and review.

### Fix pass, review round 1 (2026-11-18)

- M1: both Release builds in `test-runner.runfile-coverage.launch.cs` use one helper,
  `BuildReleaseAsync`, on `DotNet.Build().WithProject().WithConfiguration("Release").WithNoLogo()`
  (argv `build <project> --configuration Release --nologo`, the long form of `-c Release`). Same
  working directory, `WithNoValidation`, 3-minute timeout. The helper also keeps the file at 499
  lines (inline, it hit TWA0038 at 503). The `run <probe> -- …` site stays on the base builder.
  `glossary.md` gets a "Typed verb builder" row.
- M2: `ci-test-suite.cs` MTP run, `mtp-validation-harness.cs`, and `mtp-node-identity.cs` move to
  `DotNet.Test().WithProject()…WithArguments(<MTP tokens>)`. Checked against the beta.10
  `DotNetTestBuilder.Build` (decompiled): project, then `--configuration`, `--results-directory`,
  `--nologo`, then the extra tokens in call order, so each argv is unchanged. MTP's `--output Detailed`
  goes through `WithArguments`, not `WithOutput`. `WithNoValidation` and `WithTimeout` kept. The
  ci-test-suite Design comment now says the typed builder emits the same line.
- M3: `runfile-coverage.static.cs` Design comment: the base builder stays because `DotNet.Build()`
  emits `--verbosity` before `--nologo`, not because `--artifacts-path` is missing.

### How to validate

Smoke: `dotnet run --file tools/dev-cli/dev.cs -- self-install`, then `./bin/dev build`, then
`dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs`
Expect: self-install and build succeed with no AMURU0001; coverage runfile prints `Passed: 16`.

### Gates

First pass (before the block):
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.tautology.cs` -> 22/22 pass
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs` -> 7/7 pass
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` -> 16/16 pass (runs the edited `.launch.cs` and `.static.cs` partials and covers `source/.../runfile-coverage.static.cs`)
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs` -> 5/5 pass
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs` -> 1/1 pass
- `mtp/mtp-node-identity.cs`: not a standalone runfile (CS7022 on the generated MTP entry point). This predates the change. It ran inside `dev test` below.

Resume pass (no test file edited in this pass):
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` -> pass
- `./bin/dev build` -> pass (0 errors, no AMURU diagnostics)
- `./bin/dev test` with OTEL set -> pass (runfile suite exit 0; MTP 249/249). Ran once because the
  task asks for the SigNoz parenting check on `dev test` and `ci-test-suite.cs` has no other coverage.
- `./bin/dev benchmark --runs 1 --workloads startup` -> jaribu-runfile, jaribu, fixie, tunit, xunit
  startup cold and warm runs pass. The in-process BenchmarkDotNet step fails (pre-existing .NET 11
  issue, see Follow-up above).
- `ganda repo audit` (1st) -> FAIL `package-pin-currency`: Amuru and Amuru.Tools 3.0.0-beta.9 behind 3.0.0-beta.10.
  Fix: pins to 3.0.0-beta.10. Reran: `self-install` pass, `./bin/dev build` pass (no AMURU diagnostics),
  `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` -> 16/16 pass,
  `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.telemetry.cs` -> 7/7 pass.
- `ganda repo audit` (2nd) -> pass (exit 0). One advisory warning, `vscode-window-icon` (`.vscode/settings.json`
  missing), is non-blocking and predates this task.

Fix pass, review round 1:
- `./bin/dev build` (1st) -> FAIL TWA0038, `test-runner.runfile-coverage.launch.cs` 503 lines. Fix: shared
  `BuildReleaseAsync` helper (499 lines). Rerun -> pass, no AMURU diagnostics.
- `dotnet run --file tools/dev-cli/dev.cs -- self-install` -> pass (compiles the edited `ci-test-suite.cs`)
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.cs` -> 16/16 pass (runs the edited `.launch.cs` partial)
- `dotnet run --file tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs` -> 5/5 pass
- `mtp/mtp-node-identity.cs`: not a standalone runfile (CS7022 on the generated MTP entry point, pre-existing).
  It compiles in `./bin/dev build` (mtp-runner project). CI runs it.
- `ci-test-suite.cs` MTP run: no standalone test; `dev test` in CI runs it.
- `ganda repo audit` -> pass (exit 0). Advisory `vscode-window-icon` warning only, pre-existing.

### Review disposition

- Rounds: 2. Roster: general. Effort: 2.
- Round 2 re-checked M1, M2, and M3 on `448d3cc..97e4ca3`.
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0 open / 2 fixed / 0 wontfix; nit 0 open / 1 fixed / 0 wontfix.
- Disposition: clean. No wontfix. No escalation.
- Paths: `review/review-framework.md`, `review/round-2/merged.md`, `review/disposition.md`.

## Review

- Round 1 (2026-11-18): open M1, M2, M3. The fix pass closed them.
- Round 2 (2026-11-18): clean. M1, M2, and M3 fixed. No new findings.
- Round 2: Done
## Notes

Steve 2026-11-18: "Consumer repos can now bump to Amuru beta.9. Expect AMURU0001 compile
errors wherever they still call Shell.Builder("dotnet") with a verb that has a typed builder."
Sibling: ganda 670 does the same bump plus Jaribu beta.17.

## Blocked

2026-11-18: TimeWarp.Nuru.DevCli content raises AMURU0001 under Amuru 3.0.0-beta.9 so the dev CLI cannot compile; needs timewarp-nuru 598 released, then bump TimeWarp.Nuru.DevCli here https://github.com/TimeWarpEngineering/timewarp-nuru/tree/master/kanban/to-do/598-move-dev-cli-content-gh-and-dotnet-shell-calls-to-the-amuru-beta-9-typed-builders

- https://github.com/TimeWarpEngineering/timewarp-nuru/tree/master/kanban/to-do/598-move-dev-cli-content-gh-and-dotnet-shell-calls-to-the-amuru-beta-9-typed-builders

Resolved 2026-11-18: TimeWarp.Nuru.DevCli 4.0.0-beta.16 is published and pinned here.

## Learnings

- Misread: the implementer restatement's acceptance bar is narrower than the task. The task requires `dev build`, `dev test` (it says all three suites), green benchmark endpoints, and a SigNoz check that `dotnet-cli` and `TimeWarp.Jaribu` spans parent to `dev test`. The restatement's acceptance list stops at "touched test files pass when run alone" and does not name the SigNoz check or the benchmark endpoints. The recorded `## Results` still cover `./bin/dev test` (runfile exit 0, MTP 249/249), the SigNoz trace, and `./bin/dev benchmark` startup. The in-process BenchmarkDotNet step is not green; Results call that a pre-existing .NET 11 gap. `ci-test-suite.cs` documents two `dev test` targets, and says the validation project is not a third target, so the task's "three suites" does not match the current command.
