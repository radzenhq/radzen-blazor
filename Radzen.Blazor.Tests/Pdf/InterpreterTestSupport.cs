#nullable enable

using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Shared helpers for the C4a content-interpreter / lazy-materialization tests.
internal static class InterpreterTestSupport
{
    public static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    public static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    // Round-trips a document through save + load so its pages carry retained raw
    // content bytes and materialize lazily on first Content access.
    public static Document SaveAndLoad(Document document) => Load(document.ToArray());

    public static byte[] PageContentBytes(byte[] file, int pageIndex)
        => ContentTestHelpers.PageContent(DocumentReader.Parse(file), pageIndex);

    public static void AssertMatrix(Matrix expected, Matrix actual)
        => AssertMatrix(expected.A, expected.B, expected.C, expected.D, expected.E, expected.F, actual);

    // Matrix has no arbitrary-component constructor, so expected values that are not
    // expressible via Translate/Scale/Rotate are asserted component-wise here.
    public static void AssertMatrix(double a, double b, double c, double d, double e, double f, Matrix actual)
    {
        Assert.Equal(a, actual.A, 3);
        Assert.Equal(b, actual.B, 3);
        Assert.Equal(c, actual.C, 3);
        Assert.Equal(d, actual.D, 3);
        Assert.Equal(e, actual.E, 3);
        Assert.Equal(f, actual.F, 3);
    }
}
