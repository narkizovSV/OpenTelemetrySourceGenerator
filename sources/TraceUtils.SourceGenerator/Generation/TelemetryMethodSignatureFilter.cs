using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TraceUtils.SourceGenerator.Infrastructure;

/// <summary>
/// Логика поиска интересующих нас методов в синтаксическом дереве
/// </summary>
internal static class TelemetryMethodSignatureFilter
{
    public static bool Predicate(SyntaxNode node, CancellationToken cancellationToken)
    {
        return node is MethodDeclarationSyntax m && m.AttributeLists.Count > 0;
    }
}