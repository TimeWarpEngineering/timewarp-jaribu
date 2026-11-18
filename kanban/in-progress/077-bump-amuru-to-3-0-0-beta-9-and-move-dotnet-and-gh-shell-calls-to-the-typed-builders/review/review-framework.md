# Review framework — task 077

**Date:** 2026-11-18
**Host task:** kanban/in-progress/077-bump-amuru-to-3-0-0-beta-9-and-move-dotnet-and-gh-shell-calls-to-the-typed-builders/
**Diff scope:** branch `task/077-bump-amuru-to-3-0-0-beta-9-and-move-dotnet-and-gh` vs `master` (6 commits, product diff 21 files, +119/−131). Review artifacts under `review/` are untracked and out of the product diff.
**Plan / brief:** Bump TimeWarp.Amuru and TimeWarp.Amuru.Tools off 3.0.0-beta.5, move `Shell.Builder("dotnet")` / `Shell.Builder("gh")` call sites onto the typed builders, drop the dev CLI's hand-rolled TRACEPARENT copy, and set the Jaribu package version to 1.0.0-beta.18. The shipped pins are 3.0.0-beta.10 because `package-pin-currency` rejected beta.9. Nuru and Nuru.DevCli moved to 4.0.0-beta.16 so the dev CLI compiles.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle grok session `01a1ee4d-d3cb-7f53-a7b2-3ea0e7d05a75` (2026-11-18). Implementer: claude session `15c48a64-29ad-4af8-94a1-63cad29dc44f`. Round 2: grok session `01a1ee61-93fb-7ce2-a79f-2acbd030c188` (2026-11-18).

## Budget (by-diff)

- Lines changed: 211 (round-2 delta `448d3cc..97e4ca3`; the scope paragraph above is round 1)
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 2

**Date:** 2026-11-18
**Diff scope:** `448d3cc..97e4ca3`. Product files: `glossary.md`, `source/timewarp-jaribu/runfile-coverage.static.cs`, `tests/timewarp-jaribu/single-file-tests/core/test-runner.runfile-coverage.launch.cs`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs`, `tests/timewarp-jaribu/single-file-tests/mtp/mtp-validation-harness.cs`, `tools/dev-cli/endpoints/ci-test-suite.cs`.
**Plan / brief:** Re-check open M1, M2, and M3. Do not re-review the whole branch.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok session `01a1ee61-93fb-7ce2-a79f-2acbd030c188` (2026-11-18).

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Inspection used for one suspected gap

`dnx dotnet-inspect` on `TimeWarp.Amuru.Tools` 3.0.0-beta.9 and 3.0.0-beta.10 (local NuGet cache), plus the package XML docs. Reason: the Results claim that several calls stay on `DotNet.Builder()` because the verb builder lacks an option. That claim is checked against the shipped beta.10 surface, not by rebuilding.
