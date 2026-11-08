# Disposition — task 047

**Date:** 2026-11-09
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Round 1 used one general reviewer at effort 3. It found no bugs. The implementation meets every task requirement and matches the implementer restatement. `./bin/dev test` passed 165 of 165. An out-of-tree probe confirmed that the `[Input]` metadata decoder orders every supported argument shape. There were 2 suggestions and 3 nits, all closed as wontfix with the rationale below. None affects identity correctness.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | The hand-written blob decoder is correct and falls back safely. Moving to `DecodeValue` is a refactor that changes no behavior, so it can be a follow-up. | review oracle |
| M2 | suggestion | The metadata path and the fallback give the same order on CoreCLR, so a regression is invisible to users. The probe verified each tag. Add a guard test together with M1. | review oracle |
| M3 | nit | The repeated overload scan costs nothing measurable at current sizes. A cache would add state. | review oracle |
| M4 | nit | It takes a test that mutates its own attribute argument. Identity (ordinals) is unaffected. | review oracle |
| M5 | nit | Unquoted `char` affects display only. It belongs with task 055 argument-text work. | review oracle |

## Escalations

- None.
