namespace TimeWarp.Jaribu.TestingPlatform;

using System.Reflection;
using Microsoft.Testing.Platform.CommandLine;
using Microsoft.Testing.Platform.Extensions.OutputDevice;
using Microsoft.Testing.Platform.OutputDevice;
using Microsoft.Testing.Platform.Services;

#region Purpose
// Bridges Jaribu discovery and execution to Microsoft.Testing.Platform.
#endregion

#region Design
// ExecuteRequestAsync copies RegisteredTestClasses before the loop.
// A class may call ClearRegisteredTests or RegisterTests while it runs.
// Enumerating the live list throws Collection was modified and drops every
// later class. RunAllTestsCore uses the same snapshot.
// DiscoverTestExecutionRequest and RunTestExecutionRequest are the only
// requests that select or run cases. Any other request type throws, and the
// handler publishes an error node for that failure. Tree and uid filters go
// through MtpTreeFilter; a filter this adapter does not recognize throws too.
//
// context.CancellationToken is passed into the class run and into session
// close. A fired token stops the class loop. Cooperative cancellation is not
// republished as an Error node.
// --strict-signatures and --allow-empty are passed to RunTestsAsync.
// Signature warnings go to the output device when the class produced nodes,
// or when the MTP filter is NopFilter and no method filter is set.
// A uid or tree filter passes a case predicate, so an empty class that was
// not selected does not emit NoTests. The same predicate omits strict
// near-miss Error nodes when that class produced no selected nodes. The
// warning display already hides that class.
#endregion

/// <summary>
/// Implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Bridges Jaribu's test runner with M.T.P. for test discovery and execution.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer, IOutputDeviceDataProducer
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
    if (context.CancellationToken.IsCancellationRequested)
    {
      return Task.FromResult(new CreateTestSessionResult
      {
        IsSuccess = false,
        ErrorMessage = "Test session creation was cancelled."
      });
    }

    TestRunner.BeginTestSession();
    return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
  }

  public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
  {
    try
    {
      await TestRunner.EndTestSessionAsync(context.CancellationToken).ConfigureAwait(false);
      if (context.CancellationToken.IsCancellationRequested)
      {
        return new CloseTestSessionResult
        {
          IsSuccess = false,
          ErrorMessage = "Test session close was cancelled."
        };
      }

      return new CloseTestSessionResult { IsSuccess = true };
    }
    catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
    {
      return new CloseTestSessionResult
      {
        IsSuccess = false,
        ErrorMessage = "Test session close was cancelled."
      };
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
      bool isDiscovery;
      ITestExecutionFilter filter;
      switch (context.Request)
      {
        case DiscoverTestExecutionRequest discover:
          isDiscovery = true;
          filter = discover.Filter;
          break;
        case RunTestExecutionRequest run:
          isDiscovery = false;
          filter = run.Filter;
          break;
        default:
          throw new NotSupportedException(
            $"Unsupported MTP request type '{context.Request.GetType().FullName}'.");
      }

      MtpSink sink = new(this, context.MessageBus, context.Request.Session.SessionUid);

      ICommandLineOptions commandLine = ServiceProvider.GetCommandLineOptions();
      string? filterTag = ResolveFilterTag(commandLine);
      string? filterClass = GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterClassOption);
      string? filterMethod = GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterMethodOption);
      bool strictSignatures = commandLine.IsOptionSet(JaribuCommandLineOptionsProvider.StrictSignaturesOption);
      bool allowEmpty = commandLine.IsOptionSet(JaribuCommandLineOptionsProvider.AllowEmptyOption);
      IOutputDevice outputDevice = ServiceProvider.GetOutputDevice();

      // Snapshot so RegisterTests / ClearRegisteredTests during a class cannot tear the list.
      Type[] registeredClasses = [.. TestRunner.RegisteredTestClasses];
      CancellationToken cancellationToken = context.CancellationToken;

      foreach (Type testClass in registeredClasses)
      {
        if (cancellationToken.IsCancellationRequested)
        {
          break;
        }

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
            if (cancellationToken.IsCancellationRequested)
            {
              break;
            }

            if (filterMethod is not null &&
                !method.Name.Contains(filterMethod, StringComparison.OrdinalIgnoreCase))
            {
              continue;
            }

            foreach (ReportedTestCase testCase in TestCaseIdentity.EnumerateReportedCases(testClass, method, filterTag))
            {
              if (cancellationToken.IsCancellationRequested)
              {
                break;
              }

              if (!MtpTreeFilter.Matches(testClass, testCase, filter))
              {
                continue;
              }

              TestNodeInfo node = new
              (
                Uid: testCase.Uid,
                DisplayName: testCase.DisplayName,
                State: TestNodeState.Discovered,
                Parameters: testCase.Parameters,
                Method: method
              );
              await sink.OnTestDiscoveredAsync(node).ConfigureAwait(false);
            }
          }
        }
        else
        {
          // Selection filters omit methods (no Skipped nodes). Tag filter keeps Skipped semantics.
          // MTP uid/tree filters select reported cases, including individual [Input] rows.
          // NopFilter stays null so a full run still fails an empty class.
          Func<ReportedTestCase, bool>? casePredicate = filter is NopFilter
            ? null
            : testCase => MtpTreeFilter.Matches(testClass, testCase, filter);
          bool selectionFilter = casePredicate is not null || filterMethod is not null;
          TestRunStats stats = await TestRunner.RunTestsAsync(
            testClass,
            sink,
            filterTag,
            methodNameContains: filterMethod,
            casePredicate: casePredicate,
            strictSignatures: strictSignatures,
            allowEmpty: allowEmpty,
            cancellationToken: cancellationToken
          ).ConfigureAwait(false);
          if (stats.SignatureWarnings.Count > 0 && (!selectionFilter || stats.TotalTests > 0))
          {
            foreach (string warning in stats.SignatureWarnings)
            {
              await outputDevice.DisplayAsync(
                this,
                new WarningMessageOutputDeviceData(warning),
                cancellationToken).ConfigureAwait(false);
            }
          }
        }
      }
    }
    catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
    {
      // The in-flight node is already Cancelled. Do not publish a synthetic Error.
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

}
