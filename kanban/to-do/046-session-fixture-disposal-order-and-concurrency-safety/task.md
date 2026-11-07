# Session fixture disposal order and concurrency safety

## Description
`EndSessionAsync` disposes fixtures in dictionary order, not reverse creation order, so dependent fixtures can be torn down first. `Nesting` and `Registry` are process-wide; the runner checks `IsSessionActive` then calls `BeginSession` as two separate steps, so concurrent runs can both claim ownership. Unbalanced `EndSessionAsync` silently returns.
Findings: CORE-6, CORE-7 in task 038 report.
## Requirements
- Track creation order and dispose in reverse.
- Atomic begin-if-inactive.
- Unbalanced end calls throw or log; `Clear()` semantics documented.
- Tests for ordering and for concurrent begin/end.
## Checklist
- [ ] Reverse-order disposal
- [ ] Atomic begin
- [ ] Observable unbalanced end
- [ ] Tests

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
