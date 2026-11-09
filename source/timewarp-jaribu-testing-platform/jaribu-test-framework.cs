namespace TimeWarp.Jaribu.TestingPlatform;

using System.Globalization;
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
// handler publishes an error node for that failure and then returns. The Error
// node is the report. Rethrowing would also crash the framework. A throw from
// the publish is an AggregateException that keeps the original exception and
// the publish failure.
// Tree and uid filters go
// through MtpTreeFilter; a filter this adapter does not recognize throws too.
//
// context.CancellationToken is passed into the class run and into session
// close. A fired token stops the class loop. Cooperative cancellation is not
// republished as an Error node.
// CreateTestSessionAsync keeps the clear epoch BeginTestSession returns.
// CloseTestSessionAsync ends that epoch. A clear since the begin makes the
// close return without a session failure. A close with no stored epoch uses
// the parameterless end, which throws when no host session is active.
// The outermost close clears MethodSourceLocation caches after that attempt,
// including when dispose fails, so a long-lived host drops PDB maps.
// The adapter passes the raw --filter-tag argument, or null when that option
// is unset, into DiscoverTestCases and RunTestsAsync. Those methods trim it
// and read JARIBU_FILTER_TAG only when the argument is null. A class or
// method whose tags exist and none match is one method-level node.
// Discovery lists that node. The run reports it as Skipped. Uid-list
// equality stays in MtpTreeFilter. This assembly does not call core internals.
// --strict-signatures and --allow-empty are passed to RunTestsAsync.
// Signature warnings go to the output device when the class produced nodes,
// or when the MTP filter is NopFilter and no method filter is set.
// A uid or tree filter passes a case predicate, so an empty class that was
// not selected does not emit NoTests. The same predicate omits strict
// near-miss Error nodes when that class produced no selected nodes. The
// warning display already hides that class.
// The dotnet test pipe replaces the terminal device. Protocol 1.3.0 and
// later forward WarningMessageOutputDeviceData. SDK 10.0.403 speaks only
// 1.0.0 and drops that message, so a passing run would hide the near-miss.
// HostDropsOutputDeviceWarnings reads ProxyOutputDevice.OriginalOutputDevice,
// DotnetTestPassthroughOutputDevice, and
// IPushOnlyProtocol.IsDisplayMessageForwardingSupported.
// An unresolved IPushOnlyProtocol counts as a host that drops the warning.
// An exception that escapes the request publishes one Error node and then returns.
// That node's uid is Jaribu.Error.{ExceptionType.FullName}. One request publishes
// at most one such node, so the exception type is enough to keep the uid stable.
// When the host drops it, and strict mode did not already publish an Error,
// the same text is published as a skipped node. That is the line the SDK 10
// reporter prints. A host that forwards the warning does not get the node.
#endregion

/// <summary>
/// Implementation of ITestFramework for Microsoft.Testing.Platform integration.
/// Bridges Jaribu's test runner with M.T.P. for test discovery and execution.
/// </summary>
internal sealed class JaribuTestFramework : ITestFramework, IDataProducer, IOutputDeviceDataProducer
{
  private readonly IExtension Extension;
  private readonly IServiceProvider ServiceProvider;
  private readonly Lock SessionEpochGate = new();
  private readonly Stack<long> SessionClearEpochs = new();

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

    long clearEpoch = TestRunner.BeginTestSession();
    lock (SessionEpochGate)
    {
      SessionClearEpochs.Push(clearEpoch);
    }

