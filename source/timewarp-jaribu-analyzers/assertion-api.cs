namespace TimeWarp.Jaribu.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

#region Purpose
// Decides whether an invocation is an assertion from the method it binds to.
#endregion

#region Design
// The containing type decides, not the method name. Shouldly (any type in the Shouldly namespace)
// and Assert-style types (name ends in Assert: xUnit, NUnit, MSTest) are assertion APIs.
// Jaribu ships no assertion types. A production method named Should... is a production call.
#endregion

/// <summary>
/// Classifies invocations as assertions by their bound method's containing type.
/// </summary>
internal readonly struct AssertionApi
{
  private readonly SemanticModel model;
  private readonly CancellationToken cancellationToken;

  public AssertionApi(SemanticModel model, CancellationToken cancellationToken)
  {
    this.model = model;
    this.cancellationToken = cancellationToken;
  }

  public bool IsAssertion(InvocationExpressionSyntax invocation)
  {
    SymbolInfo info = model.GetSymbolInfo(invocation, cancellationToken);
    if (info.Symbol is IMethodSymbol method)
    {
      return IsAssertionType(method.ContainingType);
    }

    // Overload resolution failed: an assertion only when every candidate is one.
    if (info.CandidateSymbols.IsDefaultOrEmpty)
    {
      return false;
    }

    foreach (ISymbol candidate in info.CandidateSymbols)
    {
      if (candidate is not IMethodSymbol candidateMethod || !IsAssertionType(candidateMethod.ContainingType))
      {
        return false;
      }
    }

    return true;
  }

  private static bool IsAssertionType(INamedTypeSymbol? type)
  {
    for (INamedTypeSymbol? current = type; current is not null; current = current.ContainingType)
    {
      if (current.Name.EndsWith("Assert", StringComparison.Ordinal))
      {
        return true;
      }
    }

    INamespaceSymbol? root = type?.ContainingNamespace;
    while (root is { ContainingNamespace.IsGlobalNamespace: false })
    {
      root = root.ContainingNamespace;
    }

    return root is { IsGlobalNamespace: false, Name: "Shouldly" };
  }
}
