#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class InlineImageEiBoundaryTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    [Fact]
    public void Tokenize_EiWithoutPrecedingWhitespace_ResumesAtFollowingOperator()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Ascii("q BI /W 1 /H 1 /CS /G /BPC 8 ID "));
        bytes.AddRange([0x2A, (byte)'E', (byte)'I']);
        bytes.AddRange(Ascii("\nQ\n"));

        var operators = ContentTokenizer.Tokenize([.. bytes])
            .Where(t => t.Kind == ContentTokenizer.TokenKind.Operator)
            .Select(t => t.Text)
            .ToArray();

        Assert.Equal(new[] { "q", "Q" }, operators);
    }
}
