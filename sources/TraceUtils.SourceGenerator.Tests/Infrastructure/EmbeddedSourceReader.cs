using System.Reflection;
using System.Text;

namespace TraceUtils.SourceGenerator.Tests.Infrastructure;

/// <summary>
/// Читает встроенный файл в сборке
/// </summary>
internal static class EmbeddedSourceReader
{
    private static readonly Assembly Assembly = typeof(EmbeddedSourceReader).Assembly;

    public static string ReadSource(string fileName)
    {
        var resourceName = Assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.Ordinal));

        if (resourceName is null)
        {
            throw new InvalidOperationException($"Внедренный ресурс '{fileName}' не был найден.");
        }

        using var stream = Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource stream для '{resourceName}' не был найден.");

        using var reader = new StreamReader(stream, Encoding.UTF8);

        return reader.ReadToEnd();
    }
}
