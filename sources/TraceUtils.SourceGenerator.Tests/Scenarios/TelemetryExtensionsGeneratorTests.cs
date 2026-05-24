using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TraceUtils.SourceGenerator.Tests.Infrastructure;
using System.Text;

namespace TraceUtils.SourceGenerator.Tests.Scenarios;

internal class TelemetryExtensionsGeneratorTests
{
    [Test]
    public Task GeneratesBasicSyncMethods()
    {
        var source = EmbeddedSourceReader.ReadSource("AllSignaturesMethods.cs");
        var generated = NormalizeGenerated(GeneratorTestHarness.Run(source).Single().Value);
        AssertValidCSharpSyntax(generated);
        return VerifyGenerated(generated);
    }

    private static void AssertValidCSharpSyntax(string source)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        var errors = CSharpSyntaxTree.ParseText(source, parseOptions)
            .GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.That(errors, Is.Empty, () => string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    private static Task VerifyGenerated(string generated)
    {
        return Verify(generated)
            .UseDirectory("Verify");
    }

    private static string NormalizeGenerated(string generated)
    {
        var normalized = generated
            .Replace("\r\n", "\n")
            .Replace('\t', ' ');

        var lines = normalized.Split('\n');
        var builder = new StringBuilder(normalized.Length);
        var blankLineCount = 0;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var isBlank = string.IsNullOrWhiteSpace(line);

            if (isBlank)
            {
                blankLineCount++;
                if (blankLineCount > 1)
                    continue;
            }
            else
            {
                blankLineCount = 0;
            }

            builder.AppendLine(line);
        }

        return builder.ToString().TrimEnd() + Environment.NewLine;
    }
}
