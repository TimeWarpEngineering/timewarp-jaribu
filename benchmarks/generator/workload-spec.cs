#region Purpose
// Names the six benchmark workloads and their case counts.
#endregion

#region Design
// The generator and the results manifest share this list so a host cannot
// grow a workload on its own. Case count is classes × methods, and × inputs
// when the kind is parameterized.
#endregion

internal sealed record WorkloadSpec(string Id, string Kind, int Classes, int MethodsPerClass, int Inputs)
{
  internal int Cases => Kind == WorkloadKind.Parameterized
    ? Classes * MethodsPerClass * Inputs
    : Classes * MethodsPerClass;
}

internal static class WorkloadKind
{
  internal const string Startup = "startup";
  internal const string Volume = "volume";
  internal const string Parameterized = "parameterized";
  internal const string Async = "async";
  internal const string Fixtures = "fixtures";
}

internal static class WorkloadCatalog
{
  internal const string SpecId = "040-1";

  internal static readonly WorkloadSpec[] All =
  [
    new("startup", WorkloadKind.Startup, 1, 1, 0),
    new("volume-1k", WorkloadKind.Volume, 1, 1_000, 0),
    new("volume-10k", WorkloadKind.Volume, 1, 10_000, 0),
    new("parameterized", WorkloadKind.Parameterized, 1, 100, 50),
    new("async-1k", WorkloadKind.Async, 1, 1_000, 0),
    new("fixtures", WorkloadKind.Fixtures, 100, 10, 0)
  ];
}
