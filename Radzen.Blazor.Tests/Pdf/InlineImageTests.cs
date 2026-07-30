#nullable enable

using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class InlineImageTests
{
    private static readonly byte[] Payload = [0x00, 0x01, 0x45, 0x49, 0xFF, 0x0A, 0xDE, 0xAD, 0x28, 0x42];

    private static byte[] StreamWithInlineImage()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes(
            "q 1 0 0 1 0 0 cm\n" +
            "BI /W 2 /H 2 /CS /RGB /BPC 8 ID "));
        bytes.AddRange(Payload);
        bytes.AddRange(Encoding.ASCII.GetBytes(
            " EI\n" +
            "Q\n" +
            "BT /F0 12 Tf 10 700 Td (After) Tj ET\n"));
        return [.. bytes];
    }

    private static Document Load(byte[] content)
    {
        var document = new Document();
        document.Pages.Add().SetContent(content);
        return InterpreterTestSupport.Load(document.ToArray());
    }

    [Fact]
    public void Interpreter_MaterializesPayloadAsOneElementAndTrailingText()
    {
        var content = Load(StreamWithInlineImage()).Pages[0].Content;

        Assert.IsType<InlineImageContent>(content[0]);
        Assert.Equal("After", Assert.IsType<TextContent>(content[1]).Text);
        Assert.Equal(2, content.Count);
    }

    [Fact]
    public void Extractor_SkipsPayloadAndExtractsTrailingText()
    {
        var extracted = Load(StreamWithInlineImage()).Pages[0].ExtractText();

        Assert.Equal("After", extracted);
    }
}
