#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Radzen.Blazor.Pdf.Tests;

internal static class PdfTestResources
{
    private const string ResourceRoot = "Radzen.Blazor.Tests.Pdf.Resources.";
    private static readonly Assembly Assembly = typeof(PdfTestResources).Assembly;

    public static Stream Open(string relativePath)
    {
        var name = ResourceRoot + relativePath.Replace('\\', '.').Replace('/', '.');
        var stream = Assembly.GetManifestResourceStream(name);
        if (stream == null)
        {
            var available = string.Join(Environment.NewLine, Assembly.GetManifestResourceNames().OrderBy(n => n));
            throw new FileNotFoundException($"Embedded resource '{name}' not found. Available resources:{Environment.NewLine}{available}");
        }

        return stream;
    }

    public static byte[] ReadAllBytes(string relativePath)
    {
        using var stream = Open(relativePath);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
