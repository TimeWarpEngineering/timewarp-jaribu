# Harden the GitHub workflow

## Description
Only `ubuntu-latest` runs for a library with Windows and macOS terminal output. `id-token: write` is granted to every run including PRs. No `timeout-minutes`, `concurrency` group, or NuGet cache. The API key is passed as a command-line argument. The path filter skips docs-only PRs, so a required check would wait forever. `upload-artifact` is on v4 while other actions are on v5/v6.
Finding: TOOL-6 in task 038 report.
## Requirements
- OS matrix (ubuntu, windows, macos) for the test job.
- `id-token: write` only on the release job.
- `timeout-minutes`, `concurrency`, NuGet cache.
- Pass secrets via env, not argv.
- Make docs-only PRs produce a passing status (paths-ignore with a no-op job, or drop the filter).
- Align action versions.
## Checklist
- [ ] Matrix
- [ ] Permissions scoped
- [ ] Timeout, concurrency, cache
- [ ] Secret handling
- [ ] Docs-only PR status

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
