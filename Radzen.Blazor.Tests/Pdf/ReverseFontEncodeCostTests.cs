#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ReverseFontEncodeCostTests
{
    [Fact]
    public void MultiCharMappings_StillEncodeAsSingleCode()
    {
        var font = ReverseFont.FromGlyphIds(new Dictionary<ushort, int>
        {
            [1] = 'f',
            [2] = 0x10400,
        });

        Assert.True(font.TryEncode("f\U00010400", out var codes));
        Assert.Equal([0, 1, 0, 2], codes);
        Assert.False(font.TryEncode("fx", out _));
    }
}
