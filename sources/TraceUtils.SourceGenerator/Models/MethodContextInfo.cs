using Microsoft.CodeAnalysis;

namespace TraceUtils.SourceGenerator.Models;

/// <summary>
/// 
/// </summary>
/// <param name="MethodSymbol"></param>
/// <param name="ContainingNamespace"></param>
/// <param name="ContainingTypeName"></param>
/// <param name="OperationName"></param>
/// <param name="ActivityType"></param>
public record struct MethodContextInfo(
    IMethodSymbol MethodSymbol,
    string ContainingNamespace,
    string ContainingTypeName,
    string OperationName,
    string ActivityType
);
