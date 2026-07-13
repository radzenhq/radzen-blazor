#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Lane cq-reader, finding #43: an inline image's binary payload is skipped by its exact
// length (computed from the geometry or an explicit /L) so a payload that happens to
// contain the bytes "EI" no longer truncates the image early and lexes the rest of the
// stream as garbage operators.
public class InlineImageLengthTests
{
    private static byte[] Ascii(string text) => Encoding.Latin1.GetBytes(text);

    private static string[] Operators(byte[] stream) => ContentTokenizer.Tokenize(stream)
        .Where(t => t.Kind == ContentTokenizer.TokenKind.Operator)
        .Select(t => t.Text!)
        .ToArray();

    [Fact]
    public void InlineImagePayloadContainingBoundedEI_DoesNotTruncateEarly()
    {
        // W=4 H=1 gray 8bpc => exactly 4 payload bytes. The first two are 'E' 'I' followed
        // by a space, which the old whitespace-bounded heuristic would mistake for the end.
        var bytes = new List<byte>();
        bytes.AddRange(Ascii("q BI /W 4 /H 1 /CS /G /BPC 8 ID "));
        bytes.AddRange(new byte[] { (byte)'E', (byte)'I', (byte)' ', 0x00 });
        bytes.AddRange(Ascii(" EI\n10 20 m Q"));

        Assert.Equal(new[] { "q", "m", "Q" }, Operators([.. bytes]));
    }

    [Fact]
    public void ExplicitLengthPayload_IsSkippedByLength()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Ascii("BI /W 2 /H 2 /CS /RGB /BPC 8 /F /AHx /L 5 ID "));
        bytes.AddRange(new byte[] { (byte)'E', (byte)'I', (byte)' ', (byte)'E', (byte)'I' });
        bytes.AddRange(Ascii(" EI\n5 5 m"));

        Assert.Equal(new[] { "m" }, Operators([.. bytes]));
    }

    [Fact]
    public void FilteredImageWithoutLength_FallsBackToEiScan()
    {
        // No /L and a filter: the decoded length is unknowable, so the whitespace-bounded
        // EI scan still terminates the image.
        var bytes = new List<byte>();
        bytes.AddRange(Ascii("BI /W 2 /H 2 /F /Fl ID "));
        bytes.AddRange(new byte[] { 0x78, 0x9C, 0x01, 0x02 });
        bytes.AddRange(Ascii(" EI\n7 7 m"));

        Assert.Equal(new[] { "m" }, Operators([.. bytes]));
    }
}
