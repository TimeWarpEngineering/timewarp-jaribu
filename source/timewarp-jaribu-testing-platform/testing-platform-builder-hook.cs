namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Registers Jaribu with the Microsoft.Testing.Platform application builder.
#endregion

#region Design
// AddTreeNodeFilterService publishes --treenode-filter. Without that provider
// the host rejects the option before Jaribu can match a tree path.
#endregion

public static class TestingPlatformBuilderHook
{
  public static void AddExtensions
  (
    ITestApplicationBuilder builder,
    string[] _
  )
  {
    ArgumentNullException.ThrowIfNull(builder);

    JaribuExtension extension = new();

    builder.CommandLine.AddProvider(() => new JaribuCommandLineOptionsProvider());
    builder.AddTreeNodeFilterService(extension);

    builder.RegisterTestFramework
    (
      _ => new JaribuCapabilities(),
      (_, serviceProvider) => new JaribuTestFramework(extension, serviceProvider)
    );
  }
}
