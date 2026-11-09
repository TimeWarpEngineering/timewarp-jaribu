# Disposition — task 054

**Date:** 2026-11-09
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

Round 1 (effort 3, general reviewer) found no bugs. The per-class/per-method metadata cache keeps the attribute-rejection order and propagation rules the code had before. `SessionFixture` now awaits `Task<T>` without reflection, and the existing signature check makes the removed type test redundant. The parameter copy moved from per completion to once at entry. The MTP location caches are MVID-keyed and cleared on the outermost session close. `./bin/dev test` exited 0 with 204 MTP tests passed. Both findings (one suggestion, one nit) were closed as wontfix, with the reasons below.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | Strong-keyed metadata caches keep nothing alive beyond the default ALC, which never unloads, and Jaribu does no collectible loading. A rebuilt assembly gets new keys, so nothing goes stale. This matches the existing `LocationCache` pattern. Revisit if collectible loading is added. | review orchestrator |
| M2 | nit | The outermost-close clear and the MVID key can be confirmed by reading a few lines. A test-only observable on a production cache is not worth it for this nit. | review orchestrator |

## Escalations

- None.
