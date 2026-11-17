# Round 1 — merged findings
**Date:** 2026-11-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: .github/workflows/workflow.yml:104
- Description: The ubuntu matrix leg uploads `Packages-${{ github.run_number }}` as soon as that leg succeeds (lines 104–112). The other OS legs can still fail. `packages-prune` does not run on that red workflow, and locate-run ignores it (`--status success`), but the artifact stays for 7 days under the `Packages-*` name. The prune script (lines 131–149) keeps the two newest non-expired `Packages-*` artifacts in the repo and does not check the workflow run conclusion. Nuru only creates that name after every stage has passed, and it stages the bytes under a name that does not start with `Packages-`. One master run with a green ubuntu leg and a failed other leg, followed by a later green run, makes the prune keep the failed-run artifact and delete the previous green run's packages. The latest green commit still promotes. The previous green commit, which keep-last-two retains as the spare, can lose its artifact. The orphan also uses org artifact quota.
- Suggestion: Upload `staged-packages` from the ubuntu leg on `refs/heads/master` (name must not start with `Packages-`). In `packages-prune`, after every test leg has succeeded, download it, upload `Packages-${{ github.run_number }}` (`if-no-files-found: error`, `retention-days: 7`), prune, then delete `staged-packages`. Alternatively, make the prune script skip artifacts whose workflow run conclusion is not `success`.
- Source: general
- Disposition notes: Fixed in implement-fix-2 (2026-11-18). The ubuntu leg uploads `staged-packages`; `packages-prune` publishes `Packages-<run>` after every leg passed, prunes, then deletes `staged-packages`.

## Duplicates / conflicts

- One reviewer. Nothing to collapse.
