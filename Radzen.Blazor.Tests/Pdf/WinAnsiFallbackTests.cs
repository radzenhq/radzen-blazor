#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

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
        section.Blocks.AddParagraph("Total: 100 лв.");

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.Contains("Total: 100", text, StringComparison.Ordinal);
        Assert.Contains("лв", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NonWinAnsiCharacters_NoFallback_AreNotSilentlyDeleted()
    {
        var document = new Document();
        document.Fonts.AllowUnsupportedCharacters = true;
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("AﬁB");

        var text = BuildTestSupport.Reload(document).ExtractText().Trim();

        Assert.NotEqual("AB", text);
        Assert.True(text.Length >= 3, $"expected a visible substitute, extracted '{text}'");
    }

    [Fact]
    public void OnlyNonWinAnsiText_NoFallback_StillEmitsVisibleContent()
    {
        var document = new Document();
        document.Fonts.AllowUnsupportedCharacters = true;
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("ﬁﬁﬁﬁ");

        var text = BuildTestSupport.Reload(document).ExtractText();

        Assert.False(string.IsNullOrWhiteSpace(text), "non-cp1252 text must not vanish from the page");
    }
}
