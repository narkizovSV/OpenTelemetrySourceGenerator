using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator.Generation;

/// <summary>
/// Генерирует итоговые `.g.cs` файлы, группируя методы по интерфейсу.
/// </summary>
internal static class TelemetryGroupedFileEmitter
{
    public static void GenerateGroupedFiles(
        SourceProductionContext context,
        ((string Namespace, string Type) Key, ImmutableArray<MethodSignatureInfo> Methods) group)
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

    private static string GetSimpleTypeName(string fullyQualifiedName)
    {
        var parts = fullyQualifiedName.Split('.');
        var simpleName = parts[parts.Length - 1];

        if (simpleName.StartsWith("global::", StringComparison.Ordinal))
            simpleName = simpleName.Substring("global::".Length);

        if (simpleName.StartsWith("I", StringComparison.Ordinal) && simpleName.Length > 1 && char.IsUpper(simpleName[1]))
            simpleName = simpleName.Substring(1);

        return simpleName;
    }
}
