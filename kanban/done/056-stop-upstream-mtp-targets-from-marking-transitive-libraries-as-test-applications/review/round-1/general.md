# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** branch vs `5233a36`: `.editorconfig`, `readme.md`, `build/timewarp.jaribu.testingplatform.props`, new `buildTransitive/timewarp.jaribu.testingplatform.targets`, adapter csproj pack items, `tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs`.

## Summary

The adapter now packs `buildTransitive/timewarp.jaribu.testingplatform.targets`. That file sets `IsTestingPlatformApplication=false` unless the direct-reference marker (`_JaribuTestingPlatformDirectReference`, set by the `build/` props) is present or `EnableJaribuRunner=true`. When the project opts in, it also sets `GenerateProgramFile=false` and registers the builder hook. This mirrors MSTest's `EnableMSTestRunner` gate. The gate is in `.targets`, so it sees `EnableJaribuRunner` from the project body. The extended sandbox test covers three cases: the direct consumer (Exe, flag true, entry point generated), the plain transitive library (Library, flag false, no entry point) and the opt-in library (flag true, entry point generated). It also checks the nupkg layout. The readme documents `EnableJaribuRunner`. The `[ganda.audit]` prune of `buildTransitive` is justified (NuGet needs that exact name) and carries a reason and task id, so it is not a check silenced to make it pass. Risk is low.

Verification run by this reviewer: `dotnet tests/timewarp-jaribu/single-file-tests/mtp/testing-platform-props.transitive-consumer.cs`. Result: 1/1 passed (43 s). Evaluated properties: the transitive library has `IsTestingPlatformApplication=false`, the opt-in library has `true` with `OutputType=Library`, and the direct consumer has `Exe` with `IsTestProject` and the flag both `true`. This confirms that NuGet still imports the `build/` props for the direct reference now that `buildTransitive/` also exists. No in-repo project imports the adapter props (the MTP runner uses project references), so in-repo test runs are unaffected.

## Issues

None.

## Observations (not findings)

- An opt-in project (`EnableJaribuRunner=true` without a direct reference) stays `OutputType=Library`. The readme does not say that a runnable test host also needs `OutputType=Exe`. MSTest's `EnableMSTestRunner` behaves the same way. The task asked only for the property to be documented, so this is not raised as a finding.
- A transitive project that sets `IsTestingPlatformApplication=true` itself, without `EnableJaribuRunner`, is forced back to `false`. This matches MSTest's gate, and `EnableJaribuRunner` is the documented opt-in.
