using Xunit;

namespace Radzen.Blazor.Tests
{
    public class MarkdownFormatterTests
    {
        [Fact]
        public void Apply_ReturnsNull_ForUnknownCommand()
        {
            Assert.Null(MarkdownFormatter.Apply("hello", 0, 5, "insertToday"));
        }

        [Fact]
        public void InsertText_ReplacesSelection_AndPlacesCaretAfter()
        {
            var edit = MarkdownFormatter.Apply("hello world", 6, 11, MarkdownEditorCommands.InsertText, "there");

            Assert.Equal(new MarkdownEdit(6, 11, "there", 11, 11), edit);
        }

        [Fact]
        public void InsertText_TreatsNullValueAsEmpty()
        {
            var edit = MarkdownFormatter.Apply("hello", 0, 5, MarkdownEditorCommands.InsertText, null);

            Assert.Equal(new MarkdownEdit(0, 5, "", 0, 0), edit);
        }

        [Fact]
        public void Apply_ClampsOutOfRangeSelection()
        {
            var edit = MarkdownFormatter.Apply("abc", -2, 99, MarkdownEditorCommands.InsertText, "x");

            Assert.Equal(new MarkdownEdit(0, 3, "x", 1, 1), edit);
        }

        [Theory]
        [InlineData("bold", "**")]
        [InlineData("italic", "_")]
        [InlineData("strikethrough", "~~")]
        [InlineData("code", "`")]
        public void Wrap_WrapsSelection_AndSelectsInnerText(string command, string token)
        {
            var edit = MarkdownFormatter.Apply("hello world", 6, 11, command);

            var t = token.Length;
            Assert.Equal(new MarkdownEdit(6, 11, token + "world" + token, 6 + t, 11 + t), edit);
        }

        [Theory]
        [InlineData("bold", "**")]
        [InlineData("italic", "_")]
        [InlineData("strikethrough", "~~")]
        [InlineData("code", "`")]
        public void Wrap_WithEmptySelection_InsertsTokens_AndPlacesCaretBetween(string command, string token)
        {
            var edit = MarkdownFormatter.Apply("hello", 5, 5, command);

            var t = token.Length;
            Assert.Equal(new MarkdownEdit(5, 5, token + token, 5 + t, 5 + t), edit);
        }

        [Fact]
        public void Wrap_UnwrapsWhenSelectionIncludesTokens()
        {
            // "say **hi** now" — select "**hi**" (4..10)
            var edit = MarkdownFormatter.Apply("say **hi** now", 4, 10, MarkdownEditorCommands.Bold);

            Assert.Equal(new MarkdownEdit(4, 10, "hi", 4, 6), edit);
        }

        [Fact]
        public void Wrap_UnwrapsWhenTokensSurroundSelection()
        {
            // "say **hi** now" — select "hi" (6..8)
            var edit = MarkdownFormatter.Apply("say **hi** now", 6, 8, MarkdownEditorCommands.Bold);

            Assert.Equal(new MarkdownEdit(4, 10, "hi", 4, 6), edit);
        }

        [Fact]
        public void Wrap_DoesNotUnwrapDifferentToken()
        {
            // italic applied to "**hi**" selected → wrap, not unwrap
            var edit = MarkdownFormatter.Apply("say **hi** now", 4, 10, MarkdownEditorCommands.Italic);

            Assert.Equal(new MarkdownEdit(4, 10, "_**hi**_", 5, 11), edit);
        }
    }
}
