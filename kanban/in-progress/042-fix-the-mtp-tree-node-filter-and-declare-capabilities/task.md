# Fix the MTP tree-node filter and declare capabilities

## Goal (implementer restatement)

`--treenode-filter` must select the cases an IDE or `dotnet test` names, and the adapter must say which MTP features it actually supports. Each reported case needs the four-segment path `/Assembly/Namespace/Type/Method` and a property bag that carries that case's tags and input values before `TreeNodeFilter.MatchesFilter` runs. The capabilities object must advertise tree-node filtering. Graceful stop stays undeclared until task 041 can stop a run, and every other known capability that stays out is named with the reason. Discovery and run are the only request types that execute tests; any other request type fails with an error node instead of running the suite. An out-of-process `dotnet test --treenode-filter` run must show that the selected set is the requested cases and not their neighbors.

The text a user writes in `[jaribu.parameter.N=…]` must stay the text `MtpSink` publishes. One helper owns the metadata type name and the parameter text, and the sink reads the filter's parameter-key prefix. The filter Design region must say that only those parameter keys are shared with the sink, and that `TestTag` exists only in the filter bag.

## Description
`TreeNodeFilter.MatchesFilter` is called with the flat node uid and an empty `PropertyBag`, so `--treenode-filter` (what IDEs and `dotnet test` send) selects nothing or the wrong nodes. `JaribuCapabilities` returns an empty collection, so hosts that gate features on capabilities skip them. Unknown request types fall into the run branch and execute the whole suite with a null filter.
Findings: MTP-6, MTP-7, MTP-9 in task 038 report.
## Requirements
- Build the MTP tree path (`/Assembly/Namespace/Type/Method`) and a populated property bag per case before calling the filter.
- Declare supported capabilities (tree-node filter, and graceful stop once the cancellation task lands); document each omitted one.
- Switch explicitly on request type; unsupported types produce an error node or throw.
- Validation: an out-of-process test that runs `dotnet test --treenode-filter` and asserts the selected set.
## Checklist
- [x] Tree path and property bag construction
- [x] Capabilities declared
- [x] Explicit request-type dispatch
- [x] Filter test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
- Implementation: implementer-grok (grok, vendor xai, model grok-4.7), session 01a1b70b-d1a3-7e43-a7a4-81d8c229ffcc (2026-11-08). Tree path and property bag, capability marker, request-type switch, and the out-of-process filter test.
- Implement oracle: implemented by implementer-grok (grok, vendor xai, model grok-4.7, response model grok-4.7-build), session 01a1b70b-d1a3-7e43-a7a4-81d8c229ffcc, max-turns 500, Done — 2026-11-07T18:03:41Z
- Review oracle round 1: claude (vendor anthropic, model claude-opus-5-5), ganda task work headless, effort 2, roster general, max-turns 120, 2026-11-08. Open findings M1 (suggestion) and M2 (nit), fix pass requested.
- Review oracle: review by reviewer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-opus-5-5), session f1aa84cf-5df1-4ba2-a057-705b2bfa6606, max-turns 120, artifacts general.md, merged.md — 2026-11-07T18:08:59Z
- Fix pass (implement-fix-2, 2026-11-08): `MtpNodeMetadata` is the only copy of the metadata type name and parameter text. `MtpSink` reads `MtpTreeFilter.ParameterKeyPrefix`. The filter Design region says `TestTag` stays in the filter bag.

## Results

`MtpTreeFilter` builds `/Assembly/Namespace/Type/Method` and a per-case property bag before `TreeNodeFilter.MatchesFilter`. The assembly segment is the simple name. The type segment keeps nested `+` and generic arity and drops the namespace. Only a `/` inside a segment is encoded as `%2F`. The bag carries each class and method `TestTag`, and each `[Input]` value as `jaribu.parameter.N`.

`TestingPlatformBuilderHook` calls `AddTreeNodeFilterService`, which is what registers `--treenode-filter`. `JaribuCapabilities` advertises `TreeNodeFilterCapability`. `ITestNodesTreeFilterTestFrameworkCapability` and `INamedFeatureCapability` are internal to Microsoft.Testing.Platform, so this assembly cannot implement them. Graceful stop stays undeclared until task 041 can cancel a run; advertising it would turn on `--maximum-failed-tests`. Banner ownership and the VSTest provider stay undeclared. Those omissions are written in the Design region of `jaribu-capabilities.cs`.

`ExecuteRequestAsync` switches on `DiscoverTestExecutionRequest` and `RunTestExecutionRequest`. Any other request type throws `NotSupportedException`, which the existing handler publishes as an error node. An unrecognized filter type throws the same way.

`dotnet pack -c Release` does not compile when `GeneratePackageOnBuild` is true and the Release output is missing (NU5026). A Release build first, which CI already does, lets the pack test succeed. That behavior is pre-existing.

`MtpNodeMetadata` owns `MetadataTypeName` and `FormatParameter`. `MtpTreeFilter` and `MtpSink` both call it. The sink builds `jaribu.parameter.N` from `MtpTreeFilter.ParameterKeyPrefix` instead of a second literal. `TestTag` is still added only by the filter bag. The sink does not publish it. The Design region in `mtp-tree-filter.cs` says that.

### How to validate

Smoke:

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
```

Expect:

- `TreeNodeFilter_Should_SelectTheRequestedSet` passes. Listing `/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest` discovers 1 test (`PassingTest`). Listing the `FailThenPass` method path discovers both `FailThenPass(fail)` and `FailThenPass(pass)`. `/**[TestTag=EdgeCases]` discovers 5 tests, including `NullReferenceExceptionTest` and `ArgumentExceptionTest`. `/**[jaribu.parameter.0=fail]` discovers only `FailThenPass(fail)`. An unknown method path exits 8 and discovers 0. Running the `PassingTest` path reports `total: 1`, `succeeded: 1`, `failed: 0`.
- `dotnet test` on the MTP runner reports 103 succeeded. The fix-pass run on 2026-11-08 did: `total: 103`, `succeeded: 103`, `failed: 0`, exit 0, about 2m 54s. A class filter on `NodeIdentity` before that reported 6 succeeded, including the tree-node filter test and the in-process parameter publication test.
- `ganda repo audit` passes with 0 failures. This pass did not re-run `./bin/dev test`. The MTP runner compiles the same `mtp-node-identity.cs` suite.
- `dotnet build` of `source/timewarp-jaribu-testing-platform/timewarp-jaribu-testing-platform.csproj` reports 0 warnings and 0 errors.

### Review

Round 1 (effort 2, general reviewer) found 0 bugs, 1 suggestion, and 1 nit. M1: the filter's parameter key prefix, `FormatParameter`, and `MetadataTypeName` were copies of `MtpSink` code. The fix pass moved the type name and parameter text into `MtpNodeMetadata` and pointed the sink at `MtpTreeFilter.ParameterKeyPrefix`. M2: the Design region said `MtpSink` publishes `TestTag`. It now says `TestTag` exists only in the filter bag and that only `jaribu.parameter.N` is shared. Round 2 still has to disposition the findings. Files: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`.

## Review

- Round 1: open M1 and M2 were implemented on this branch. Round 2 has not dispositioned them.
