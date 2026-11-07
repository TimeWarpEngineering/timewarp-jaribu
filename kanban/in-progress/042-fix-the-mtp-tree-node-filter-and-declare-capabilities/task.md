# Fix the MTP tree-node filter and declare capabilities

## Description
`TreeNodeFilter.MatchesFilter` is called with the flat node uid and an empty `PropertyBag`, so `--treenode-filter` (what IDEs and `dotnet test` send) selects nothing or the wrong nodes. `JaribuCapabilities` returns an empty collection, so hosts that gate features on capabilities skip them. Unknown request types fall into the run branch and execute the whole suite with a null filter.
Findings: MTP-6, MTP-7, MTP-9 in task 038 report.
## Requirements
- Build the MTP tree path (`/Assembly/Namespace/Type/Method`) and a populated property bag per case before calling the filter.
- Declare supported capabilities (tree-node filter, and graceful stop once the cancellation task lands); document each omitted one.
- Switch explicitly on request type; unsupported types produce an error node or throw.
- Validation: an out-of-process test that runs `dotnet test --treenode-filter` and asserts the selected set.
## Checklist
- [ ] Tree path and property bag construction
- [ ] Capabilities declared
- [ ] Explicit request-type dispatch
- [ ] Filter test

## Session

- Created: ce5c4952-6364-4ae8-824b-a0243ec9ccbe (2026-11-07), triaged from task 038 code review
