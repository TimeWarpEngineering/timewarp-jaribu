# Fix the MTP tree-node filter and declare capabilities

## Goal (implementer restatement)

`--treenode-filter` must select the cases an IDE or `dotnet test` names, and the adapter must say which MTP features it actually supports. Each reported case needs the four-segment path `/Assembly/Namespace/Type/Method` and a property bag that carries that case's tags and input values before `TreeNodeFilter.MatchesFilter` runs. The capabilities object must advertise tree-node filtering. Graceful stop stays undeclared until task 041 can stop a run, and every other known capability that stays out is named with the reason. Discovery and run are the only request types that execute tests; any other request type fails with an error node instead of running the suite. An out-of-process `dotnet test --treenode-filter` run must show that the selected set is the requested cases and not their neighbors.

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

## Results

`MtpTreeFilter` builds `/Assembly/Namespace/Type/Method` and a per-case property bag before `TreeNodeFilter.MatchesFilter`. The assembly segment is the simple name. The type segment keeps nested `+` and generic arity and drops the namespace. Only a `/` inside a segment is encoded as `%2F`. The bag carries each class and method `TestTag`, and each `[Input]` value as `jaribu.parameter.N`.

`TestingPlatformBuilderHook` calls `AddTreeNodeFilterService`, which is what registers `--treenode-filter`. `JaribuCapabilities` advertises `TreeNodeFilterCapability`. `ITestNodesTreeFilterTestFrameworkCapability` and `INamedFeatureCapability` are internal to Microsoft.Testing.Platform, so this assembly cannot implement them. Graceful stop stays undeclared until task 041 can cancel a run; advertising it would turn on `--maximum-failed-tests`. Banner ownership and the VSTest provider stay undeclared. Those omissions are written in the Design region of `jaribu-capabilities.cs`.

`ExecuteRequestAsync` switches on `DiscoverTestExecutionRequest` and `RunTestExecutionRequest`. Any other request type throws `NotSupportedException`, which the existing handler publishes as an error node. An unrecognized filter type throws the same way.

`dotnet pack -c Release` does not compile when `GeneratePackageOnBuild` is true and the Release output is missing (NU5026). A Release build first, which CI already does, lets the pack test succeed. That behavior is pre-existing.

### How to validate

Smoke:

```bash
./bin/dev test
dotnet test tests/timewarp-jaribu/multi-file-runners/mtp-runner/
```

Expect:

- `TreeNodeFilter_Should_SelectTheRequestedSet` passes. Listing `/timewarp-jaribu-mtp-validation/TimeWarp.Jaribu.MtpValidation/BasicTests/PassingTest` discovers 1 test (`PassingTest`). Listing the `FailThenPass` method path discovers both `FailThenPass(fail)` and `FailThenPass(pass)`. `/**[TestTag=EdgeCases]` discovers 5 tests, including `NullReferenceExceptionTest` and `ArgumentExceptionTest`. `/**[jaribu.parameter.0=fail]` discovers only `FailThenPass(fail)`. An unknown method path exits 8 and discovers 0. Running the `PassingTest` path reports `total: 1`, `succeeded: 1`, `failed: 0`.
- `dotnet test` on the MTP runner reports 103 succeeded. The run on 2026-11-08 did: `total: 103`, `succeeded: 103`, exit 0, about 2m 55s.
- `./bin/dev test` reports 103 passed. That run completed earlier in this walk, on the same uncommitted tree, before the MTP runner was re-run.
- `ganda repo audit` passes with 0 failures. `audit --fix` only wrote a gitignored VS Code window title.
