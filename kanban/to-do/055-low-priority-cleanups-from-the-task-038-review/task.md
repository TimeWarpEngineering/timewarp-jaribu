# Low-priority cleanups from the task 038 review

## Description
Grab bag of small items that do not justify separate tasks.
Findings in task 038 report: CORE-8 (runner diagnostics bypass injected sink via static Terminal), MTP-2 (unhandled-exception handler publishes Error node then rethrows; guard the publish), MTP-4 (start timestamps synthesized as UtcNow minus duration; carry real timestamps on `TestNodeInfo`), MTP-10 (PDB lookup can throw `ArgumentException` out of node publication; support embedded PDBs; log misses), MTP-15 (framework and CLI provider share one extension uid; version read from assembly version; hard-coded `InternalsVisibleTo` for test assemblies), MTP-17 (fully-qualified `MethodInfo`, misleading test comment, `Task.Delay(5000)` without token, invented fallback exceptions), TOOL-13 (git hooks return 0 when repo root not found; attest failures swallowed), TOOL-14 (redundant `ManagePackageVersionsCentrally`, `WarningLevel`, unused Nuru reference in tests), TOOL-16 (repeated `FindRepoRoot` in dev-cli commands).
## Checklist
- [ ] CORE-8
- [ ] MTP-2
- [ ] MTP-4
- [ ] MTP-10
- [ ] MTP-15
- [ ] MTP-17
- [ ] TOOL-13
- [ ] TOOL-14
- [ ] TOOL-16

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
