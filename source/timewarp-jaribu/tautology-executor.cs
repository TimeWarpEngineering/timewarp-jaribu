namespace TimeWarp.Jaribu;

using System.Globalization;
using System.Reflection;

#region Purpose
// Runs one test under coverage or prove mode and attaches the score.
#endregion

#region Design
// The baseline attempt is the result a normal run would report. Prove mode then
// breaks one claim at a time. A kill is a failing attempt. The following restore
// attempt must pass. A test that still passes while the member throws is unproven.
// No [Proves] uses the last distinct production method the baseline entered.
// KnownTautology skips the break and is not a strict failure.
// An assembly with no instrumentation marker and no hits is reported as missing
// instrumentation, not as zero coverage.
#endregion

/// <summary>Coverage collection and prove-mode attempts for one test case.</summary>
internal static class TautologyExecutor
{
  /// <summary>
  /// Runs <paramref name="runOnce"/> under <paramref name="options"/> and returns the node to publish.
  /// Options that are off delegate to a single attempt.
  /// </summary>
  public static async Task<TestNodeInfo> RunAsync(
    Func<CancellationToken, Task<TestNodeInfo>> runOnce,
    MethodInfo method,
    TautologyOptions options,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(runOnce);
    ArgumentNullException.ThrowIfNull(method);
    ArgumentNullException.ThrowIfNull(options);
    if (!options.Enabled)
    {
      return await runOnce(cancellationToken).ConfigureAwait(false);
    }

    (TestNodeInfo baseline, IReadOnlyList<string> hits) = await AttemptAsync(
      runOnce,
      method,
      brokenPrefix: null,
      cancellationToken).ConfigureAwait(false);
    List<string> production = ProductionHits(hits, method);
    string location = TautologyLocation.Format(method);
    double seconds = Seconds(baseline);
    TestRunner.MethodMetadata metadata = TestRunner.GetMethodMetadata(method);
    KnownTautologyAttribute? known = metadata.KnownTautology;
    ProvesAttribute[] explicitClaims = metadata.Proves();
    bool instrumented = TautologyProbe.AnyInstrumented();

    if (known is not null)
    {
      TautologyFinding knownFinding = Score(
        baseline,
        "known",
        known.Reason,
        location,
        production.Count,
        proven: 0,
        seconds,
        violation: false,
        known: true);
      return baseline with { Tautology = knownFinding };
    }

    if (!instrumented && production.Count == 0)
    {
      TautologyFinding missing = Score(
        baseline,
        "instrumentation",
        "Per-test coverage needs a build with JaribuInstrument=true.",
        location,
        covered: 0,
        proven: 0,
        seconds,
        violation: false,
        known: false);
      return baseline with { Tautology = missing };
    }

    if (baseline.State != TestNodeState.Passed)
    {
      TautologyFinding failed = Score(
        baseline,
        "failed",
        "The test failed before a claim was broken.",
        location,
        production.Count,
        proven: 0,
        seconds,
        violation: false,
        known: false);
      return baseline with { Tautology = failed };
    }

    bool missingClaim = options.RequireClaims && explicitClaims.Length == 0;
    if (production.Count == 0)
    {
      string reason = missingClaim
        ? "Missing [Proves] claim. Zero production methods covered."
        : "Zero production methods covered.";
      return Finish(
        baseline,
        Score(baseline, "coverage", reason, location, 0, 0, seconds, violation: true, known: false),
        options);
    }

    if (!options.Prove)
    {
      string reason = missingClaim
        ? "Missing [Proves] claim. Covered " + production.Count.ToString(CultureInfo.InvariantCulture) + " production methods."
        : "Covered " + production.Count.ToString(CultureInfo.InvariantCulture) + " production methods.";
      string rule = missingClaim ? "claim" : "coverage";
      return Finish(
        baseline,
        Score(baseline, rule, reason, location, production.Count, 0, seconds, missingClaim, known: false),
        options);
    }

    List<TautologyClaim> claims = [];
    if (explicitClaims.Length > 0)
    {
      foreach (ProvesAttribute proves in explicitClaims)
      {
        claims.Add(TautologyClaim.From(proves.Type, proves.MethodName));
      }
    }
    else if (!missingClaim && TautologyClaim.FromToken(production[^1]) is TautologyClaim inferred)
    {
      claims.Add(inferred);
    }

    if (claims.Count == 0)
    {
      return Finish(
        baseline,
        Score(
          baseline,
          "claim",
          "Missing [Proves] claim.",
          location,
          production.Count,
          0,
          seconds,
          violation: true,
          known: false),
        options);
    }

    int proven = 0;
    List<string> unproven = [];
    TestNodeInfo reported = baseline;
    foreach (TautologyClaim claim in claims)
    {
      if (cancellationToken.IsCancellationRequested)
      {
        break;
      }

      (TestNodeInfo broken, _) = await AttemptAsync(
        runOnce,
        method,
        claim.Prefix,
        cancellationToken).ConfigureAwait(false);
      seconds += Seconds(broken);
      if (!TestRunStats.IsFailingState(broken.State))
      {
        unproven.Add(claim.Display);
        continue;
      }

      (TestNodeInfo restored, _) = await AttemptAsync(
        runOnce,
        method,
        brokenPrefix: null,
        cancellationToken).ConfigureAwait(false);
      seconds += Seconds(restored);
      if (restored.State != TestNodeState.Passed)
      {
        unproven.Add(claim.Display + " (restore failed)");
        continue;
      }

      proven++;
      reported = restored;
    }

    bool inferredClaim = explicitClaims.Length == 0;
    string claimRule = inferredClaim ? "inferred:" + claims[0].Display : "claim:" + claims[0].Display;
    if (unproven.Count > 0)
    {
      string reason = "Unproven: " + string.Join(", ", unproven) + ".";
      if (missingClaim)
      {
        reason = "Missing [Proves] claim. " + reason;
      }

      return Finish(
        reported,
        Score(reported, claimRule, reason, location, production.Count, proven, seconds, violation: true, known: false),
        options);
    }

    string proved = "Proved " + proven.ToString(CultureInfo.InvariantCulture) + " claim(s).";
    if (missingClaim)
    {
      return Finish(
        reported,
        Score(
          reported,
          "claim",
          "Missing [Proves] claim. " + proved,
          location,
          production.Count,
          proven,
          seconds,
          violation: true,
          known: false),
        options);
    }

    return Finish(
      reported,
      Score(reported, claimRule, proved, location, production.Count, proven, seconds, violation: false, known: false),
      options);
  }

