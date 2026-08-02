namespace TimeWarp.Jaribu.TestingPlatform;

using System.Reflection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Services;

/// <summary>
/// Implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Bridges Jaribu's test runner with M.T.P. for test discovery and execution.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer
{
  private readonly IExtension Extension;
  private readonly IServiceProvider ServiceProvider;

  public JaribuTestFramework
  (
    IExtension extension,
    IServiceProvider serviceProvider
  )
  {
    Extension = extension;
    ServiceProvider = serviceProvider;
  }

  public string Uid => Extension.Uid;
  public string Version => Extension.Version;
  public string DisplayName => Extension.DisplayName;
  public string Description => Extension.Description;

  public Type[] DataTypesProduced =>
  [
    typeof(TestNodeUpdateMessage)
  ];

  public Task<bool> IsEnabledAsync() => Extension.IsEnabledAsync();

  public Task<CreateTestSessionResult> CreateTestSessionAsync(CreateTestSessionContext context)
  {
    TestRunner.BeginTestSession();
    return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
  }

  public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
  {
    try
    {
      await TestRunner.EndTestSessionAsync().ConfigureAwait(false);
      return new CloseTestSessionResult { IsSuccess = true };
    }
#pragma warning disable CA1031 // Host boundary: any dispose failure must become IsSuccess=false
    catch (Exception ex)
#pragma warning restore CA1031
    {
      return new CloseTestSessionResult
      {
        IsSuccess = false,
        ErrorMessage = $"Session fixture dispose failed: {ex.Message}"
      };
    }
  }

  public async Task ExecuteRequestAsync(ExecuteRequestContext context)
  {
    try
    {
      bool isDiscovery = context.Request is DiscoverTestExecutionRequest;
      ITestExecutionFilter? filter = GetFilter(context);
      MtpSink sink = new(this, context.MessageBus, context.Request.Session.SessionUid);

      ICommandLineOptions commandLine = ServiceProvider.GetCommandLineOptions();
      string? filterTag = ResolveFilterTag(commandLine);
      string? filterClass = GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterClassOption);
      string? filterMethod = GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterMethodOption);

      foreach (Type testClass in TestRunner.RegisteredTestClasses)
      {
        if (filterClass is not null &&
            testClass.FullName?.Contains(filterClass, StringComparison.OrdinalIgnoreCase) != true)
        {
          continue;
        }

        if (isDiscovery)
        {
          // Mirror the run path: a class whose class-level tags exist and none
          // match the tag filter is omitted entirely (run produces no nodes for
          // it). Method-level tag mismatches stay listed — the run reports them
          // as Skipped nodes, so they are part of the run.
          if (filterTag is not null && ClassOmittedByTagFilter(testClass, filterTag))
          {
            continue;
          }

          foreach (System.Reflection.MethodInfo method in TestRunner.DiscoverTests(testClass))
          {
            if (filterMethod is not null &&
                !method.Name.Contains(filterMethod, StringComparison.OrdinalIgnoreCase))
            {
              continue;
            }

            string testNodeUid = $"{testClass.FullName}.{method.Name}";
            if (filter is not null && !MatchesFilter(testNodeUid, filter))
            {
              continue;
            }

            TestNodeInfo node = new
            (
              Uid: testNodeUid,
              DisplayName: method.Name,
              State: TestNodeState.Discovered
            );
            await sink.OnTestDiscoveredAsync(node).ConfigureAwait(false);
          }
        }
        else
        {
          // Selection filters omit methods (no Skipped nodes). Tag filter keeps Skipped semantics.
          // Also honor MTP uid/tree filter on the run path (not only discovery).
          await TestRunner.RunTestsAsync(
            testClass,
            sink,
            filterTag,
            methodNameContains: filterMethod,
            methodPredicate: filter is null
              ? null
              : method => MatchesFilter($"{testClass.FullName}.{method.Name}", filter)
          ).ConfigureAwait(false);
        }
      }
    }
    catch (Exception ex)
    {
      await ReportUnhandledExceptionAsync(context, ex).ConfigureAwait(false);
      throw;
    }
    finally
    {
      context.Complete();
    }
  }

  private static bool ClassOmittedByTagFilter(Type testClass, string filterTag)
  {
    TestTagAttribute[] classTags = [.. testClass.GetCustomAttributes<TestTagAttribute>()];
    return classTags.Length > 0 &&
      !classTags.Any(t => t.Tag.Equals(filterTag, StringComparison.OrdinalIgnoreCase));
  }

  private static string? ResolveFilterTag(ICommandLineOptions commandLine)
  {
    string? cliTag = GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterTagOption);
    if (!string.IsNullOrWhiteSpace(cliTag))
    {
      return cliTag;
    }

    return Environment.GetEnvironmentVariable("JARIBU_FILTER_TAG");
  }

  private static string? GetOptionArgument(ICommandLineOptions commandLine, string optionName)
  {
    if (!commandLine.IsOptionSet(optionName))
    {
      return null;
    }

    if (!commandLine.TryGetOptionArgumentList(optionName, out string[]? args) ||
        args is null ||
        args.Length == 0)
    {
      return null;
    }

    string value = args[0];
    return string.IsNullOrWhiteSpace(value) ? null : value;
  }

  private async Task ReportUnhandledExceptionAsync(ExecuteRequestContext context, Exception ex)
  {
    await context.MessageBus.PublishAsync
    (
      this,
      new TestNodeUpdateMessage
      (
        sessionUid: context.Request.Session.SessionUid,
        testNode: new TestNode
        {
          Uid = new TestNodeUid($"Jaribu.Error.{Guid.NewGuid()}"),
          DisplayName = $"Unhandled: {ex.GetType().Name}",
          Properties = new PropertyBag(new ErrorTestNodeStateProperty(ex))
        }
      )
    ).ConfigureAwait(false);
  }

  private static ITestExecutionFilter? GetFilter(ExecuteRequestContext context)
    => context.Request switch
    {
      RunTestExecutionRequest r => r.Filter,
      DiscoverTestExecutionRequest d => d.Filter,
      _ => null
    };

#pragma warning disable TPEXP // TreeNodeFilter is experimental
  private static bool MatchesFilter(string testNodeUid, ITestExecutionFilter filter)
    => filter switch
    {
      TestNodeUidListFilter uidFilter =>
        uidFilter.TestNodeUids.Any(u => u.Value == testNodeUid),
      TreeNodeFilter treeFilter =>
        treeFilter.MatchesFilter(testNodeUid, new PropertyBag()),
      _ => true
    };
#pragma warning restore TPEXP
}
