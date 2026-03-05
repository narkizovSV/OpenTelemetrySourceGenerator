using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TraceUtils.SourceGenerator.Generation;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator;

/// <summary>
/// Инкрементальный генератор расширений с телеметрией для методов, помеченных атрибутами трассировки.
/// </summary>
[Generator]
public class TelemetryExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methodsWithTraceOperation = context.SyntaxProvider.ForAttributeWithMetadataName<MethodSignatureInfo>(
            fullyQualifiedMetadataName: TelemetryGenerationConstants.TraceEventAttributeName,
            predicate: TelemetryMethodSignatureTransformer.Predicate,
            transform: TelemetryMethodSignatureTransformer.Transform);

        var groupedByInterface = methodsWithTraceOperation
            .Collect()
            .SelectMany(static (methods, _) => methods
                .GroupBy(m => (m.ContainingNamespace, m.ContainingType))
                .Select(g => (Key: g.Key, Methods: g.ToImmutableArray())));

        context.RegisterSourceOutput(groupedByInterface, TelemetryGroupedFileEmitter.GenerateGroupedFiles);
    }
}