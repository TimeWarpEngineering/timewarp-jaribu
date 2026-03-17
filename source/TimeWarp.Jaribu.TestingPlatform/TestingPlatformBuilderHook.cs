namespace TimeWarp.Jaribu.TestingPlatform;

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

    builder.RegisterTestFramework
    (
      _ => new JaribuCapabilities(),
      (_, serviceProvider) => new JaribuTestFramework(extension, serviceProvider)
    );
  }
}
