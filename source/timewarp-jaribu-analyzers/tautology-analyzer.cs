namespace TimeWarp.Jaribu.Analyzers;

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

#region Purpose
// Compile-time tautology checks: empty body, no assertion, self-comparison, assigned literal.
#endregion

/// <summary>
/// Flags test methods that cannot fail: empty, no assertion, self-comparison, or a literal just assigned.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TautologyAnalyzer : DiagnosticAnalyzer
{
  /// <summary>Empty test body.</summary>
  public const string EmptyId = "JARIBU1001";

  /// <summary>No assertion can run.</summary>
  public const string NoAssertionId = "JARIBU1002";

  /// <summary>A value is compared with itself.</summary>
  public const string SelfComparisonId = "JARIBU1003";

  /// <summary>An assertion repeats a literal that was just assigned.</summary>
  public const string AssignedLiteralId = "JARIBU1004";

  private const string HelpLink =
    "https://github.com/TimeWarpEngineering/timewarp-jaribu/blob/master/documentation/tautology.md";

  private static readonly DiagnosticDescriptor EmptyRule = Rule(
    EmptyId,
    "Test body is empty",
    "Test '{0}' has an empty body");

  private static readonly DiagnosticDescriptor NoAssertionRule = Rule(
    NoAssertionId,
    "Test reaches no assertion",
    "Test '{0}' reaches no assertion");

  private static readonly DiagnosticDescriptor SelfComparisonRule = Rule(
    SelfComparisonId,
    "Test compares a value with itself",
    "Test compares '{0}' with itself");

  private static readonly DiagnosticDescriptor AssignedLiteralRule = Rule(
    AssignedLiteralId,
    "Test asserts a literal it just assigned",
    "Test asserts {0}, which was just assigned");

  private static readonly ImmutableHashSet<string> NonAssertions = ImmutableHashSet.Create(
    StringComparer.Ordinal,
    "CompletedTask",
    "FromResult",
    "FromException",
    "Delay",
    "Yield",
    "Write",
    "WriteLine",
    "GetAwaiter",
    "GetResult",
    "ConfigureAwait");

  /// <inheritdoc />
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [EmptyRule, NoAssertionRule, SelfComparisonRule, AssignedLiteralRule];

  /// <inheritdoc />
  public override void Initialize(AnalysisContext context)
  {
    if (context is null)
    {
      throw new ArgumentNullException(nameof(context));
    }

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
  }

  private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
  {
    if (context.Node is not MethodDeclarationSyntax method || !IsTest(method))
    {
      return;
    }

    if (method.Body is null)
    {
      if (method.ExpressionBody is not null &&
          !HasAssertion(method.ExpressionBody) &&
          !HasProductionCall(method.ExpressionBody))
      {
        context.ReportDiagnostic(Diagnostic.Create(
          NoAssertionRule,
          method.Identifier.GetLocation(),
          method.Identifier.Text));
      }

      return;
    }

    if (IsEmpty(method.Body))
    {
      context.ReportDiagnostic(Diagnostic.Create(EmptyRule, method.Identifier.GetLocation(), method.Identifier.Text));
      return;
    }

    if (!HasAssertion(method.Body) && !HasProductionCall(method.Body))
    {
      context.ReportDiagnostic(Diagnostic.Create(NoAssertionRule, method.Identifier.GetLocation(), method.Identifier.Text));
    }

    foreach (InvocationExpressionSyntax invocation in method.Body.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
      if (TrySelfComparison(invocation, out string symbol))
      {
        context.ReportDiagnostic(Diagnostic.Create(SelfComparisonRule, invocation.GetLocation(), symbol));
      }
    }

    ReportAssignedLiterals(context, method);
  }

  private static void ReportAssignedLiterals(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
  {
    if (method.Body is null)
    {
      return;
    }

    List<AssignedLiteral> assigned = [];
    foreach (StatementSyntax statement in method.Body.Statements)
    {
      if (HasProductionCall(statement))
      {
        assigned.Clear();
        continue;
      }

      foreach (InvocationExpressionSyntax invocation in statement.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
      {
        if (TryAssignedLiteral(invocation, assigned, out string literal))
        {
          context.ReportDiagnostic(Diagnostic.Create(AssignedLiteralRule, invocation.GetLocation(), literal));
        }
      }

      RememberLiterals(statement, assigned);
    }
  }

  private static void RememberLiterals(StatementSyntax statement, List<AssignedLiteral> assigned)
  {
    if (statement is not LocalDeclarationStatementSyntax local)
    {
      return;
    }

    foreach (VariableDeclaratorSyntax declarator in local.Declaration.Variables)
    {
      string name = declarator.Identifier.ValueText;
      assigned.RemoveAll(entry => entry.Name == name);
      if (declarator.Initializer?.Value is LiteralExpressionSyntax literal)
      {
        assigned.Add(new AssignedLiteral(name, literal.Token.Text));
      }
    }
  }

  private static bool TryAssignedLiteral(
    InvocationExpressionSyntax invocation,
    List<AssignedLiteral> assigned,
    out string literalText)
  {
    literalText = string.Empty;
    if (invocation.Expression is not MemberAccessExpressionSyntax member ||
        !IsAssertionName(member.Name.Identifier.Text) ||
        member.Expression is not IdentifierNameSyntax receiver ||
        invocation.ArgumentList.Arguments.Count == 0 ||
        invocation.ArgumentList.Arguments[0].Expression is not LiteralExpressionSyntax literal)
    {
      return false;
    }

    string name = receiver.Identifier.ValueText;
    foreach (AssignedLiteral entry in assigned)
    {
      if (entry.Name == name && entry.Literal == literal.Token.Text)
      {
        literalText = literal.Token.Text;
        return true;
      }
    }

    return false;
  }

  private readonly struct AssignedLiteral
  {
    public AssignedLiteral(string name, string literal)
    {
      Name = name;
      Literal = literal;
    }

    public string Name { get; }

    public string Literal { get; }
  }

  private static bool TrySelfComparison(InvocationExpressionSyntax invocation, out string symbol)
  {
    symbol = string.Empty;
    if (invocation.Expression is not MemberAccessExpressionSyntax member ||
        !IsAssertionName(member.Name.Identifier.Text))
    {
      return false;
    }

    if (member.Expression is IdentifierNameSyntax receiver &&
        invocation.ArgumentList.Arguments.Count > 0 &&
        invocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax argument &&
        receiver.Identifier.ValueText == argument.Identifier.ValueText)
    {
      symbol = receiver.Identifier.ValueText;
      return true;
    }

    if (invocation.ArgumentList.Arguments.Count >= 2 &&
        invocation.ArgumentList.Arguments[0].Expression is IdentifierNameSyntax left &&
        invocation.ArgumentList.Arguments[1].Expression is IdentifierNameSyntax right &&
        left.Identifier.ValueText == right.Identifier.ValueText)
    {
      symbol = left.Identifier.ValueText;
      return true;
    }

    return false;
  }

  private static bool IsTest(MethodDeclarationSyntax method)
  {
    if (method.Body is null && method.ExpressionBody is null)
    {
      return false;
    }

    if (!method.Modifiers.Any(SyntaxKind.PublicKeyword) || !method.Modifiers.Any(SyntaxKind.StaticKeyword))
    {
      return false;
    }

    if (!IsNonGenericTask(method.ReturnType))
    {
      return false;
    }

    if (method.Identifier.ValueText is "Setup" or "CleanUp" or "SetupOnce" or "CleanUpOnce")
    {
      return false;
    }

    string ns = method.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>()?.Name.ToString() ?? string.Empty;
    if (ns.StartsWith("TimeWarp.Jaribu", StringComparison.Ordinal))
    {
      return false;
    }

    foreach (AttributeListSyntax list in method.AttributeLists)
    {
      foreach (AttributeSyntax attribute in list.Attributes)
      {
        string name = attribute.Name.ToString();
        if (name is "KnownTautology" or "KnownTautologyAttribute")
        {
          return false;
        }
      }
    }

    return true;
  }

  private static bool IsNonGenericTask(TypeSyntax type)
  {
    return type switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "Task",
      QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText == "Task",
      _ => false
    };
  }

  private static bool IsEmpty(BlockSyntax body)
  {
    foreach (StatementSyntax statement in body.Statements)
    {
      if (statement is not EmptyStatementSyntax)
      {
        return false;
      }
    }

    return true;
  }

  private static bool HasAssertion(SyntaxNode node)
  {
    foreach (InvocationExpressionSyntax invocation in node.DescendantNodes().OfType<InvocationExpressionSyntax>())
    {
      if (InvocationName(invocation) is string name && IsAssertionName(name))
      {
        return true;
      }
    }

    return false;
  }

  private static bool HasProductionCall(SyntaxNode node)
  {
    foreach (InvocationExpressionSyntax invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
    {
      string? name = InvocationName(invocation);
      if (name is null)
      {
        return true;
      }

      if (!IsAssertionName(name) && !NonAssertions.Contains(name))
      {
        return true;
      }
    }

    return false;
  }

  private static string? InvocationName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
      MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
      _ => null
    };
  }

  private static bool IsAssertionName(string name)
  {
    return name.StartsWith("Should", StringComparison.Ordinal)
      || name is "Equal" or "AreEqual" or "NotEqual" or "AreNotEqual"
        or "True" or "False" or "Fail" or "Null" or "NotNull"
        or "Same" or "NotSame" or "Contains" or "Empty" or "Single";
  }

  private static DiagnosticDescriptor Rule(string id, string title, string message)
  {
    return new DiagnosticDescriptor(
      id,
      title,
      message,
      "Tautology",
      DiagnosticSeverity.Warning,
      isEnabledByDefault: true,
      helpLinkUri: HelpLink);
  }
}
