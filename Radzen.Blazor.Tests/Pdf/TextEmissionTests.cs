#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TextEmissionTests
{
    [Fact]
    public void Kerning_On_EmitsTjAdjustments()
    {
        var document = new Document { Fonts = { EnableKerning = true } };
        var section = document.Sections.Add();
        section.Blocks.Add(FeatureEmissionTestHelpers.Text("AWAY To Yo"));

        var operators = ContentOperationTestHelpers.Operators(FeatureEmissionTestHelpers.ContentBytes(document));
        Assert.Contains("TJ", operators);
    }

    [Fact]
    public void Kerning_Off_UsesTj_AndBuildIsRepeatable()
    {
        static byte[] Build()
        {
            var document = new Document();
            var section = document.Sections.Add();
            section.Blocks.Add(FeatureEmissionTestHelpers.Text("AWAY To Yo"));
            return new DocumentRenderer().ToArray(document);
        }

        var first = Build();
        var operators = ContentOperationTestHelpers.Operators(
            ContentTestHelpers.PageContent(DocumentReader.Parse(first), 0));
        Assert.Contains("Tj", operators);
        Assert.DoesNotContain("TJ", operators);
        Assert.Equal(first, Build());
    }

    [Fact]
    public void InvisibleRun_EmitsRenderModeThree()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Hidden");
        run.Invisible = true;
        section.Blocks.Add(paragraph);

        Assert.Contains("3 Tr", FeatureEmissionTestHelpers.Content(document), StringComparison.Ordinal);
    }

    [Fact]
    public void VisibleRun_DoesNotEmitRenderModeThree()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(FeatureEmissionTestHelpers.Text("Shown"));

        Assert.DoesNotContain("3 Tr", FeatureEmissionTestHelpers.Content(document), StringComparison.Ordinal);
    }
}
