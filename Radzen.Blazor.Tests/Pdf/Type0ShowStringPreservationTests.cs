#nullable enable

using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class Type0ShowStringPreservationTests
{
    private static readonly byte[] CidBytes = [0x00, 0x41, 0x00, 0x42, 0x00, 0x43];

    private const string Type0Stream =
        "BT /F0 12 Tf 10 700 Td <004100420043> Tj ET\n" +
        "1 0 0 RG 2 w 10 10 m 200 10 l S\n";

    private static byte[] TjString(byte[] content)
        => ContentStreamTokenizer.Parse(content)
            .First(op => op.Operator == "Tj")
            .Operands.Last(o => o.Kind == ContentTokenKind.String)
            .Bytes;

    [Fact]
    public void MutatingUnrelatedElement_PreservesType0ShowStringBytes()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(Type0Stream));

        var loaded = InterpreterTestSupport.Load(document.ToArray());
        var content = loaded.Pages[0].Content;

        var path = content.OfType<PathContent>().First();
        path.Thickness = 5;

        var resaved = loaded.ToArray();

        Assert.Equal(CidBytes, TjString(InterpreterTestSupport.PageContentBytes(resaved, 0)));
    }

    [Fact]
    public void GeneratedText_StillEmitsWinAnsiEncodedBytes()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Hi", 72, 700));

        var content = InterpreterTestSupport.PageContentBytes(document.ToArray(), 0);

        WinAnsiEncoding.TryGetCode('H', out var h);
        WinAnsiEncoding.TryGetCode('i', out var i);
        Assert.Equal(new[] { h, i }, TjString(content));
    }
}
