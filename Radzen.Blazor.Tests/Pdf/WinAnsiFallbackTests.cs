#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class WinAnsiFallbackTests
{
    [Fact]
    public void NonWinAnsiCharacters_RenderViaRegisteredFallback()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = document.Sections.Add();
        section.Blocks.Add(new Paragraph("Total: 100 лв."));

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.Contains("Total: 100", text, StringComparison.Ordinal);
        Assert.Contains("лв", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NonWinAnsiCharacters_NoFallback_AreNotSilentlyDeleted()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Paragraph("AﬁB"));

        var renderer = new DocumentRenderer { UnsupportedCharacters = UnsupportedCharacterPolicy.Substitute };
        var text = BuildTestSupport.Reload(document, renderer).ExtractText().Trim();

        Assert.NotEqual("AB", text);
        Assert.True(text.Length >= 3, $"expected a visible substitute, extracted '{text}'");
    }

    [Fact]
    public void OnlyNonWinAnsiText_NoFallback_StillEmitsVisibleContent()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Paragraph("ﬁﬁﬁﬁ"));

        var renderer = new DocumentRenderer { UnsupportedCharacters = UnsupportedCharacterPolicy.Substitute };
        var text = BuildTestSupport.Reload(document, renderer).ExtractText();

        Assert.False(string.IsNullOrWhiteSpace(text), "non-cp1252 text must not vanish from the page");
    }
}
