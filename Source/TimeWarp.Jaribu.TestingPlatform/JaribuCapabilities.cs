using Microsoft.Testing.Platform.Capabilities.TestFramework;

namespace TimeWarp.Jaribu.TestingPlatform;

internal sealed class JaribuCapabilities : ITestFrameworkCapabilities
{
    public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => [];
}