  private static async Task<(TestNodeInfo Node, IReadOnlyList<string> Hits)> AttemptAsync(
    Func<CancellationToken, Task<TestNodeInfo>> runOnce,
    MethodInfo method,
    string? brokenPrefix,
    CancellationToken cancellationToken)
  {
    TautologyProbe.Scope scope = TautologyProbe.Push(brokenPrefix);
    try
    {
      try
      {
        TestNodeInfo node = await runOnce(cancellationToken).ConfigureAwait(false);
        return (node, scope.Snapshot());
      }
      catch (ProbeBrokenException broken)
      {
        // A broken runner method can throw outside the test body. That still kills the attempt.
        string typeName = method.DeclaringType?.FullName ?? method.DeclaringType?.Name ?? "Test";
        string uid = typeName + "." + method.Name;
        TestNodeInfo failed = new(
          uid,
          method.Name,
          TestNodeState.Failed,
          TimeSpan.Zero,
          broken,
          broken.Message,
          Method: method);
        return (failed, scope.Snapshot());
      }
    }
    finally
    {
      TautologyProbe.Pop(scope);
    }
  }

  private static List<string> ProductionHits(IReadOnlyList<string> hits, MethodInfo method)
  {
    string testAssembly = method.DeclaringType?.Assembly.GetName().Name ?? string.Empty;
    List<string> production = [];
    foreach (string hit in hits)
    {
      int bar = hit.IndexOf('|', StringComparison.Ordinal);
      if (bar <= 0)
      {
        continue;
      }

      if (string.Equals(hit[..bar], testAssembly, StringComparison.Ordinal))
      {
        continue;
      }

      if (hit.Contains('<', StringComparison.Ordinal))
      {
        continue;
      }

      production.Add(hit);
    }

    return production;
  }

  private static TautologyFinding Score(
    TestNodeInfo node,
    string rule,
    string reason,
    string location,
    int covered,
    int proven,
    double seconds,
    bool violation,
    bool known)
  {
    double kills = proven == 0
      ? 0
      : seconds <= 0 ? double.PositiveInfinity : proven / seconds;
    return new TautologyFinding(
      node.Uid,
      rule,
      reason,
      location,
      covered,
      proven,
      seconds,
      kills,
      violation,
      known);
  }

  private static TestNodeInfo Finish(
    TestNodeInfo node,
    TautologyFinding finding,
    TautologyOptions options)
  {
    if (finding.Violation && options.Strict && node.State == TestNodeState.Passed)
    {
      return node with
      {
        State = TestNodeState.Failed,
        Message = finding.Reason,
        Exception = new TautologyException(finding.Reason),
        Tautology = finding
      };
    }

    return node with { Tautology = finding };
  }

  private static double Seconds(TestNodeInfo node)
  {
    return node.Duration?.TotalSeconds ?? 0;
  }
}