    return Task.FromResult(new CreateTestSessionResult { IsSuccess = true });
  }

  public async Task<CloseTestSessionResult> CloseTestSessionAsync(CloseTestSessionContext context)
  {
    bool clearLocations = false;
    try
    {
      long? clearEpoch;
      lock (SessionEpochGate)
      {
        clearEpoch = SessionClearEpochs.Count == 0
          ? null
          : SessionClearEpochs.Pop();
        clearLocations = SessionClearEpochs.Count == 0;
      }

      if (clearEpoch is long epoch)
      {
        await TestRunner.EndTestSessionAsync(epoch, context.CancellationToken).ConfigureAwait(false);
      }
      else
      {
        await TestRunner.EndTestSessionAsync(context.CancellationToken).ConfigureAwait(false);
      }

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
    finally
    {
      if (clearLocations)
      {
        MethodSourceLocation.Clear();
      }
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
      bool hostDropsWarnings = HostDropsOutputDeviceWarnings(outputDevice);

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
          // Same helper and the same arguments as the run below, so the listed
          // nodes are the nodes the run will produce.
          Func<ReportedTestCase, bool>? discoveryPredicate = filter is NopFilter
            ? null
            : testCase => MtpTreeFilter.Matches(testClass, testCase, filter);
          foreach (ReportedTestCase testCase in TestRunner.DiscoverTestCases(
            testClass,
            filterTag,
            filterMethod,
            casePredicate: discoveryPredicate))
          {
            if (cancellationToken.IsCancellationRequested)
            {
              break;
            }

            TestNodeInfo node = new
            (
              Uid: testCase.Uid,
              DisplayName: testCase.DisplayName,
              State: TestNodeState.Discovered,
              Parameters: testCase.Parameters,
              Method: testCase.Method
            );
            await sink.OnTestDiscoveredAsync(node).ConfigureAwait(false);
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
            for (int index = 0; index < stats.SignatureWarnings.Count; index++)
            {
              string warning = stats.SignatureWarnings[index];
              await outputDevice.DisplayAsync(
                this,
                new WarningMessageOutputDeviceData(warning),
                cancellationToken).ConfigureAwait(false);

              // Strict mode already published an Error for this method.
              if (hostDropsWarnings && !strictSignatures)
              {
                string owner = testClass.FullName ?? testClass.Name;
                string indexText = index.ToString(CultureInfo.InvariantCulture);
                await sink.OnTestCompletedAsync(new TestNodeInfo(
                  Uid: $"{owner}#signature-warning-{indexText}",
                  DisplayName: WarningDisplayName(warning),
                  State: TestNodeState.Skipped,
                  Message: warning)).ConfigureAwait(false);
              }
            }
          }
        }
      }
    }
    catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
    {
      // The in-flight node is already Cancelled. Do not publish a synthetic Error.
    }
#pragma warning disable CA1031 // Host boundary: the Error node is the report for any other failure.
    catch (Exception ex)
    {
      try
      {
        await ReportUnhandledExceptionAsync(context, ex).ConfigureAwait(false);
      }
      catch (Exception publishFailure)
      {
        throw new AggregateException(
          "Jaribu could not publish the unhandled-exception node.",
          ex,
          publishFailure);
      }
    }
#pragma warning restore CA1031
    finally
    {
      context.Complete();
    }
  }

  private static string? ResolveFilterTag(ICommandLineOptions commandLine)
  {
    // Validation rejects a blank argument. Core trims a set value and reads the environment only for null.
    return GetOptionArgument(commandLine, JaribuCommandLineOptionsProvider.FilterTagOption);
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

  private static string WarningDisplayName(string warning)
  {
    const string Prefix = "Near-miss test '";
    if (!warning.StartsWith(Prefix, StringComparison.Ordinal))
    {
      return warning;
    }

    int end = warning.IndexOf('\'', Prefix.Length);
    if (end <= Prefix.Length)
    {
      return warning;
    }

    string qualified = warning[Prefix.Length..end];
    int dot = qualified.LastIndexOf('.');
    return dot >= 0 && dot < qualified.Length - 1
      ? qualified[(dot + 1)..]
      : qualified;
  }

  private bool HostDropsOutputDeviceWarnings(IOutputDevice outputDevice)
  {
    // GetOutputDevice returns a proxy. The pipe host sits on OriginalOutputDevice.
    object device = outputDevice;
    if (string.Equals(device.GetType().Name, "ProxyOutputDevice", StringComparison.Ordinal))
    {
      PropertyInfo? original = device.GetType().GetProperty(
        "OriginalOutputDevice",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
      if (original?.GetValue(device) is object inner)
      {
        device = inner;
      }
    }

    const string PassthroughName = "DotnetTestPassthroughOutputDevice";
    if (!string.Equals(device.GetType().Name, PassthroughName, StringComparison.Ordinal))
    {
      return false;
    }

    Type? protocolType = device.GetType().Assembly.GetType(
      "Microsoft.Testing.Platform.ServerMode.IPushOnlyProtocol");
    object? protocol = protocolType is null ? null : ServiceProvider.GetService(protocolType);
    if (protocol is null)
    {
      return true;
    }

    PropertyInfo? supported = protocol.GetType().GetProperty("IsDisplayMessageForwardingSupported");
    return supported?.GetValue(protocol) is not true;
  }

  internal static string UnhandledErrorUid(Exception exception)
  {
    ArgumentNullException.ThrowIfNull(exception);
    Type type = exception.GetType();
    string typeName = type.FullName ?? type.Name;
    return $"Jaribu.Error.{typeName}";
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
          Uid = new TestNodeUid(UnhandledErrorUid(ex)),
          DisplayName = $"Unhandled: {ex.GetType().Name}",
          Properties = new PropertyBag(new ErrorTestNodeStateProperty(ex))
        }
      )
    ).ConfigureAwait(false);
  }

}
