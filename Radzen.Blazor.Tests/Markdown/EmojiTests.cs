using Xunit;

namespace Radzen.Documents.Markdown.Tests;

public class EmojiTests
{
    private static string ToXml(string markdown)
    {
        var document = MarkdownParser.Parse(markdown);
        return XmlVisitor.ToXml(document);
    }

    [Theory]
    [InlineData(":smile:", "😄")]
    [InlineData(":tent:", "⛺")]
    [InlineData(":+1:", "👍")]
    [InlineData(":wave:", "👋")]
    [InlineData(":heart:", "❤️")]
    [InlineData(":SMILE:", "😄")]
    public void Emoji_Shortcode_Is_Replaced(string markdown, string expected)
    {
        var xml = ToXml(markdown);
        Assert.Contains($"<text>{expected}</text>", xml);
    }

    [Fact]
    public void Emoji_Shortcode_In_Text()
    {
        var xml = ToXml("Hello :wave: world");
        Assert.Contains("<text>👋</text>", xml);
        Assert.Contains("<text>Hello </text>", xml);
    }

    [Fact]
    public void Emoji_In_Emphasis()
    {
        var xml = ToXml("*:heart:*");
        Assert.Contains("<emph>", xml);
        Assert.Contains("<text>❤️</text>", xml);
    }

    [Fact]
    public void Multiple_Emojis()
    {
        var xml = ToXml(":smile: :heart:");
        Assert.Contains("<text>😄</text>", xml);
        Assert.Contains("<text>❤️</text>", xml);
    }

    [Theory]
    [InlineData(":notanemoji:")]
    [InlineData(": smile:")]
    [InlineData(":smile :")]
    public void Invalid_Emoji_Shortcode_Is_Not_Replaced(string markdown)
    {
        var xml = ToXml(markdown);
        Assert.DoesNotContain("😄", xml);
    }
}
