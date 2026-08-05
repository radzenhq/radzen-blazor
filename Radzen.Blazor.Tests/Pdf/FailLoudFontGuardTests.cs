using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FailLoudFontGuardTests
{
    private static Document WithText(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return document;
    }

    [Theory]
    [InlineData("مرحبا")]
    [InlineData("नमस्ते")]
    [InlineData("สวัสดี")]
    [InlineData("שלום")]
    [InlineData("מזלטוב")]
    [InlineData("שׁוּוֹ")]
    [InlineData("ᠠᠷᠠ")]
    [InlineData("ߊߕߜ")]
    [InlineData("ࠀࠁ")]
    [InlineData("ࡰࡱ")]
    [InlineData("A‏B")]
    [InlineData("A⁦B")]
    [InlineData("\U0001E900\U0001E921")]
    public void ComplexOrRtlScript_ThrowsInsteadOfRenderingBroken(string text)
    {
        var document = WithText(text);
        var ex = Assert.Throws<NotSupportedException>(() => new DocumentRenderer().ToArray(document));
        Assert.Contains("script", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Привет")]
    [InlineData("Γειά")]
    public void NonShapingScript_RendersWithoutThrowing(string text)
    {
        var bytes = new DocumentRenderer().ToArray(WithText(text));
        Assert.True(bytes.Length > 0);
    }

    [Theory]
    [InlineData("中文")]
    [InlineData("가나")]
    public void CjkAndHangul_RenderWithoutThrowing(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterCjk(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Cjk);
        var bytes = new DocumentRenderer { UnsupportedCharacters = UnsupportedCharacterPolicy.Substitute }.ToArray(document);
        Assert.True(bytes.Length > 0);
    }
}
