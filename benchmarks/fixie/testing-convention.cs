#region Purpose
// Selects the TimeWarp.Fixie discovery and execution convention for the benchmark project.
#endregion

#region Design
// Fixie finds one ITestProject. The base convention supplies Input, tags, and
// ConfigureServices. NotTest keeps this type out of the workload.
// Custom arguments are tag filters, so the benchmark launch must not append
// dotnet flags after the project options.
#endregion

using TimeWarp.Fixie;

[NotTest]
public sealed class TestingConvention : TimeWarp.Fixie.TestingConvention;
