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

        var (operationName, activityType, inputParametersName, outputParametersName, writeTagsToDictionary) =
            ExtractOperationInfo(attrSyntaxContext.Attributes);

        return new MethodContextInfo(
            MethodSymbol: methodSymbol,
            ContainingNamespace: containingNamespace,
            ContainingTypeName: containingTypeName,
            OperationName: operationName,
            ActivityType: activityType,
            InputParametersName: inputParametersName,
            OutputParametersName: outputParametersName,
            WriteTagsToDictionary: writeTagsToDictionary
        );
    }

    private static (string OperationName, string ActivityType, string InputParametersName, string OutputParametersName, bool WriteTagsToDictionary) ExtractOperationInfo(ImmutableArray<AttributeData> attributes)
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

                var writeTagsToDictionary = attr.ConstructorArguments.Length > 4
                    && attr.ConstructorArguments[4].Value is bool writeTagsFromCtor
                    && writeTagsFromCtor;

                foreach (var namedArg in attr.NamedArguments)
                {
                    if (namedArg.Key == "WriteTagsToDictionary" && namedArg.Value.Value is bool writeTags)
                    {
                        writeTagsToDictionary = writeTags;
                    }
                    else if (namedArg.Key == "InputParametersName" && namedArg.Value.Value is string inputName)
                    {
                        inputParametersName = inputName;
                    }
                    else if (namedArg.Key == "OutputParametersName" && namedArg.Value.Value is string outputName)
                    {
                        outputParametersName = outputName;
                    }
                }

                return (operationName, activityType, inputParametersName, outputParametersName, writeTagsToDictionary);
            }
        }

        return ("UnknownOperation", "Internal", "input.parameters", "output.parameters", false);
    }
}

