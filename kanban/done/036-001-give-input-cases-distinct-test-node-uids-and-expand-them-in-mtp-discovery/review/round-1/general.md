# Round 1 — general
**Date:** 2026-10-21
**Scope reviewed:** `master...HEAD` (`0f9de3d`): `source/timewarp-jaribu/{test-case-identity,reported-test-case,test-node-info,test-runner}.cs`, `source/timewarp-jaribu-testing-platform/{jaribu-test-framework,mtp-sink,method-source-location}.cs`, the new meta-tests, and ci-runner props. Audit `--fix` baseline files were not reviewed.

## Summary

The change does what the goal asks on the main paths. Each `[Input]` row gets `{FullName}.{Method}[n]`, and discovery and the run share `TestCaseIdentity`. Under MTP a failing row stays visible when a later row passes. I verified this with commands. `./bin/dev test` exits 0 with 57/57. `dotnet test mtp-runner --list-tests -- --filter-class Parameterized` reports `Discovered 7 tests.`, and the run reports `total: 7, failed: 2, succeeded: 5`. Two defects remain. The bad-once-hook path still breaks discovery/run parity for `[Skip]` and tag-mismatched methods that have `[Input]` rows. The new `TestMethodIdentifierProperty` also puts the namespace into `TypeName`, which violates the MTP contract. There is one perf suggestion and two nits.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-jaribu/test-runner.cs:651 (also the Design region at source/timewarp-jaribu/test-case-identity.cs:14-17)
- Description: When a once-hook signature is invalid (`resolutionError`), the run fans out each method through `SelectInputCases`, which calls `ExpandInputs`. That path runs **before** the `[Skip]` and method-tag short-circuits in `RunTestWithSinkAsync`. Discovery (`EnumerateReportedCases`) lists a `[Skip]` or tag-mismatched method as one method-level node. In this path the run emits one `[n]` node per row, so the node sets diverge, which is the exact drift this task fixes. An MTP uid filter that names the discovered node (`X.M`) also matches none of the run's cases (`X.M[0]`, `X.M[1]`), so the selected test produces no result. The Design region says "Hook-failure fan-out uses ExpandInputs … because that path runs only after those short-circuits". That holds for the SetupOnce-*failure* path (line 793) but not for the resolution-error path. Reproduced with a scratch runfile (fixture: `SetupOnce(int)` plus `[Skip][Input(1)][Input(2)] Skipped(int)`):
  ```
  discovered: BadHook.Skipped
  ran:        BadHook.Skipped[0]:Failed, BadHook.Skipped[1]:Failed
  uid-filter on discovered Skipped node ran: 0
  ```
- Suggestion: In the `resolutionError` branch, build cases with `TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag)`, then apply `casePredicate`, for example by giving `SelectInputCases` a `filterTag` parameter or adding a sibling helper. Reword the Design comment to match. Add a meta-test case (bad `SetupOnce` signature plus a `[Skip]` method with `[Input]` rows) asserting that discovered uids equal run uids.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:152
- Description: `CreateMethodIdentifier` passes `declaringType.FullName` (for example `Mtp_.NodeIdentity_Given_+Probe`) as `typeName`. The MTP 2.3.3 contract (`Microsoft.Testing.Platform.xml`, `TestMethodIdentifierProperty` ctor/`TypeName`) reads: "Type name in metadata format, **not including the namespace**. … Nested types are represented by `+`." Consumers that combine `Namespace` + `TypeName`, such as the TRX report and IDE test explorers, will see the namespace twice (`Ns.Ns.Type`). That breaks the method identity that M10 is meant to publish. The meta-test `StartedFailure_Should_PublishInProgressAndParameters` asserts only `MethodName` and `ParameterTypeFullNames`, so this defect is not caught.
- Suggestion: Strip `"{Namespace}."` from `FullName` when the namespace is non-empty, keeping `+` for nesting. Assert `Namespace` and `TypeName` in the meta-test. For `Probe`, expect `Namespace == "Mtp_"` and `TypeName == "NodeIdentity_Given_+Probe"`.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu-testing-platform/mtp-sink.cs:82 / source/timewarp-jaribu-testing-platform/method-source-location.cs:176
- Description: `MethodSourceLocation.TryCreate` opens and parses the assembly's portable PDB on every `PublishNodeAsync`. That is once at discovery, once at in-progress, and once at completion for every node, including every `[Input]` row. On a large suite this means thousands of full PDB opens and parses, and the results are the same for every row of a method.
- Suggestion: Cache the result per `MethodInfo`, or keep one `MetadataReaderProvider` per assembly path, in a static `ConcurrentDictionary`. Negative results (no PDB) should be cached too.
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-jaribu/single-file-tests/mtp/mtp-node-identity.cs:71
- Description: The `TestFileLocationProperty` check is guarded by `if (location is not null)`. If location resolution regresses to always-null, the test still passes. The CI build is Debug with portable PDBs beside the assembly, so the property should always be present here.
- Suggestion: Assert `location.ShouldNotBeNull()`. If a no-PDB configuration has to be tolerated, gate the check on the PDB existing.
- Status: open

### Issue 5 — Severity: nit
- File: kanban/to-do/036-001-give-input-cases-distinct-test-node-uids-and-expand-them-in-mtp-discovery/task.md:81
- Description: The "How to validate" smoke command `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner -- --list-tests -- --filter-class Parameterized` fails with exit 12, a handshake mismatch: "The test host reported execution mode 'discover', but 'dotnet test' expected 'run'". `--list-tests` must be passed to `dotnet test`, not to the host. The meta-test already passes it that way.
- Suggestion: Change it to `dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner --list-tests -- --filter-class Parameterized`. That form prints `Discovered 7 tests.`
- Status: open
