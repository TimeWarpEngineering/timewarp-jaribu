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

  // Names that are assertions only on Assert. Anywhere else they hide a test with no assertion.
  private static readonly ImmutableHashSet<string> NotProof = ImmutableHashSet.Create(
    StringComparer.Ordinal,
    "Contains",
    "Empty",
    "Single",
    "True",
    "Null");

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
    if (context.Node is not MethodDeclarationSyntax method || !IsTest(context, method))
    {
      return;
    }

    if (method.Body is null)
    {
      if (method.ExpressionBody is not null &&
          !HasAssertion(method.ExpressionBody) &&
          !HasProductionCall(method.ExpressionBody) &&
          !HasObjectCreation(method.ExpressionBody))
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

    if (!HasAssertion(method.Body) && !HasProductionCall(method.Body) && !HasObjectCreation(method.Body))
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
      if (ClearsLiterals(statement))
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
    if (statement is LocalDeclarationStatementSyntax local)
    {
      foreach (VariableDeclaratorSyntax declarator in local.Declaration.Variables)
      {
        string name = declarator.Identifier.ValueText;
        Forget(assigned, name);
        if (declarator.Initializer?.Value is LiteralExpressionSyntax literal)
        {
          assigned.Add(new AssignedLiteral(name, literal.Token.Text));
        }
      }
    }

    foreach (SyntaxNode node in statement.DescendantNodesAndSelf())
    {
      if (node is AssignmentExpressionSyntax assignment && assignment.Left is IdentifierNameSyntax target)
      {
        string name = target.Identifier.ValueText;
        Forget(assigned, name);
        // A literal inside if, foreach, or a lambda did not necessarily run.
        // Only the statement's own assignment is remembered. Nested writes forget.
        if (assignment.Parent == statement &&
            assignment.Kind() == SyntaxKind.SimpleAssignmentExpression &&
            assignment.Right is LiteralExpressionSyntax literal)
        {
          assigned.Add(new AssignedLiteral(name, literal.Token.Text));
        }
      }
      else if (node is PrefixUnaryExpressionSyntax prefix &&
               prefix.Operand is IdentifierNameSyntax prefixTarget &&
               prefix.Kind() is SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression)
      {
        Forget(assigned, prefixTarget.Identifier.ValueText);
      }
      else if (node is PostfixUnaryExpressionSyntax postfix &&
               postfix.Operand is IdentifierNameSyntax postfixTarget &&
               postfix.Kind() is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression)
      {
        Forget(assigned, postfixTarget.Identifier.ValueText);
      }
    }
  }

  private static void Forget(List<AssignedLiteral> assigned, string name)
  {
    assigned.RemoveAll(entry => entry.Name == name);
  }

  private static bool ClearsLiterals(StatementSyntax statement)
  {
    if (HasProductionCall(statement) || HasObjectCreation(statement))
    {
      return true;
    }

    foreach (MemberAccessExpressionSyntax access in statement.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>())
    {
      if (access.Parent is InvocationExpressionSyntax invocation && IsAssertion(invocation))
      {
        continue;
      }

      if (NonAssertions.Contains(access.Name.Identifier.ValueText))
      {
        continue;
      }

      return true;
    }

    return false;
  }

  private static bool TryAssignedLiteral(
    InvocationExpressionSyntax invocation,
    List<AssignedLiteral> assigned,
    out string literalText)
  {
    literalText = string.Empty;
    if (!IsAssertion(invocation) ||
        invocation.Expression is not MemberAccessExpressionSyntax member ||
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
    if (!IsAssertion(invocation) || invocation.Expression is not MemberAccessExpressionSyntax member)
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

  private static bool IsTest(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
  {
    if (method.Body is null && method.ExpressionBody is null)
    {
      return false;
    }

    if (context.SemanticModel.GetDeclaredSymbol(method) is not IMethodSymbol symbol)
    {
      return false;
    }

    if (symbol.DeclaredAccessibility != Accessibility.Public || !symbol.IsStatic || symbol.IsGenericMethod)
    {
      return false;
    }

    if (symbol.ReturnType is not INamedTypeSymbol returnType || returnType.IsGenericType)
    {
      return false;
    }

    if (returnType.Name != "Task" ||
        returnType.ContainingNamespace.ToDisplayString() != "System.Threading.Tasks")
    {
      return false;
    }

    if (symbol.Name is "Setup" or "CleanUp" or "SetupOnce" or "CleanUpOnce")
    {
      return false;
    }

    return !HasKnownTautology(context, method);
  }

  private static bool HasKnownTautology(SyntaxNodeAnalysisContext context, MethodDeclarationSyntax method)
  {
    foreach (AttributeListSyntax list in method.AttributeLists)
    {
      foreach (AttributeSyntax attribute in list.Attributes)
      {
        if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is not IMethodSymbol constructor)
        {
          continue;
        }

        if (constructor.ContainingType is { Name: "KnownTautologyAttribute" } type &&
            type.ContainingNamespace.ToDisplayString() == "TimeWarp.Jaribu")
        {
          return true;
        }
      }
    }

    return false;
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
    if (node.DescendantNodesAndSelf().OfType<ThrowStatementSyntax>().Any() ||
        node.DescendantNodesAndSelf().OfType<ThrowExpressionSyntax>().Any())
    {
      return true;
    }

    foreach (InvocationExpressionSyntax invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
    {
      if (IsAssertion(invocation))
      {
        return true;
      }
    }

    return false;
  }

  private static bool HasObjectCreation(SyntaxNode node)
  {
    return node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().Any() ||
      node.DescendantNodesAndSelf().OfType<ImplicitObjectCreationExpressionSyntax>().Any();
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

      if (!IsAssertion(invocation) && !NonAssertions.Contains(name) && !NotProof.Contains(name))
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

  private static bool IsAssertion(InvocationExpressionSyntax invocation)
  {
    string? name = InvocationName(invocation);
    if (name is null)
    {
      return false;
    }

    if (name.StartsWith("Should", StringComparison.Ordinal))
    {
      return true;
    }

    if (name is "Equal" or "AreEqual" or "NotEqual" or "AreNotEqual"
        or "False" or "Fail" or "NotNull" or "Same" or "NotSame")
    {
      return true;
    }

    if (name is not ("True" or "Null" or "Contains" or "Empty" or "Single"))
    {
      return false;
    }

    return invocation.Expression is MemberAccessExpressionSyntax member && IsAssertReceiver(member.Expression);
  }

  private static bool IsAssertReceiver(ExpressionSyntax expression)
  {
    return expression switch
    {
      IdentifierNameSyntax identifier => identifier.Identifier.ValueText == "Assert",
      MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText == "Assert",
      _ => false
    };
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
