using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FailLoudFontGuardTests
{
    private static DocumentBuilder WithText(string text)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return builder;
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
        var builder = WithText(text);
        var ex = Assert.Throws<NotSupportedException>(() => builder.ToArray());
        Assert.Contains("script", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Hello world")]
    [InlineData("Привет")]
    [InlineData("Γειά")]
    public void NonShapingScript_RendersWithoutThrowing(string text)
    {
        var bytes = WithText(text).ToArray();
        Assert.True(bytes.Length > 0);
    }

    [Theory]
    [InlineData("中文")]
    [InlineData("가나")]
    public void CjkAndHangul_RenderWithoutThrowing(string text)
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterCjk(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Cjk);
        var bytes = builder.ToArray();
        Assert.True(bytes.Length > 0);
    }
}
