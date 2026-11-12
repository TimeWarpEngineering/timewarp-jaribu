namespace FixieBench;

#region Purpose
// Entry point for the Fixie benchmark host.
#endregion

#region Design
// Fixie.Main.cs is the package's entry point and has no Purpose region.
// This file is the same call, so the package file stays out of the compilation.
#endregion

file static class Program
{
  private static Task<int> Main(string[] args)
  {
    return Fixie.Internal.EntryPoint.Main(typeof(Program).Assembly, customArguments: args);
  }
}
