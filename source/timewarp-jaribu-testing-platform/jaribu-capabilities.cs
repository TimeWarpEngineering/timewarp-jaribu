namespace TimeWarp.Jaribu.TestingPlatform;

#region Purpose
// Names the MTP capabilities Jaribu implements and the ones it leaves out.
#endregion

#region Design
// Tree-node filtering is supported. TestingPlatformBuilderHook registers
// AddTreeNodeFilterService, MtpTreeFilter matches /Assembly/Namespace/Type/Method,
// and this collection carries TreeNodeFilterCapability.
// MTP's ITestNodesTreeFilterTestFrameworkCapability is internal to the platform
// assembly, so a framework outside that assembly cannot implement it. The host
// logs that interface and still sends TreeNodeFilter when the option is registered.
// Omitted IGracefulStopTestExecutionCapability. The request CancellationToken
// is honored. Advertising graceful stop turns on --maximum-failed-tests, which
// calls StopTestExecutionAsync. That is a separate stop from the request token.
// Omitted IBannerMessageOwnerCapability. The platform banner is the message.
// Omitted the VSTest provider. INamedFeatureCapability is internal, and Jaribu
// does not bridge VSTest.
#endregion

/// <summary>
/// Capabilities Jaribu advertises to Microsoft.Testing.Platform.
/// Tree-node filtering is supported. Graceful stop, banner ownership, and the
/// VSTest provider are omitted; see the Design region for each reason.
/// </summary>
internal sealed class JaribuCapabilities : ITestFrameworkCapabilities
{
  private static readonly ITestFrameworkCapability[] Declared =
  [
    TreeNodeFilterCapability.Instance
  ];

  /// <summary>
  /// Gets the supported capabilities. The single entry is tree-node filtering.
  /// </summary>
  public IReadOnlyCollection<ITestFrameworkCapability> Capabilities => Declared;

  /// <summary>
  /// Marks that Jaribu honors <c>--treenode-filter</c>.
  /// </summary>
  internal sealed class TreeNodeFilterCapability : ITestFrameworkCapability
  {
    /// <summary>Shared instance placed on <see cref="JaribuCapabilities"/>.</summary>
    public static readonly TreeNodeFilterCapability Instance = new();

    private TreeNodeFilterCapability()
    {
    }
  }
}
