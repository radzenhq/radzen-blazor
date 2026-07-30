#nullable enable

using System.Linq;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 9.4.1: text objects shall not be nested
public class ContentEditorNestedTextObjectTests
{
    [Fact]
    public void ModifiedTextInsideTextObject_DoesNotNestTextObjects()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Ascii("BT /F1 12 Tf 10 10 Td (Hi) Tj ET"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        loaded.Pages[0].Content.OfType<TextContent>().Single().Color = Color.Red;

        var content = Encoding.ASCII.GetString(InterpreterTestSupport.PageContentBytes(loaded.ToArray(), 0));

        Assert.Equal(1, Count(content, "BT"));
        Assert.Equal(1, Count(content, "ET"));
    }

    [Fact]
    public void ModifiedTextInsideTextObject_KeepsFollowingRunsPosition()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Ascii("BT /F1 12 Tf 10 10 Td (Hi) Tj 5 0 Td (There) Tj ET"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        var runs = loaded.Pages[0].Content.OfType<TextContent>().ToList();
        var expected = runs[1].Transform;
        runs[0].Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().ToList();

        Assert.Equal(2, actual.Count);
        Assert.Equal("There", actual[1].Text);
        InterpreterTestSupport.AssertMatrix(expected, actual[1].Transform);
    }

    [Fact]
    public void ModifiedTextInsideTextObject_DoesNotLeakStateOntoFollowingRuns()
    {
        var document = new Document();
        document.Pages.Add().SetContent(Ascii("BT /F1 12 Tf 10 10 Td (Hi) Tj 5 0 Td (There) Tj ET"));

        var loaded = InterpreterTestSupport.SaveAndLoad(document);
        loaded.Pages[0].Content.OfType<TextContent>().First().Color = Color.Red;

        var reloaded = InterpreterTestSupport.SaveAndLoad(loaded);
        var actual = reloaded.Pages[0].Content.OfType<TextContent>().ToList();

        Assert.Equal(Color.Red, actual[0].Color);
        Assert.Equal(Color.Black, actual[1].Color);
    }

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

    private static int Count(string content, string op)
    {
        var count = 0;
        foreach (var token in content.Split([' ', '\n', '\r'], System.StringSplitOptions.RemoveEmptyEntries))
        {
            if (token == op)
            {
                count++;
            }
        }

        return count;
    }
}
