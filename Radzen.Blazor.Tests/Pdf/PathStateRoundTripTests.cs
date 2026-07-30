#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

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

    private static byte[] ReencodeAfterMutation()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii(Source));

        var loaded = InterpreterTestSupport.Load(document.ToArray());
        loaded.Pages[0].Content[0].Transform = Matrix.Translate(5, 5);

        return InterpreterTestSupport.PageContentBytes(loaded.ToArray(), 0);
    }

    [Fact]
    public void MutatedPage_PreservesDashClipEvenOddCmykAndNamedColorspace()
    {
        var operators = new HashSet<string>(ContentStreamTokenizer.Operators(ReencodeAfterMutation()));

        Assert.Contains("d", operators);
        Assert.Contains("W", operators);
        Assert.Contains("f*", operators);
        Assert.Contains("k", operators);
        Assert.Contains("cs", operators);
        Assert.Contains("scn", operators);
    }

    [Fact]
    public void MutatedPage_PreservesDashArrayOperandsAndCmykChannels()
    {
        var content = ReencodeAfterMutation();

        var dash = FindOperation(content, "d");
        Assert.Equal(3, dash.Num(1), 3);
        Assert.Equal(2, dash.Num(2), 3);
        Assert.Equal(0, dash.Num(4), 3);

        var cmyk = FindOperation(content, "k");
        Assert.Equal(4, cmyk.Operands.Count);
        Assert.Equal(0.1, cmyk.Num(0), 3);
        Assert.Equal(0.4, cmyk.Num(3), 3);
    }

    private static ContentOperation FindOperation(byte[] content, string op)
    {
        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == op)
            {
                return operation;
            }
        }

        throw new Xunit.Sdk.XunitException($"operator '{op}' not found");
    }
}
