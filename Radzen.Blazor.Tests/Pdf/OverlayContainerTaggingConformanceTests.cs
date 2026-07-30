#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class OverlayContainerTaggingConformanceTests
{
    private static (Document Document, DocumentRenderer Renderer) Author(bool ua, PdfAConformance conformance, ContainerLayout layout, double rotation)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Boxed";
        var builderRenderer = new DocumentRenderer();
        builderRenderer.Accessibility = ua ? PdfUaConformance.PdfUa1 : PdfUaConformance.None;
        builderRenderer.Conformance = conformance;
        if (ua)
        {
            document.Language = "en-US";
        }

        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(8), Layout = layout, Rotation = rotation });
        container.Blocks.AddParagraph().Inlines.Add("OVERLAID").Font.Family = BuildTestSupport.Latin;
        return (document, builderRenderer);
    }

    private static List<ContentOperation> Ops((Document Document, DocumentRenderer Renderer) authored)
        => ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(
            BuildTestSupport.Read(authored.Document, authored.Renderer), 0));

    private static byte[] Rendered((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static HashSet<string> TagsWrappingText(List<ContentOperation> ops)
    {
        var stack = new List<string>();
        var tags = new HashSet<string>();
        foreach (var operation in ops)
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    stack.Add(operation.Operands.Count > 0 ? operation.Operands[0].Text : "");
                    break;
                case "EMC":
                    if (stack.Count > 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }

                    break;
                case "Tj" or "TJ" or "'" or "\"":
                    foreach (var tag in stack)
                    {
                        tags.Add(tag);
                    }

                    break;
            }
        }

        return tags;
    }

    [Fact]
    public void PdfUA_OverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: true, PdfAConformance.None, ContainerLayout.Overlay, 0)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void LevelA_OverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: false, PdfAConformance.PdfA3A, ContainerLayout.Overlay, 0)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void PdfUA_RotatedOverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: true, PdfAConformance.None, ContainerLayout.Overlay, 30)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void PlainDocument_OverlayContainerOutputIsByteStable()
    {
        var a = Rendered(Author(ua: false, PdfAConformance.None, ContainerLayout.Overlay, 0));
        var b = Rendered(Author(ua: false, PdfAConformance.None, ContainerLayout.Overlay, 0));
        Assert.Equal(a, b);
    }
}
