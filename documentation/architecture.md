# Architecture

Jaribu discovers public static `Task` methods and reports each one through an `ITestResultSink`. The same test class runs as a .NET runfile or under Microsoft.Testing.Platform.

## Runner

`TestRunner` is the public entry point.

- `RunTests<T>` and `RunAllTests` write through a `TerminalSink` and return an exit code. `0` means the run succeeded. `1` means a test failed, the run was cancelled, or no tests executed when empty runs are not allowed.
- `RunTestsAsync` takes the sink, the filters, and a `CancellationToken`.
- `RegisterTests<T>` records a class. `RegisteredTestClasses` returns a snapshot.
- Discovery keeps `public static Task` methods. `Task<T>`, `ValueTask`, `ValueTask<T>`, `async void`, and an open generic `Task` are near-misses: a warning by default, a failed run when strict signatures are on. Ordinary `void` methods are ignored.
- A class with no discoverable tests fails unless `allowEmpty` is set. A method or case filter that selects nothing skips that empty-class node.

Lifecycle hooks are `Setup`, `CleanUp`, `SetupOnce`, and `CleanUpOnce`. Each must be `public static Task` with no parameters. A near-miss hook fails the class. `SetupOnce` runs before the first test that executes. `CleanUpOnce` runs afterward only when setup was attempted.

`RegisterSessionFixture<T>` shares one `IAsyncDisposable` across classes in a session. The session creates it on the first `SessionFixture.GetAsync<T>` and disposes it when the session ends.

## Sinks

```csharp
TestRunner.RunTestsAsync(sink)
    ├── TerminalSink → console, via TimeWarp.Terminal
    ├── MtpSink      → Microsoft.Testing.Platform IMessageBus (internal)
    └── NullSink     → discards events
```

`NullSink`, `TerminalSink`, and `MtpSink` reject a null node, class name, stats object, or result list. A null tag filter is allowed.

The runner's `ActivitySource` is `TimeWarp.Jaribu`. Each class run is a `jaribu.class` span and each case is a child `jaribu.test` span. Setup and cleanup hooks are `jaribu.hook` spans. The test process exports them when `OTEL_EXPORTER_OTLP_ENDPOINT` is set and flushes before it returns. See [telemetry.md](telemetry.md).

`TestNodeState` is Discovered, InProgress, Passed, Failed, Skipped, Timeout, Error, or Cancelled. `TestRunStats.FailedCount` includes Failed, Error, Timeout, and Cancelled. Skipped does not fail the run.

## Attributes

| Attribute | Role |
|-----------|------|
| `[TestTag("name")]` | Class or method tag. Null is rejected. Empty and whitespace are kept. |
| `[Skip("reason")]` | Skips the method. Null is rejected. Empty and whitespace are kept. |
| `[Input(...)]` | One invocation per attribute. `[Input(null)]` is one null argument. |
| `[Timeout(ms)]` | Positive milliseconds. On a method it limits that body. On a class it limits hooks and session-fixture creation. Zero and negative are rejected. |
| `[Proves(type, name)]` | Claim proved only under `--prove`. A normal run ignores it. Overloads of `name` share one claim. |
| `[KnownTautology("reason")]` | Smoke or compile-only test. Listed in the tautology report. Strict mode does not fail the run for it. |

A constructor rejection (`ArgumentException` or `CustomAttributeFormatException`) on a discovered method, or on the class, is one Error node for that method. The message names the attribute. The run fails. The process stays up, and a sibling method still runs. A class tag miss and a method tag miss do not replace that Error. The healthy sibling of a class tag miss stays Skipped and does not run. The healthy sibling of a method tag miss still runs.

`TestRunner.DefaultHookTimeout` is the hook budget when the class has no `[Timeout]`. Zero, negative, and sub-millisecond values throw.

## Microsoft.Testing.Platform adapter

The package `TimeWarp.Jaribu.TestingPlatform` exposes one public type, `TestingPlatformBuilderHook`. The platform calls `AddExtensions(ITestApplicationBuilder builder, string[] args)`. The hook registers the command-line provider, the tree-node filter service, and `JaribuTestFramework`.

`JaribuCapabilities` advertises tree-node filtering. It does not advertise graceful stop, banner ownership, or the VSTest provider. The request `CancellationToken` is honored.

Options:

| Option | Effect |
|--------|--------|
| `--filter-tag` | Any listed tag (repeat the option). A miss is one Skipped node per method, unless that method's attribute constructor throws. That rejection is an Error node and the run fails. `JARIBU_FILTER_TAG` is a comma list. A blank value is ignored. CLI wins when set. | 
| `--exclude-tag` | Any listed tag, after include. A hit is one Skipped node whose message names the tag. `JARIBU_EXCLUDE_TAG` is a comma list. CLI wins when set. |
| `--filter-class` | Substring of the class full name. Non-matches are omitted. |
| `--filter-method` | Substring of the method name. Non-matches are omitted. |
| `--strict-signatures` | Near-miss signatures fail the run. |
| `--allow-empty` | A class with no discoverable tests succeeds. |
| `--per-test-coverage` | Record production methods each test enters. |
| `--prove` | Break each `[Proves]` claim, or the implied claim, and require a failure then a pass. |
| `--strict-tautology` | Unproven and zero-coverage tests fail the run. |
| `--require-claims` | A test with no `[Proves]` is a tautology warning, or a failure when strict. |

Tree paths use `/Assembly/Namespace/Type/Method`.

## Package version

`<Version>` lives in `source/Directory.Build.props`. The root `Directory.Build.props` sets `TargetFramework` to `net11.0` and turns analyzers on. It does not set the package version.

The library package references Shouldly, TimeWarp.Terminal, TimeWarp.Amuru, and OpenTelemetry. `TerminalSink` and `TestHelpers` write through TimeWarp.Terminal. Test authors call Shouldly. TimeWarp.Amuru is on the package graph for runfiles that shell out. OpenTelemetry exports test spans only when the OTLP endpoint is set.
