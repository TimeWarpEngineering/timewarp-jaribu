using Microsoft.Testing.Platform.Extensions;

namespace TimeWarp.Jaribu.TestingPlatform;

internal sealed class JaribuExtension : IExtension
{
    public string Uid => "TimeWarp.Jaribu";
    public string Version => typeof(JaribuExtension).Assembly
        .GetName().Version?.ToString() ?? "1.0.0";
    public string DisplayName => "TimeWarp.Jaribu Test Framework";
    public string Description => "Lightweight test framework for .NET runfiles and compiled projects";

    public Task<bool> IsEnabledAsync() => Task.FromResult(true);
}
