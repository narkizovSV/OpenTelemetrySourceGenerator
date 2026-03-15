using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using TraceUtils.SourceGenerator.Infrastructure;
using TraceUtils.SourceGenerator.Models;

namespace TraceUtils.SourceGenerator;

/// <summary>
/// Инкрементальный генератор расширений с телеметрией для методов, помеченных атрибутами трассировки.
/// Файлы генерируются только если в проекте доступен TraceUtils.Utils.HasListeners (проект ссылается на TraceUtils).
/// </summary>
[Generator]
public class TelemetryExtensionsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // абстрактные классы ограничить
        var methodsWithTraceOperation = context.SyntaxProvider.ForAttributeWithMetadataName<MethodContextInfo>(
            fullyQualifiedMetadataName: TelemetryGenerationConstants.ActivityOperationAttributeName,
            predicate: TelemetryMethodSignatureFilter.Predicate,
            transform: TelemetryMethodSignatureTransformer.Transform);

        var allMethods = methodsWithTraceOperation.Collect();

        var groupedByInterface = methodsWithTraceOperation
            .Collect()
            .SelectMany(static (methods, _) => methods
                .GroupBy(m => (m.ContainingNamespace, m.ContainingTypeName))
                .Select(g => (Key: g.Key, Methods: g.ToImmutableArray())));

        context.RegisterSourceOutput(groupedByInterface, TelemetryGroupedFileEmitter.GenerateGroupedFiles);
    }
}
