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
        [InlineData("italic", "*")]
        [InlineData("strikethrough", "~~")]
        [InlineData("code", "`")]
        public void Wrap_WrapsSelection_AndSelectsInnerText(string command, string token)
        {
            var edit = MarkdownFormatter.Apply("hello world", 6, 11, command);

            var t = token.Length;
            Assert.Equal(new MarkdownEdit(6, 11, token + "world" + token, 6 + t, 11 + t), edit);
        }

        // caret must sit in actual whitespace to insert empty delimiters; a caret merely at a word's
        // edge (e.g. old "hello" at position 5) now expands to the word instead (see Bold_ExpandsCaretToWord).
        [Theory]
        [InlineData("bold", "**")]
        [InlineData("italic", "*")]
        [InlineData("strikethrough", "~~")]
        [InlineData("code", "`")]
        public void Wrap_WithEmptySelection_InsertsTokens_AndPlacesCaretBetween(string command, string token)
        {
            var edit = MarkdownFormatter.Apply("a  b", 2, 2, command);

            var t = token.Length;
            Assert.Equal(new MarkdownEdit(2, 2, token + token, 2 + t, 2 + t), edit);
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
        public void Wrap_EmptyCaretBetweenTokens_WrapsUnflankedRun()
        {
            // "****" has no whitespace around it and is unflanked, so InlineParser.ScanSpans never pairs
            // the asterisks into a token (CommonMark flanking rules require non-whitespace/punctuation
            // context to open/close). The caret at 2..2 therefore expands to the whole "****" run like any
            // other word and gets wrapped, rather than being special-cased as "between empty tokens".
            var edit = MarkdownFormatter.Apply("****", 2, 2, MarkdownEditorCommands.Bold);

            Assert.Equal(new MarkdownEdit(0, 4, "**" + "****" + "**", 2, 6), edit);
        }

        [Fact]
        public void Wrap_DoesNotUnwrapDifferentToken()
        {
            // italic applied to "**hi**" selected → wrap (with the canonical "*" delimiter), not unwrap
            var edit = MarkdownFormatter.Apply("say **hi** now", 4, 10, MarkdownEditorCommands.Italic);

            Assert.Equal(new MarkdownEdit(4, 10, "*" + "**hi**" + "*", 5, 11), edit);
        }

        // enchev repro 1: trailing space in selection
        [Fact]
        public void Bold_TrimsTrailingWhitespace()
        {
            // "hello world", selection [0,6) = "hello "
            var edit = MarkdownFormatter.Apply("hello world", 0, 6, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(0, 5, "**hello**", 2, 7), edit);
        }

        // enchev repro 2: caret inside a word expands to the word
        [Fact]
        public void Bold_ExpandsCaretToWord()
        {
            var edit = MarkdownFormatter.Apply("hello world", 2, 2, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(0, 5, "**hello**", 2, 7), edit);
        }

        [Fact]
        public void Bold_CaretInWhitespace_InsertsEmptyDelimiters()
        {
            var edit = MarkdownFormatter.Apply("a  b", 2, 2, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(2, 2, "****", 4, 4), edit);
        }

        // enchev repro 3: partial selection inside existing bold unwraps the whole token
        [Fact]
        public void Bold_PartialSelectionInsideBold_UnwrapsToken()
        {
            // "hello **world**", select "orl" [10,13)
            var edit = MarkdownFormatter.Apply("hello **world**", 10, 13, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(6, 15, "world", 6, 11), edit);
        }

        // enchev repro 4: star emphasis is recognized by the italic toggle
        [Fact]
        public void Italic_UnwrapsStarEmphasis()
        {
            var edit = MarkdownFormatter.Apply("*text*", 1, 5, MarkdownEditorCommands.Italic);
            Assert.Equal(new MarkdownEdit(0, 6, "text", 0, 4), edit);
        }

        [Fact]
        public void Italic_UnwrapsUnderscoreEmphasis()
        {
            var edit = MarkdownFormatter.Apply("_text_", 1, 5, MarkdownEditorCommands.Italic);
            Assert.Equal(new MarkdownEdit(0, 6, "text", 0, 4), edit);
        }

        [Fact]
        public void Italic_EmitsStar()
        {
            var edit = MarkdownFormatter.Apply("word", 0, 4, MarkdownEditorCommands.Italic);
            Assert.Equal(new MarkdownEdit(0, 4, "*word*", 1, 5), edit);
        }

        // overlapping: selection extends past an existing token → strip inner, wrap whole
        [Fact]
        public void Bold_SelectionContainingBoldToken_StripsAndWrapsWhole()
        {
            // "**a** b", select all [0,7)
            var edit = MarkdownFormatter.Apply("**a** b", 0, 7, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(0, 7, "**a b**", 2, 5), edit);
        }

        [Fact]
        public void Bold_DoesNotMatchTokensOnOtherLines()
        {
            // bold on line 2 must not affect detection for a selection on line 1
            var edit = MarkdownFormatter.Apply("plain\n**bold**", 0, 5, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(0, 5, "**plain**", 2, 7), edit);
        }

        [Fact]
        public void Code_UnwrapsInlineCode()
        {
            var edit = MarkdownFormatter.Apply("`x`", 1, 2, MarkdownEditorCommands.Code);
            Assert.Equal(new MarkdownEdit(0, 3, "x", 0, 1), edit);
        }

        [Fact]
        public void Bold_LeadingWhitespaceOnLine_MapsSpanOffsetsBackToDocumentCoordinates()
        {
            // ScanSpans trims its input, so the span for "**bold**" is reported relative to the trimmed
            // line ("**bold** x"), not the original line ("  **bold** x"). Selecting "ol" inside "bold"
            // (document offsets 5..7) must still resolve to the correct outer token range (2..10) by
            // compensating for the line's 2 leading spaces.
            var edit = MarkdownFormatter.Apply("  **bold** x", 5, 7, MarkdownEditorCommands.Bold);
            Assert.Equal(new MarkdownEdit(2, 10, "bold", 2, 6), edit);
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

        [Fact]
        public void HorizontalRule_WithSelection_InsertsAfterSelectionWithoutReplacing()
        {
            var edit = MarkdownFormatter.Apply("ab cd", 0, 2, MarkdownEditorCommands.HorizontalRule);

            Assert.Equal(new MarkdownEdit(2, 2, "\n---\n", 7, 7), edit);
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
