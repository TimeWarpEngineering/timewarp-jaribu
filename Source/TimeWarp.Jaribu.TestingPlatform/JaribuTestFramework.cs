using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using Microsoft.Testing.Platform.Requests;

namespace TimeWarp.Jaribu.TestingPlatform;

/// <summary>
/// Stub implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Task 013 will implement the full test discovery and execution logic.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer
{
    private readonly IExtension _extension;

    public JaribuTestFramework(IExtension extension, IServiceProvider serviceProvider)
    {
        _extension = extension;
    }

    public string Uid => _extension.Uid;
    public string Version => _extension.Version;
    public string DisplayName => _extension.DisplayName;
    public string Description => _extension.Description;

    public Type[] DataTypesProduced =>
    [
        typeof(TestNodeUpdateMessage)
    ];

    public Task<bool> IsEnabledAsync() => _extension.IsEnabledAsync();

    public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
    {
        return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
    }

    public Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
    {
        return Task.FromResult(new CloseTestSessionResult { IsSuccess = true });
    }

    public Task ExecuteRequestAsync(ExecuteRequestContext context)
    {
        // Stub implementation - Task 013 will implement full logic
        // For now, just complete the request successfully
        context.Complete();
        return Task.CompletedTask;
    }
}
