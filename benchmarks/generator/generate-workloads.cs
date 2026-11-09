#!/usr/bin/env -S dotnet --
#:include workload-spec.cs
#:include emit-text.cs
#:include emit-jaribu.cs
#:include emit-fixie.cs
#:include emit-tunit.cs
#:include emit-xunit.cs

using System.Globalization;
using System.Linq;

#region Purpose
// Writes one workload folder per host from WorkloadCatalog.
#endregion

#region Design
// The runfile is the only writer of benchmarks/generated. dev benchmark calls it
// before every measurement so a hand edit of a generated test cannot survive.
// A manifest records the spec id and case counts the report divides by.
#endregion

string repo = FindRepo(args);
string generated = Path.Combine(repo, "benchmarks", "generated");
if (Directory.Exists(generated))
{
  Directory.Delete(generated, recursive: true);
}

foreach (WorkloadSpec spec in WorkloadCatalog.All)
{
  string jaribu = Path.Combine(generated, "jaribu", spec.Id);
  JaribuEmit.Write(jaribu, spec);
  FixieEmit.Write(Path.Combine(generated, "fixie", spec.Id), spec);
  TUnitEmit.Write(Path.Combine(generated, "tunit", spec.Id), spec);
  XunitEmit.Write(Path.Combine(generated, "xunit", spec.Id), spec);
  WriteRunfile(generated, spec);
}

WriteManifest(generated);
Console.WriteLine("generated " + generated);
return 0;

static void WriteRunfile(string generated, WorkloadSpec spec)
{
  string directory = Path.Combine(generated, "jaribu-runfile", spec.Id);
  Directory.CreateDirectory(directory);
  string path = Path.Combine(directory, "run.cs");
  string project = Path.GetFullPath(Path.Combine(directory, "..", "..", "..", "..", "source", "timewarp-jaribu", "timewarp-jaribu.csproj"));
  string includeRoot = Path.GetFullPath(Path.Combine(generated, "jaribu", spec.Id));
  using StreamWriter writer = new(path, append: false);
  writer.WriteLine("#!/usr/bin/env -S dotnet --");
  writer.WriteLine("#:project " + project);
  foreach (string file in Directory.GetFiles(includeRoot, "*.g.cs").Order())
  {
    writer.WriteLine("#:include " + Path.GetFullPath(file));
  }

  writer.WriteLine();
  writer.WriteLine("int exitCode = await TimeWarp.Jaribu.TestRunner.RunAllTests().ConfigureAwait(false);");
  writer.WriteLine("Bench.BenchProbe.Report();");
  writer.WriteLine("return exitCode;");
}

static void WriteManifest(string generated)
{
  using StreamWriter writer = new(Path.Combine(generated, "manifest.txt"), append: false);
  writer.WriteLine("spec=" + WorkloadCatalog.SpecId);
  foreach (WorkloadSpec spec in WorkloadCatalog.All)
  {
    writer.WriteLine(
      "workload=" + spec.Id
      + " kind=" + spec.Kind
      + " cases=" + spec.Cases.ToString(System.Globalization.CultureInfo.InvariantCulture));
  }
}

static string FindRepo(string[] args)
{
  for (int index = 0; index < args.Length - 1; index++)
  {
    if (args[index] == "--repo")
    {
      return Path.GetFullPath(args[index + 1]);
    }
  }

  DirectoryInfo? directory = new(AppContext.BaseDirectory);
  while (directory is not null)
  {
    if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
    {
      return directory.FullName;
    }

    directory = directory.Parent;
  }

  directory = new DirectoryInfo(Directory.GetCurrentDirectory());
  while (directory is not null)
  {
    if (File.Exists(Path.Combine(directory.FullName, "timewarp-jaribu.slnx")))
    {
      return directory.FullName;
    }

    directory = directory.Parent;
  }

  throw new InvalidOperationException("Could not find the repository root. Pass --repo.");
}
