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

## Checklist

- [ ] Pins at beta.9, build clean with zero AMURU0001
- [ ] dotnet sites on typed builders; gh sites on `Gh` builders
- [ ] Hand TRACEPARENT code removed or justified in a comment
- [ ] Span parenting confirmed in SigNoz for `dev test`
- [ ] Version bumped to 1.0.0-beta.18

## Depends on

- None. Amuru 3.0.0-beta.9 is published.

## Session

- Created: 533fd3ed-9595-4328-8934-d2075db5b31f (2026-11-18)

## Notes

Steve 2026-11-18: "Consumer repos can now bump to Amuru beta.9. Expect AMURU0001 compile
errors wherever they still call Shell.Builder("dotnet") with a verb that has a typed builder."
Sibling: ganda 670 does the same bump plus Jaribu beta.17.
