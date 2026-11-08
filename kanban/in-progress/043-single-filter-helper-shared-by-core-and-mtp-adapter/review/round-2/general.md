# Round 2 — general
**Date:** 2026-11-08
**Scope reviewed:** fix delta `5c09bbe..71e1344` (round-1 base.sha to fix commit). Product files: `jaribu-test-framework.cs`, `mtp-tree-filter.cs`, `test-filter.cs`, `timewarp-jaribu.csproj`.

## Summary

The fix pass removes core's `InternalsVisibleTo` for the adapter. `ResolveFilterTag` now returns the raw `--filter-tag` argument, or null when the option is unset. `MtpTreeFilter` compares uid lists with ordinal equality inline. `MethodTagMatches` and `UidMatches` are deleted. Risk is low. The CI runner passed 154 of 154 in this review, including FilterParity, NodeIdentity, and TransitiveConsumer.

Re-verified:

- M1 fixed. No `InternalsVisibleTo` remains in core. A grep of the adapter source finds no `TestFilter` call, so the adapter compiles only against core's public surface. Core's public `DiscoverTestCases` (`test-case-identity.cs:165`) and the class run (`test-runner.class-run.cs:208`) both call `TestFilter.ResolveTag`. A null argument reads `JARIBU_FILTER_TAG`, a set value is trimmed, and a blank explicit value means no filter. The adapter passes the same `filterTag` to discovery and to the run, so parity holds. The validator still rejects a blank CLI argument, so the adapter never forwards one.
- The inline uid compare (`Any` with `StringComparison.Ordinal`) matches the deleted `UidMatches` for a non-null list, including an empty list matching nothing. The uid list filter is never null on that switch arm.
- M2 fixed. `MethodTagMatches` is gone and nothing referenced it. `UidMatches` had only the adapter as a caller, so deleting it leaves no dead code.
- The Purpose and Design regions in `test-filter.cs`, `jaribu-test-framework.cs`, and `mtp-tree-filter.cs` match the new behavior.

## Issues

None.
