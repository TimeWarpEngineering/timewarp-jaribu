# Round 1 — general
**Date:** 2026-11-09
**Scope reviewed:** `master...task/053-fix-documentation-readme-agentmd-test-plan-xml-doc`: readme.md, agent.md, documentation/architecture.md, tests/timewarp-jaribu/test-plan.md, the sink and attribute sources, the MTP hook and extension, and `test-runner.runner-guards.cs`.

## Summary

Most of the change does what the task asks. The dependency line, license, shebang, version pin, `agent.md` paths, version location, the rewritten test plan, and the architecture overview all match the repo. Every link and backticked path in the four docs resolves. Every public type and member in both shipping libraries has XML docs. The undocumented members found by a scan are all on internal or private types. Two defects remain. One readme sample still does not compile. The attribute guards go further than null and turn a previously valid `[Skip("")]` into a process crash.

## Issues

### Issue 1 — Severity: bug
- File: readme.md:189
- Description: The "Test file pattern" sample uses `[ModuleInitializer]` without `using System.Runtime.CompilerServices;`. The requirement is that readme samples compile. I copied the sample verbatim into `/tmp/rv053/pattern.cs` and pointed `#:project` at `source/timewarp-jaribu/timewarp-jaribu.csproj`. `dotnet run` fails with `CS0246: The type or namespace name 'ModuleInitializer' could not be found`. The other two samples were fixed by fully qualifying the attribute. This one was missed. The implementer's Results checked only the first sample. The session-fixture snippet at readme.md:524 has the same short name, but it is a fragment, not a file.
- Suggestion: Add `using System.Runtime.CompilerServices;` to the sample, or write `[System.Runtime.CompilerServices.ModuleInitializer]` the way the first sample does.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-jaribu/skip-attribute.cs:24
- Description: `SkipAttribute` and `TestTagAttribute` (test-tag-attribute.cs:23) now call `ArgumentException.ThrowIfNullOrWhiteSpace`. The task asked for null guards. On master, `[Skip("")]` and `[TestTag("")]` were valid. The attribute constructor runs inside `GetCustomAttribute` in the class loop (`test-runner.method-run.cs:45`, `test-case-identity.cs:132`, `test-filter.cs:186`, and `mtp-tree-filter.cs:108` under MTP). So the new throw is not reported as a test node. It aborts the whole process. Probe `/tmp/rv053/emptyskip.cs` has one passing test and one `[Skip("")]` test. Run through `RunAllTests`, it prints `Good ✓ PASSED` and then `Unhandled exception. System.ArgumentException ... (Parameter 'reason')`, exiting 134 with no summary. A consumer with an empty skip reason or tag upgrades and gets a crashed runner instead of a skipped test. The new test only constructs the attributes directly, so it does not cover this path.
- Suggestion: Keep the change to what the task asked for, `ArgumentNullException.ThrowIfNull`, and update the XML docs, architecture.md:36–37, and the test. If rejecting blank values is wanted, that is a behavior change. It needs the runner to report the bad attribute as an Error node (see Issue 3) and a readme note.
- Status: open

### Issue 3 — Severity: suggestion
- File: source/timewarp-jaribu/test-runner.method-run.cs:45
- Description: The guards added here (a null reason, tag, or `[Input]` array) and the existing `[Timeout(0)]` guard all throw from attribute construction. The runner reads those attributes through `GetCustomAttribute(s)` and does not catch the throw, so one bad attribute on one method takes down the whole run, as Issue 2 shows. The docs now say the attributes "reject" these values (architecture.md:36–39, test-plan.md "What a green run means"). Today, rejected means an unhandled crash. No test covers a rejected attribute on a registered test class.
- Suggestion: Catch `CustomAttributeFormatException` and `ArgumentException` when the runner reads a method's Jaribu attributes. Report that method as an Error node whose message names the attribute. Add a runner-guards case with a nested fixture carrying a bad attribute that asserts an Error node and a failing exit. If that is out of scope for this docs task, wontfix it with a follow-up task id.
- Status: open
