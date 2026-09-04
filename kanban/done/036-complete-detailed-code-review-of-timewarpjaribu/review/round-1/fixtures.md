# Round 1 — fixtures
**Date:** 2026-09-04
**Scope reviewed:** Session/class/assembly fixtures, SetupOnce, dispose, generation/race leftovers at SHA e5ef320

## Summary

Class-scoped `SetupOnce`/`CleanUpOnce` and session-scoped `RegisterSessionFixture`/`SessionFixture.GetAsync` match `skills/tw-jaribu/SKILL.md`: lazy once-hooks after skip/tag short-circuits, fail-fast signatures, CleanUpOnce in finally only when SetupOnce ran, synthetic CleanUpOnce failure node, and session dispose at outermost end. Task **031** follow-ups remain present (generation-guarded create/end store, Clear throws on live instances, `RunAllTests` dispose → exit 1, sticky create failure, dead catch removed). MTP `CreateTestSessionAsync`/`CloseTestSessionAsync` pairs with runfile `RunAllTests` wrap and lone `RunTestsAsync` session-of-one; no assembly-scope leftovers and no new dispose/nesting/generation holes found.

## Issues

None.
