using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;

namespace TraceUtils.SourceGenerator.Tests.Infrastructure;

internal static class GeneratorTestHarness
{
    public static ImmutableDictionary<string, string> Run(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        IIncrementalGenerator generator = new TelemetryExtensionsGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        var result = driver.GetRunResult();
        return result.Results[0]
            .GeneratedSources
            .ToImmutableDictionary(
                sourceResult => sourceResult.HintName,
                sourceResult => sourceResult.SourceText.ToString());
    }

    private static ImmutableArray<MetadataReference> GetMetadataReferences()
    {
        // возвращает строку с путями ко всем «доверенным» сборкам платформы
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (string.IsNullOrWhiteSpace(tpa))
        {
            throw new InvalidOperationException("TRUSTED_PLATFORM_ASSEMBLIES is not available.");
        }

        var references = tpa
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

        return references.Add(MetadataReference.CreateFromFile(typeof(ActivityOperationAttribute).Assembly.Location));
    }
}
