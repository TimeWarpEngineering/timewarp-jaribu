namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Registers Jaribu with the Microsoft.Testing.Platform application builder.
#endregion

#region Design
// AddTreeNodeFilterService publishes --treenode-filter. Without that provider
// the host rejects the option before Jaribu can match a tree path.
#endregion

/// <summary>
/// Entry point Microsoft.Testing.Platform calls to register the Jaribu test framework.
/// </summary>
public static class TestingPlatformBuilderHook
{
  /// <summary>
  /// Registers the command-line provider, the tree-node filter service, and the test framework.
  /// </summary>
  /// <param name="builder">The platform application builder.</param>
  /// <param name="args">
  /// Arguments the platform passed to the hook. Jaribu reads its options from the command-line
  /// provider, so the array is checked for null and otherwise unused.
  /// </param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="builder"/> or <paramref name="args"/> is null.
  /// </exception>
  public static void AddExtensions
  (
    ITestApplicationBuilder builder,
    string[] args
  )
  {
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(args);

    JaribuExtension extension = new();

    builder.CommandLine.AddProvider(() => new JaribuCommandLineOptionsProvider(extension));
    builder.AddTreeNodeFilterService(extension);

    builder.RegisterTestFramework
    (
      _ => new JaribuCapabilities(),
      (_, serviceProvider) => new JaribuTestFramework(extension, serviceProvider)
    );
  }
}
