#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TextEmissionTests
{
    [Fact]
    public void Kerning_On_EmitsTjAdjustments()
    {
        var builder = new DocumentBuilder { Fonts = { EnableKerning = true } };
        var section = builder.Sections.Add();
        section.Blocks.Add(FeatureEmissionTestHelpers.Text("AWAY To Yo"));

        var content = FeatureEmissionTestHelpers.Content(builder);
        Assert.Contains("] TJ", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Kerning_Off_UsesTj_AndBuildIsRepeatable()
    {
        static byte[] Build()
        {
            var builder = new DocumentBuilder();
            var section = builder.Sections.Add();
            section.Blocks.Add(FeatureEmissionTestHelpers.Text("AWAY To Yo"));
            return builder.ToArray();
        }

        var first = Build();
        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(DocumentReader.Parse(first), 0));
        Assert.Contains(" Tj", content, StringComparison.Ordinal);
        Assert.DoesNotContain("] TJ", content, StringComparison.Ordinal);
        Assert.Equal(first, Build());
    }

    [Fact]
    public void InvisibleRun_EmitsRenderModeThree()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Hidden");
        run.Invisible = true;
        section.Blocks.Add(paragraph);

        Assert.Contains("3 Tr", FeatureEmissionTestHelpers.Content(builder), StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleRun_DoesNotEmitRenderModeThree()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.Add(FeatureEmissionTestHelpers.Text("Shown"));

        Assert.DoesNotContain("3 Tr", FeatureEmissionTestHelpers.Content(builder), StringComparison.Ordinal);
    }

    [Fact]
    public void DeviceCmykRun_EmitsKOperator()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Cyan");
        run.SetFillCmyk(1, 0, 0, 0);
        section.Blocks.Add(paragraph);

        var content = FeatureEmissionTestHelpers.Content(builder);
        Assert.Contains("1 0 0 0 k", content, StringComparison.Ordinal);
    }
}
