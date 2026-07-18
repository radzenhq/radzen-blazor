#nullable enable
using System.Text;
using Xunit;
using Radzen.Documents.Pdf;

namespace Radzen.Blazor.Pdf.Tests;

public class Base14FallbackKerningTests
{
    private static string Content(bool enableKerning)
    {
        var builder = new DocumentBuilder();
        builder.Fonts.EnableKerning = enableKerning;
        BuildTestSupport.RegisterLatin(builder);
        builder.Fonts.SetFallback(BuildTestSupport.Latin);

        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("АТ");

        var reader = BuildTestSupport.Read(builder);
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        return Encoding.Latin1.GetString(BuildTestSupport.Content(reader, page));
    }

    [Fact]
    public void Fallback_Run_Applies_Kerning_When_Enabled()
    {
        Assert.Contains("TJ", Content(enableKerning: true));
    }

    [Fact]
    public void Fallback_Run_Has_No_Kerning_When_Disabled()
    {
        Assert.DoesNotContain("TJ", Content(enableKerning: false));
    }
}
