#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The interpreter (which materializes elements) and the editor (which maps byte ranges to
// those elements) must agree on which operators produce an element. When they disagree the
// public Page.Content getter throws from ContentEditor.Map.
public class ContentEditorPaintOperatorAgreementTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static void RoundTrip(string stream)
    {
        var content = Ascii(stream);
        var elements = new ContentCollection();
        ContentInterpreter.Materialize(content, elements);
        ContentEditor.Map(content, elements);
    }

    [Fact]
    public void NoPaintPathDiscard_WithoutClip_MapsSafely()
    {
        RoundTrip("0 0 100 100 re n");
    }

    [Fact]
    public void NoPaintPathDiscard_WithClip_MapsSafely()
    {
        RoundTrip("0 0 100 100 re W n");
    }

    [Fact]
    public void BareNoPaint_WithoutPath_MapsSafely()
    {
        RoundTrip("n");
    }

    [Fact]
    public void TjWithoutStringOperand_MapsSafely()
    {
        RoundTrip("BT /F0 12 Tf Tj ET");
    }
}
