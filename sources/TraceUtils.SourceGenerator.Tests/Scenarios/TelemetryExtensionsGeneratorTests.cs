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
        return VerifyGenerated(generated);
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
