using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Fail-loud policy: unsupported font/script features throw rather than silently
// emitting broken output. Scripts that need no shaping stay unaffected.
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
    [InlineData("مرحبا")] // Arabic "marhaba"
    [InlineData("नमस्ते")] // Devanagari "namaste"
    [InlineData("สวัสดี")] // Thai "sawasdee"
    [InlineData("שלום")] // Hebrew "shalom" - RTL, was silently reversed before the guard
    [InlineData("מזלטוב")] // Hebrew "mazel tov"
    [InlineData("שׁוּוֹ")] // Hebrew presentation forms
    [InlineData("ᠠᠷᠠ")] // Mongolian
    [InlineData("ߊߕߜ")] // N'Ko
    public void ComplexOrRtlScript_ThrowsInsteadOfRenderingBroken(string text)
    {
        var builder = WithText(text);
        var ex = Assert.Throws<NotSupportedException>(() => builder.ToArray());
        Assert.Contains("script", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Hello world")]      // Latin
    [InlineData("Привет")] // Cyrillic "Privet"
    [InlineData("Γειά")]  // Greek
    public void NonShapingScript_RendersWithoutThrowing(string text)
    {
        var bytes = WithText(text).ToArray();
        Assert.True(bytes.Length > 0);
    }

    [Theory]
    [InlineData("中文")]  // CJK Han
    [InlineData("가나")] // Hangul
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
