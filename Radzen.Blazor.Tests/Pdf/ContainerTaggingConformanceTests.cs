#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class ContainerTaggingConformanceTests
{
    private static (Document Document, DocumentRenderer Renderer) Author(bool ua, PdfAConformance conformance)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Boxed";
        var renderer = new DocumentRenderer();
        renderer.Accessibility = ua ? PdfUaConformance.PdfUa1 : PdfUaConformance.None;
        renderer.Conformance = conformance;
        if (ua)
        {
            document.Language = "en-US";
        }

        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(8) });
        container.Blocks.AddParagraph().Inlines.Add("BOXED").Font.Family = BuildTestSupport.Latin;
        return (document, renderer);
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
    public void PdfUA_ContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: true, PdfAConformance.None)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void LevelA_ContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: false, PdfAConformance.PdfA3A)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void PlainDocument_ContainerOutputIsByteStable()
    {
        var a = Rendered(Author(ua: false, PdfAConformance.None));
        var b = Rendered(Author(ua: false, PdfAConformance.None));
        Assert.Equal(a, b);
    }
}
