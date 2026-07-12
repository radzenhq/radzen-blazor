using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

// Coverage of the DocumentBuilder.CompressOutput / Document.CompressOutput opt-in
// wired into DocumentWriter.UseCompressedStreams.
public class CompressOutputTests
{
    private static DocumentBuilder MakeBuilder()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        for (var i = 0; i < 40; i++)
        {
            section.Blocks.AddParagraph($"Compressible line number {i} with repeated filler text.");
        }
        return builder;
    }

    [Fact]
    public void CompressOutput_ProducesReadableFile()
    {
        var builder = MakeBuilder();
        builder.CompressOutput = true;

        var bytes = builder.ToArray();

        var reader = DocumentReader.Parse(bytes);
        Assert.NotNull(reader.Resolve(reader.Trailer["Root"]!));
    }

    [Fact]
    public void CompressOutput_ShrinksOutput()
    {
        var plain = MakeBuilder().ToArray();

        var compressedBuilder = MakeBuilder();
        compressedBuilder.CompressOutput = true;
        var compressed = compressedBuilder.ToArray();

        Assert.True(compressed.Length < plain.Length,
            $"expected compressed ({compressed.Length}) < plain ({plain.Length})");
    }

    [Fact]
    public void CompressOutput_DefaultsToPlain()
    {
        var a = MakeBuilder().ToArray();

        var explicitFalse = MakeBuilder();
        explicitFalse.CompressOutput = false;
        var b = explicitFalse.ToArray();

        Assert.Equal(a.Length, b.Length);
    }
}
