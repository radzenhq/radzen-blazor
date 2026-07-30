#nullable enable

using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentInterpreterHardeningTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static ContentCollection Materialize(string stream)
    {
        var target = new ContentCollection();
        ContentInterpreter.Materialize(Ascii(stream), target);
        return target;
    }

    private static string Emit(ContentElement element)
    {
        using var writer = new ContentWriter();
        element.Emit(writer);
        return Encoding.Latin1.GetString(writer.ToArray());
    }

    [Fact]
    public void UnterminatedArray_DoesNotThrow()
    {
        var content = Materialize("BT /F0 12 Tf [(Hello) 5");

        Assert.Empty(content);
    }

    [Fact]
    public void UnterminatedArray_FollowedByShow_KeepsText()
    {
        var content = Materialize("BT /F0 12 Tf 10 700 Td [(Hello) 5 (world)");

        Assert.Empty(content);
    }

    [Fact]
    public void PatternFill_KeepsPatternName()
    {
        var content = Materialize("/Pattern cs /P0 scn 0 0 10 10 re f");

        var path = Assert.IsType<PathContent>(Assert.Single(content));
        Assert.True(path.FillPaint.HasValue);
        var paint = path.FillPaint.Value;
        Assert.Equal("Pattern", paint.ColorSpace);
        Assert.Equal("P0", paint.PatternName);
    }

    [Fact]
    public void PatternFill_ReEmitsPatternName()
    {
        var content = Materialize("/Pattern cs /P0 scn 0 0 10 10 re f");
        ((PathContent)content[0]).EvenOdd = true;

        var body = Emit(content[0]);

        Assert.Contains("/Pattern cs", body);
        Assert.Contains("/P0 scn", body);
    }

    [Fact]
    public void PatternStroke_ReEmitsPatternName()
    {
        var content = Materialize("/Pattern CS /P0 SCN 1 w 0 0 m 10 10 l S");

        var body = Emit(content[0]);

        Assert.Contains("/Pattern CS", body);
        Assert.Contains("/P0 SCN", body);
    }

    [Fact]
    public void NestedDictionaryOperand_DoesNotLeakIntoTheOperatorFrame()
    {
        var content = Materialize("/Tag << /A << /B 1 >> /C 2 >> DP 0 0 10 10 re f");

        var raw = Assert.IsType<RawContent>(content[0]);
        Assert.Equal("DP", raw.Operator);

        var body = Emit(raw);
        Assert.Equal("/Tag DP\n", body);
        Assert.IsType<PathContent>(content[1]);
    }
}
