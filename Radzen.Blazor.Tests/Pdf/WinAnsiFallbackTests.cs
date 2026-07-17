#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class WinAnsiFallbackTests
{
    [Fact]
    public void NonWinAnsiCharacters_RenderViaRegisteredFallback()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        builder.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("Total: 100 лв.");

        var text = BuildTestSupport.Reload(builder).ExtractText();

        Assert.Contains("Total: 100", text, StringComparison.Ordinal);
        Assert.Contains("лв", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NonWinAnsiCharacters_NoFallback_AreNotSilentlyDeleted()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("AБB");

        var text = BuildTestSupport.Reload(builder).ExtractText().Trim();

        Assert.NotEqual("AB", text);
        Assert.True(text.Length >= 3, $"expected a visible substitute, extracted '{text}'");
    }

    [Fact]
    public void OnlyNonWinAnsiText_NoFallback_StillEmitsVisibleContent()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("Дума");

        var text = BuildTestSupport.Reload(builder).ExtractText();

        Assert.False(string.IsNullOrWhiteSpace(text), "non-cp1252 text must not vanish from the page");
    }
}
