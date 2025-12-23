using Microsoft.Testing.Platform.Builder;

namespace TimeWarp.Jaribu.TestingPlatform;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder builder, string[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var extension = new JaribuExtension();

        builder.RegisterTestFramework(
            _ => new JaribuCapabilities(),
            (capabilities, serviceProvider) => new JaribuTestFramework(extension, serviceProvider));
    }
}
