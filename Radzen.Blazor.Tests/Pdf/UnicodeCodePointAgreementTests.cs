#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class UnicodeCodePointAgreementTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0xD800)]
    [InlineData(0xDFFF)]
    [InlineData(0x110000)]
    public void InvalidCodepoint_ExtractionAndEmbeddedToUnicodeAgree(int invalidCodepoint)
    {
        var font = Type0EmbedSupport.LoadLiberation();
        var gid = font.GetGlyphId('A');
        var source = new Dictionary<ushort, int> { [gid] = invalidCodepoint };

        var reverse = ReverseFont.FromGlyphIds(source);
        var extracted = reverse.Decode([(byte)(gid >> 8), (byte)gid]);

        var embedded = Type0EmbedSupport.Embed(font, source);
        var stream = Type0EmbedSupport.Stream(embedded.Reader, embedded.Top["ToUnicode"]);
        var cmap = Type0EmbedSupport.ParseToUnicode(
            Type0EmbedSupport.DecodeStream(embedded.Reader, stream));

        Assert.Equal("\uFFFD", extracted);
        Assert.Equal(extracted, Assert.Single(cmap).Value);
    }
}
