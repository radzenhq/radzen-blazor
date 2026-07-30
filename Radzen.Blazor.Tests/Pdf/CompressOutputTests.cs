using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class CompressOutputTests
{
    private static Document MakeBuilder()
    {
        var document = new Document();
        var section = document.Sections.Add();
        for (var i = 0; i < 40; i++)
        {
            section.Blocks.AddParagraph($"Compressible line number {i} with repeated filler text.");
        }
        return document;
    }

    [Fact]
    public void CompressOutput_ProducesReadableFile()
    {
        var document = MakeBuilder();
        var builderRenderer = new DocumentRenderer();
        builderRenderer.CompressOutput = true;

        var bytes = builderRenderer.ToArray(document);

        var reader = DocumentReader.Parse(bytes);
        Assert.NotNull(reader.Resolve(reader.Trailer["Root"]!));
    }

    [Fact]
    public void CompressOutput_ShrinksOutput()
    {
        var plain = new DocumentRenderer().ToArray(MakeBuilder());

        var compressedBuilderRenderer = new DocumentRenderer();
        var compressedBuilder = MakeBuilder();
        compressedBuilderRenderer.CompressOutput = true;
        var compressed = compressedBuilderRenderer.ToArray(compressedBuilder);

        Assert.True(compressed.Length < plain.Length,
            $"expected compressed ({compressed.Length}) < plain ({plain.Length})");
    }

    [Fact]
    public void CompressOutput_DefaultsToPlain()
    {
        var a = new DocumentRenderer().ToArray(MakeBuilder());
var explicitFalseRenderer = new DocumentRenderer();

        var explicitFalse = MakeBuilder();
        explicitFalseRenderer.CompressOutput = false;
        var b = explicitFalseRenderer.ToArray(explicitFalse);

        Assert.Equal(a.Length, b.Length);
    }
}
