#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PathGraphicsByteSafetyTests
{
    private static PortableDocument BuildDocument()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        var path = new PathContent { Stroke = true, Fill = true, Thickness = 2, StrokeColor = Color.Blue, FillColor = Color.Red };
        path.MoveTo(10, 10);
        path.LineTo(100, 10);
        path.LineTo(100, 80);
        path.Close();
        page.Content.Add(path);
        return document;
    }

    [Fact]
    public void DefaultPath_IsDeterministicByteForByte()
    {
        Assert.Equal(BuildDocument().ToArray(), BuildDocument().ToArray());
    }

    [Fact]
    public void DefaultPath_EmitsNoNewGraphicsOperators()
    {
        var emission = Emit(BuildDocument());
        var content = IndirectObject(
            emission,
            Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page ")).Groups[1].Value);

        Carries("page content", " rg\n", content);
        Carries("page content", " RG\n", content);
        Carries("page content", "\nB\n", content);

        string[] forbidden =
        [
            " k\n", " K\n", " g\n", " G\n", " J\n", " j\n", " M\n", " ri\n", " d\n",
            "\nW\n", "\nW*\n", "\nf*\n", "\nB*\n", " scn\n", " cs\n",
        ];

        foreach (var fragment in forbidden)
        {
            Lacks("page content", fragment, content);
        }
    }
}
