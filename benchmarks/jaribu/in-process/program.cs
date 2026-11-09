#region Purpose
// Hosts BenchmarkDotNet for in-process Jaribu runner timings.
#endregion

#region Design
// BenchmarkSwitcher.Run returns a sequence of summaries. A critical validation
// error, a failed report, or an empty run (unknown option, no match) exits 1.
#endregion

using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Jaribu.InProcess;

IEnumerable<Summary> summaries = BenchmarkSwitcher.FromAssembly(typeof(JaribuRunnerBench).Assembly).Run(args);
int failed = 0;
int seen = 0;
foreach (Summary summary in summaries)
{
  seen++;
  if (summary.HasCriticalValidationErrors)
  {
    failed++;
  }

  foreach (BenchmarkReport report in summary.Reports)
  {
    if (!report.Success)
    {
      failed++;
    }
  }
}

return failed == 0 && seen > 0 ? 0 : 1;
