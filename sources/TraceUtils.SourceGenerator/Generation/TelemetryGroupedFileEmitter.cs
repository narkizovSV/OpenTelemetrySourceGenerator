using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Infrastructure;

/// <summary>
/// Генерирует сгруппированные файлы с extension-методами телеметрии.
/// </summary>
internal static class TelemetryGroupedFileEmitter
{
    public static void GenerateGroupedFiles(SourceProductionContext context, ((string Namespace, string Type) Key, ImmutableArray<MethodContextInfo> Methods) group)
    {
        var simpleTypeName = GetSimpleTypeName(group.Key.Type);

        var methodsBuilder = new StringBuilder();
        foreach (var method in group.Methods)
        {
            methodsBuilder.AppendLine(TelemetryExtensionMethodRenderer.GenerateExtensionMethodBody(method));
            methodsBuilder.AppendLine();
        }

        var source = TelemetryGenerationConstants.GroupedFileTemplate
            .Replace("{{ContainingNamespace}}", group.Key.Namespace)
            .Replace("{{ClassName}}", simpleTypeName)
            .Replace("{{Methods}}", methodsBuilder.ToString());

        context.AddSource($"{simpleTypeName}TelemetryExtensions.g.cs", source);
    }

    private static string GetSimpleTypeName(string typeName)
    {
        if (typeName.StartsWith("I") && typeName.Length > 1 && char.IsUpper(typeName[1]))
        {
            return typeName.Substring(1);
        }

        return typeName;
    }
}
