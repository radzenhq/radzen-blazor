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

        [Theory]
        [InlineData("quote", "> ")]
        [InlineData("unorderedList", "- ")]
        [InlineData("taskList", "- [ ] ")]
        public void PrefixLines_PrefixesEveryLineInSelection(string command, string prefix)
        {
            // caret in the middle of line 2 of three lines; only that line is affected
            var text = "one\ntwo\nthree";
            var edit = MarkdownFormatter.Apply(text, 5, 5, command);

            var replacement = prefix + "two";
            Assert.Equal(new MarkdownEdit(4, 7, replacement, 4, 4 + replacement.Length), edit);
        }

        [Fact]
        public void PrefixLines_ExpandsPartialMultiLineSelection()
        {
            // select from inside "one" to inside "two"
            var edit = MarkdownFormatter.Apply("one\ntwo\nthree", 1, 5, MarkdownEditorCommands.Quote);

            Assert.Equal(new MarkdownEdit(0, 7, "> one\n> two", 0, 11), edit);
        }

        [Fact]
        public void PrefixLines_StripsPrefixWhenAllLinesHaveIt()
        {
            var edit = MarkdownFormatter.Apply("- a\n- b", 0, 7, MarkdownEditorCommands.UnorderedList);

            Assert.Equal(new MarkdownEdit(0, 7, "a\nb", 0, 3), edit);
        }

        [Fact]
        public void PrefixLines_IgnoresTrailingNewlineInSelection()
        {
            // selecting "one\n" (0..4) must not drag line 2 in
            var edit = MarkdownFormatter.Apply("one\ntwo", 0, 4, MarkdownEditorCommands.Quote);

            Assert.Equal(new MarkdownEdit(0, 3, "> one", 0, 5), edit);
        }

        [Fact]
        public void OrderedList_NumbersLines()
        {
            var edit = MarkdownFormatter.Apply("a\nb\nc", 0, 5, MarkdownEditorCommands.OrderedList);

            Assert.Equal(new MarkdownEdit(0, 5, "1. a\n2. b\n3. c", 0, 14), edit);
        }

        [Fact]
        public void OrderedList_StripsNumbersWhenAllLinesAreNumbered()
        {
            var edit = MarkdownFormatter.Apply("1. a\n12. b", 0, 10, MarkdownEditorCommands.OrderedList);

            Assert.Equal(new MarkdownEdit(0, 10, "a\nb", 0, 3), edit);
        }

        [Theory]
        [InlineData("title", "# title")]
        [InlineData("# title", "## title")]
        [InlineData("## title", "### title")]
        [InlineData("### title", "title")]
        [InlineData("###### title", "title")]
        public void Heading_CyclesLevel(string line, string expected)
        {
            var edit = MarkdownFormatter.Apply(line, 0, 0, MarkdownEditorCommands.Heading);

            Assert.Equal(new MarkdownEdit(0, line.Length, expected, 0, expected.Length), edit);
        }

        [Fact]
        public void Heading_AppliesFirstLineLevelToAllSelectedLines()
        {
            var edit = MarkdownFormatter.Apply("# a\nb", 0, 5, MarkdownEditorCommands.Heading);

            Assert.Equal(new MarkdownEdit(0, 5, "## a\n## b", 0, 9), edit);
        }

        [Fact]
        public void PrefixLines_DoesNotDoublePrefix_PartiallyPrefixedSelection()
        {
            var edit = MarkdownFormatter.Apply("> a\nb", 0, 5, MarkdownEditorCommands.Quote);

            Assert.Equal(new MarkdownEdit(0, 5, "> a\n> b", 0, 7), edit);
        }

        [Fact]
        public void OrderedList_RenumbersPartiallyNumberedSelection()
        {
            var edit = MarkdownFormatter.Apply("1. a\nb\n7. c", 0, 11, MarkdownEditorCommands.OrderedList);

            Assert.Equal(new MarkdownEdit(0, 11, "1. a\n2. b\n3. c", 0, 14), edit);
        }

        [Fact]
        public void CodeBlock_FencesSelection_OnOwnLines()
        {
            // "ab" selected in the middle of a line
            var edit = MarkdownFormatter.Apply("x ab y", 2, 4, MarkdownEditorCommands.CodeBlock);

            Assert.Equal(new MarkdownEdit(2, 4, "\n```\nab\n```\n", 7, 9), edit);
        }

        [Fact]
        public void CodeBlock_AtLineBoundaries_AddsNoExtraNewlines()
        {
            var edit = MarkdownFormatter.Apply("code\n", 0, 5, MarkdownEditorCommands.CodeBlock);

            Assert.Equal(new MarkdownEdit(0, 5, "```\ncode\n```", 4, 9), edit);
        }

        [Fact]
        public void CodeBlock_WithEmptySelection_PlacesCaretInsideFence()
        {
            var edit = MarkdownFormatter.Apply("", 0, 0, MarkdownEditorCommands.CodeBlock);

            Assert.Equal(new MarkdownEdit(0, 0, "```\n\n```", 4, 4), edit);
        }

        [Fact]
        public void HorizontalRule_AtLineStart_InsertsRuleAndNewline()
        {
            var edit = MarkdownFormatter.Apply("a\nb", 2, 2, MarkdownEditorCommands.HorizontalRule);

            Assert.Equal(new MarkdownEdit(2, 2, "---\n", 6, 6), edit);
        }

        [Fact]
        public void HorizontalRule_MidLine_AddsLeadingNewline()
        {
            var edit = MarkdownFormatter.Apply("ab", 1, 1, MarkdownEditorCommands.HorizontalRule);

            Assert.Equal(new MarkdownEdit(1, 1, "\n---\n", 6, 6), edit);
        }

        [Fact]
        public void HorizontalRule_BeforeExistingNewline_AddsNoTrailingNewline()
        {
            var edit = MarkdownFormatter.Apply("a\nb", 1, 1, MarkdownEditorCommands.HorizontalRule);

            Assert.Equal(new MarkdownEdit(1, 1, "\n---", 5, 5), edit);
        }

        [Theory]
        [InlineData("link", "[")]
        [InlineData("image", "![")]
        public void Link_WrapsSelection_AndPlacesCaretAfter(string command, string open)
        {
            var edit = MarkdownFormatter.Apply("see docs now", 4, 8, command, "https://x.y");

            var replacement = open + "docs](https://x.y)";
            Assert.Equal(new MarkdownEdit(4, 8, replacement, 4 + replacement.Length, 4 + replacement.Length), edit);
        }

        [Fact]
        public void Link_WithEmptySelection_UsesLabel()
        {
            var edit = MarkdownFormatter.Apply("", 0, 0, MarkdownEditorCommands.Link, "https://x.y", "Docs");

            Assert.Equal(new MarkdownEdit(0, 0, "[Docs](https://x.y)", 19, 19), edit);
        }

        [Fact]
        public void Link_WithEmptySelectionAndNoLabel_PlacesCaretInsideBrackets()
        {
            var edit = MarkdownFormatter.Apply("", 0, 0, MarkdownEditorCommands.Image, "https://x.y/i.png");

            Assert.Equal(new MarkdownEdit(0, 0, "![](https://x.y/i.png)", 2, 2), edit);
        }
    }
}
