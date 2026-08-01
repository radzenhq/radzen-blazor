#nullable enable

using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentInterpreterSpanTests
{
    private static void AssertSpanPerElement(string stream)
    {
        var content = TestBytes.Ascii(stream);
        var elements = new ContentCollection();
        var spans = ContentInterpreter.Materialize(content, elements);
        Assert.Equal(elements.Count, spans.Count);
    }

    [Fact]
    public void ClipDiscardPath_HasSpanPerElement() => AssertSpanPerElement("0 0 100 100 re W n");

    [Fact]
    public void NonClipDiscardPath_HasSpanPerElement() => AssertSpanPerElement("0 0 100 100 re n");

    [Fact]
    public void BareNoPaint_HasSpanPerElement() => AssertSpanPerElement("n");

    [Fact]
    public void ShowWithoutStringOperand_HasSpanPerElement() => AssertSpanPerElement("BT /F0 12 Tf Tj ET");

    [Fact]
    public void NamedDo_ProducesOneXObjectElementWithSpan()
    {
        var content = TestBytes.Ascii("/Im0 Do");
        var elements = new ContentCollection();
        var spans = ContentInterpreter.Materialize(content, elements);

        Assert.Single(elements.OfType<XObjectContent>());
        Assert.Equal(elements.Count, spans.Count);
    }

    [Fact]
    public void NamelessDo_ProducesNoElementAndKeepsSpansAligned()
    {
        var content = TestBytes.Ascii("q Do Q");
        var elements = new ContentCollection();
        var spans = ContentInterpreter.Materialize(content, elements);

        Assert.DoesNotContain(elements, e => e is XObjectContent);
        Assert.Equal(elements.Count, spans.Count);
    }

    [Fact]
    public void NamelessDo_InEditedPage_IsPreservedNotRejected()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(TestBytes.Ascii("q Do Q"));
        page.Content.Add(new TextContent("x", 10, 10));

        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = PortableDocument.LoadFromStream(buffer);

        Assert.Contains("Do", ContentOperationTestHelpers.Operators(reloaded.Pages[0].GetContent()!));
    }
}
