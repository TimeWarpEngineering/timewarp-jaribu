# Move Jaribu to net11.0 and unblock the Amuru 3.x pin

## Description

Jaribu targets net10.0 everywhere. TimeWarp.Amuru 3.x and TimeWarp.Amuru.Tools 3.x ship only a net11.0 build, so `ganda repo audit --fix` bumps the pins to 3.x on every run and restore then fails with NU1202. Tasks 052 and 055 had to hold the pins at 2.0.0 by hand. Move the repo to net11.0 so the audit's newest-version rule and the build agree.

Scope:
- Root `Directory.Build.props` and `benchmarks/generator/Directory.Build.props`: `TargetFramework` net11.0.
- `Directory.Packages.props`: raise TimeWarp.Amuru and TimeWarp.Amuru.Tools to the newest 3.x, drop the "2.0.0 is the newest net10.0 build" comments. Check TimeWarp.Nuru / Nuru.DevCli dependency on Amuru; if Nuru 4.0.0-beta.11 still pins Amuru 2.0.0, use the newest Nuru that works with Amuru 3.x, or report the blocker in Results instead of forcing it.
- Hard-coded `net10.0` paths in tests: `testing-platform-props.transitive-consumer.cs` (project TFMs and `obj/Debug/net10.0`), `release-gate.decisions.cs` (`lib/net10.0`), `mtp-review-cleanups.cs` (`lib/net10.0`). Derive from the build TFM where practical.
- `.github/workflows/workflow.yml`: `dotnet-version` to the 11.0.x rc channel on every job, including the benchmark SDK pin step from task 040. Add `include-prerelease` or `dotnet-quality: preview` as needed until 11 GA.
- Docs: `agent.md`, `readme.md` sample, `documentation/architecture.md`.
- Runfiles (`#!/usr/bin/env -S dotnet --` files under tests/, tools/dev-cli, .githooks, benchmarks): confirm they build on SDK 11 and that any `#:property TargetFramework` is updated.
- `tools/dev-cli` self-install must still produce `bin/dev`.

Known SDK 10 vs 11 differences already seen in this repo: SDK 10 drops `WarningMessageOutputDeviceData` on the MTP output device (task 044 added a skipped-node fallback). Keep that fallback; do not remove SDK 10 compatibility code unless the test suite proves it dead.

Local SDK 11.0.100-rc.3 is installed; CI currently uses 10.0.403.

## Checklist

- [ ] Both Directory.Build.props files target net11.0
- [ ] Amuru and Amuru.Tools pinned to newest 3.x; Nuru compatibility confirmed or blocker recorded
- [ ] No hard-coded net10.0 left outside kanban/ (grep is clean)
- [ ] Workflow installs SDK 11 on every job and the benchmark pin step matches
- [ ] `./bin/dev test` passes on SDK 11 (ci-runner and MTP runner)
- [ ] `ganda repo audit --fix` then `ganda repo audit` exits 0 with no Amuru revert needed
- [ ] Docs updated (agent.md, readme.md, architecture.md)
- [ ] Results records before/after package pins and any API or behavior differences found on net11.0

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-09)

## Notes

Origin: follow-up from tasks 052 and 055, where the reviewer flagged that every `audit --fix` run re-bumps Amuru to 3.x and breaks restore on net10.0. Mentioned in `kanban/done/052-*/task.md` Results.
