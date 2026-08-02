using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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
        var rendered = new DocumentRenderer().Render(MakeBuilder());
        rendered.CompressOutput = true;

        var bytes = rendered.ToArray();

        var reader = DocumentReader.Parse(bytes);
        Assert.NotNull(reader.Resolve(reader.Trailer["Root"]!));
    }

    [Fact]
    public void CompressOutput_ShrinksOutput()
    {
        var plain = new DocumentRenderer().ToArray(MakeBuilder());

        var rendered = new DocumentRenderer().Render(MakeBuilder());
        rendered.CompressOutput = true;
        var compressed = rendered.ToArray();

        Assert.True(compressed.Length < plain.Length,
            $"expected compressed ({compressed.Length}) < plain ({plain.Length})");
    }

    [Fact]
    public void CompressOutput_DefaultsToPlain()
    {
        var a = new DocumentRenderer().ToArray(MakeBuilder());

        var rendered = new DocumentRenderer().Render(MakeBuilder());
        rendered.CompressOutput = false;
        var b = rendered.ToArray();

        Assert.Equal(a.Length, b.Length);
    }
}
