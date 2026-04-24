using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Infrastructure;

/// <summary>
/// Преобразует символы Roslyn в модель сигнатуры метода для последующей генерации кода.
/// </summary>
internal static class TelemetryMethodSignatureTransformer
{
    public static MethodContextInfo Transform(GeneratorAttributeSyntaxContext attrSyntaxContext, CancellationToken cancellationToken)
    {
        var methodSymbol = (IMethodSymbol)attrSyntaxContext.TargetSymbol;

        var containingNamespace = methodSymbol.ContainingNamespace.ToDisplayString();
        var containingTypeName = methodSymbol.ContainingType.Name;

        var (operationName, activityType, inputParametersName, outputParametersName) = ExtractOperationInfo(attrSyntaxContext.Attributes);

        return new MethodContextInfo(
            MethodSymbol: methodSymbol,
            ContainingNamespace: containingNamespace,
            ContainingTypeName: containingTypeName,
            OperationName: operationName,
            ActivityType: activityType,
            InputParametersName: inputParametersName,
            OutputParametersName: outputParametersName
        );
    }

    private static (string OperationName, string ActivityType, string InputParametersName, string OutputParametersName) ExtractOperationInfo(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.AttributeClass?.ToDisplayString() == TelemetryGenerationConstants.ActivityOperationAttributeName)
            {
                var operationName = attr.ConstructorArguments.Length > 0
                    ? attr.ConstructorArguments[0].Value?.ToString() ?? "UnknownOperation"
                    : "UnknownOperation";

                var activityType = "Internal";
                if (attr.ConstructorArguments.Length > 1)
                {
                    var activityTypeArg = attr.ConstructorArguments[1];
                    if (activityTypeArg.Type is INamedTypeSymbol enumType && enumType.EnumUnderlyingType != null)
                    {
                        var enumValue = (int)(activityTypeArg.Value ?? 0);
                        var enumMember = enumType.GetMembers()
                            .OfType<IFieldSymbol>()
                            .FirstOrDefault(f => f.HasConstantValue && Equals(f.ConstantValue, enumValue));

                        activityType = enumMember?.Name ?? "Internal";
                    }
                }

                var inputParametersName = attr.ConstructorArguments.Length > 2
                    ? attr.ConstructorArguments[2].Value?.ToString() ?? "input.parameters"
                    : "input.parameters";

                var outputParametersName = attr.ConstructorArguments.Length > 3
                    ? attr.ConstructorArguments[3].Value?.ToString() ?? "output.parameters"
                    : "output.parameters";

                return (operationName, activityType, inputParametersName, outputParametersName);
            }
        }

        return ("UnknownOperation", "Internal", "input.parameters", "output.parameters");
    }
}

