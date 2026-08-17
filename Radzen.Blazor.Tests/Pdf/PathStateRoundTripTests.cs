#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PathStateRoundTripTests
{
    private const string Source =
        "q\n" +
        "[3 2] 0 d\n" +
        "0.1 0.2 0.3 0.4 k\n" +
        "10 10 100 50 re\n" +
        "W f*\n" +
        "Q\n" +
        "/Cs1 cs\n" +
        "0.2 0.4 0.6 scn\n" +
        "200 200 50 50 re\n" +
        "f\n";

    private static string ReencodeAfterMutation()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(Source));

        var loaded = InterpreterTestSupport.Load(document.ToArray());
        loaded.Pages[0].Content[1].Transform = Matrix.Translate(5, 5);

        var emission = Emit(loaded);
        return IndirectObject(
            emission,
            Shaped("page", @"/Contents (\d+) 0 R", Line(emission, "/Type /Page ")).Groups[1].Value);
    }

    [Fact]
    public void MutatedPage_PreservesDashClipEvenOddCmykAndNamedColorspace()
    {
        var content = ReencodeAfterMutation();

        Carries("page content", " d\n", content);
        Carries("page content", "\nW f*\n", content);
        Carries("page content", " k\n", content);
        Carries("page content", " cs\n", content);
        Carries("page content", " scn\n", content);
    }

    [Fact]
    public void MutatedPage_PreservesDashArrayOperandsAndCmykChannels()
    {
        var content = ReencodeAfterMutation();

        Carries("page content", "\n[3 2] 0 d\n", content);
        Shaped("page content cmyk fill", @"\n0\.1 \S+ \S+ 0\.4 k\n", content);
    }
}
