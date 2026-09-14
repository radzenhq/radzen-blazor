using System.Linq;
using Radzen.Blazor;
using Xunit;

namespace Radzen.Blazor.Tests;

public class MarkdownEditorEngineTests
{
    [Fact]
    public void TypingCoalescesIntoWordsAndUndoRestoresThem()
    {
        var engine = new MarkdownEditorEngine("");

        var previous = ' ';

        foreach (var ch in "one two")
        {
            engine.InsertText(engine.Text.Length, engine.Text.Length, ch.ToString(), literal: true, key: "insert", merge: !(previous == ' ' && ch != ' '));
            previous = ch;
        }

        Assert.Equal("one two", engine.Text);

        var update = engine.Undo();

        Assert.Equal("one ", engine.Text);
        Assert.Equal((4, 4), (update!.SelectionStart, update.SelectionEnd));
        Assert.Equal("one ", update.Text);

        engine.Undo();
        Assert.Equal("", engine.Text);

        engine.Redo();
        Assert.Equal("one ", engine.Text);
    }

    [Fact]
    public void BackspaceCoalescesBackwards()
    {
        var engine = new MarkdownEditorEngine("abc");

        engine.Apply(2, 3, "", (2, 2), "delete", merge: true);
        engine.Apply(1, 2, "", (1, 1), "delete", merge: true);

        Assert.Equal("a", engine.Text);
        engine.Undo();
        Assert.Equal("abc", engine.Text);
    }

    [Theory]
    [InlineData("*", "\\*")]
    [InlineData("a_b", "a_b")]
    [InlineData("_a", "\\_a")]
    [InlineData("[x]", "\\[x\\]")]
    [InlineData("# not a heading", "\\# not a heading")]
    [InlineData("- not a list", "\\- not a list")]
    [InlineData("1. not a list", "1\\. not a list")]
    [InlineData("12 apples", "12 apples")]
    [InlineData("a - b", "a - b")]
    public void LiteralTextIsEscapedAtLineStart(string typed, string expected)
    {
        var engine = new MarkdownEditorEngine("");

        engine.InsertText(0, 0, typed, literal: true);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void LiteralTextInTheMiddleOfALineDoesNotEscapeBlockMarkers()
    {
        var engine = new MarkdownEditorEngine("ab");

        engine.InsertText(1, 1, "# ", literal: true);

        Assert.Equal("a# b", engine.Text);
    }

    [Theory]
    [InlineData("para", 4, "para\n\n", 6)]
    [InlineData("# head", 6, "# head\n\n", 8)]
    [InlineData("> quote", 7, "> quote\n>\n>", 11)]
    [InlineData("- item", 6, "- item\n- ", 9)]
    [InlineData("- [ ] task", 10, "- [ ] task\n- [ ] ", 17)]
    [InlineData("1. one", 6, "1. one\n2. ", 10)]
    [InlineData("- a\n  - b", 9, "- a\n  - b\n  - ", 14)]
    [InlineData("> - a", 5, "> - a\n> - ", 10)]
    [InlineData("> - a\n> - ", 10, "> - a\n>\n>", 9)]
    [InlineData("- a\n- ", 6, "- a\n\n", 5)]
    [InlineData("```\ncode\n```", 8, "```\ncode\n\n```", 9)]
    [InlineData("ab", 1, "a\n\nb", 3)]
    [InlineData("a\n\n", 3, "a\n\n\n", 4)]
    [InlineData("a\n\n\n\nb", 3, "a\n\n\n\n\nb", 4)]
    [InlineData("> a\n>\n> ", 8, "> a\n\n", 5)]
    [InlineData("```\nc\n```", 9, "```\nc\n```\n\n", 11)]
    [InlineData("| a |\n| - |", 11, "| a |\n| - |\n\n", 13)]
    [InlineData("---", 0, "\n\n---", 0)]
    [InlineData("**bold** rest", 6, "**bold**\n\nrest", 10)]
    [InlineData("a *em*", 3, "a\n\n*em*", 3)]
    public void EnterInsertsTheBreakForTheContainingBlock(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertParagraph(caret, caret);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("para", 4, "para  \n", 7)]
    [InlineData("> quote", 7, "> quote  \n> ", 12)]
    [InlineData("- item", 6, "- item  \n  ", 11)]
    public void ShiftEnterInsertsAHardBreakWithTheContinuationPrefix(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertLineBreak(caret, caret);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("**bold** plain", 6, "**bold**  plain", 9)]
    [InlineData("**bold**", 6, "**bold** ", 9)]
    [InlineData("a *em*", 5, "a *em* ", 7)]
    [InlineData("**bold**", 2, "**bold**", 0)]
    [InlineData("plain", 3, "pla in", 4)]
    public void WhitespaceTypedAtADelimiterEdgeGoesOutsideTheDelimiters(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertText(caret, caret, " ", literal: true);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update.SelectionStart);
    }

    [Theory]
    [InlineData("- [x] a\n- [ ] b", 13, 14, "- [x] a\n\nb", 9)]
    [InlineData("- [ ] ", 5, 6, "", 0)]
    [InlineData("- a\n- b", 5, 6, "- a\n\nb", 5)]
    [InlineData("# head", 1, 2, "head", 0)]
    [InlineData("> quoted", 1, 2, "quoted", 0)]
    [InlineData("- a\n- bc", 6, 7, "- a\n- c", 6)]
    [InlineData("ab", 1, 2, "a", 1)]
    public void BackspaceAtAMarkerRemovesTheWholeMarker(string text, int start, int end, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(start, end);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update.SelectionStart);
    }

    [Theory]
    [InlineData("a\n\n\n", 4, "a\n\n\n\nx", 6)]
    [InlineData("a\n\n\n", 3, "a\n\nx\n\n", 4)]
    [InlineData("a\n\n", 3, "a\n\nx", 4)]
    [InlineData("\n\n\nb", 1, "\n\nx\n\nb", 3)]
    [InlineData("> a\n>\n>\n> ", 11, "> a\n>\n>\n>\n> x", 13)]
    [InlineData("ab", 1, "axb", 2)]
    [InlineData("```\nc\n```", 9, "```\nc\n```\n\nx", 12)]
    [InlineData("| a |\n| - |", 11, "| a |\n| - |\n\nx", 14)]
    [InlineData("---", 3, "---\n\nx", 6)]
    [InlineData("```\nc\n```", 0, "x\n\n```\nc\n```", 1)]
    [InlineData("```\nc\n```\n", 10, "```\nc\n```\n\nx", 12)]
    [InlineData("```\nc", 5, "```\ncx\n", 6)]
    public void TypingOnAnEmptyLineKeepsTheNeighboringEmptyLines(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertText(caret, caret, "x", literal: true);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update.SelectionStart);
    }

    [Theory]
    [InlineData("| a |\n| - |\n\n```\nc\n```", 4, 15)]
    [InlineData("| a |\n| - |\n\n```\nc\n```", 11, 15)]
    [InlineData("p\n\n```\nc\n```", 1, 7)]
    [InlineData("```\nc\n```", 3, 4)]
    [InlineData("```\nc\n```", 5, 7)]
    [InlineData("```\nc\n```\n\np", 9, 11)]
    public void DeletingAcrossASealedBlockBoundaryDoesNothing(string text, int start, int end)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.Delete(start, end);

        Assert.Equal(text, engine.Text);
    }

    [Theory]
    [InlineData("**bold**\n\nx", 6, 10, "**bold**x", 6)]
    [InlineData("**bold** text", 4, 11, "**bo**xt", 4)]
    [InlineData("a **bold** b", 0, 5, "**old** b", 0)]
    [InlineData("**bold**", 2, 6, "", 0)]
    [InlineData("x **bold** y", 3, 7, "x **d** y", 2)]
    [InlineData("[text](u) more", 5, 12, "[text](u)re", 5)]
    [InlineData("`code` x", 4, 7, "`cod`x", 4)]
    [InlineData("plain text", 2, 5, "pl text", 2)]
    public void DeletingAcrossADelimiterKeepsTheDelimiter(string text, int start, int end, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(start, end);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("[link](u) rest", 5, "[link](u)\n\nrest", 11)]
    [InlineData("`code` rest", 5, "`code`\n\nrest", 8)]
    [InlineData("a [link](u)", 3, "a\n\n[link](u)", 3)]
    public void EnterAtTheEdgeOfALinkOrCodeSpanBreaksOutsideIt(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertParagraph(caret, caret);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("p\n\n- [ ] b", 1, 9, "pb", 1)]
    [InlineData("ab", 1, 2, "a", 1)]
    public void ForwardDeleteJoinsInsteadOfRemovingMarkers(string text, int start, int end, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(start, end, forward: true);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("- a\n\n> q", 3, 7, "- a\n\nq", 5)]
    [InlineData("# h\n\np", 2, 6, "", 0)]
    [InlineData("- a\n  - b", 3, 8, "- a\n- b", 6)]
    [InlineData("p\n\n- a\n- b", 1, 10, "p", 1)]
    public void BackspaceAtBlockStartsHandlesMarkersRegardlessOfTheBrowserRange(string text, int start, int end, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(start, end);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void UndoOfMergedBackspacesRestoresACollapsedCaret()
    {
        var engine = new MarkdownEditorEngine("abc");

        engine.Delete(2, 3, false, "delete", merge: true);
        engine.Delete(1, 2, false, "delete", merge: true);
        var update = engine.Undo();

        Assert.Equal("abc", engine.Text);
        Assert.Equal((3, 3), (update!.SelectionStart, update.SelectionEnd));
    }

    [Theory]
    [InlineData("ab\n\n- [x] cd", 1, 11, "a**b**\n\n- [x] **c**d", 3, 17)]
    [InlineData("Edit this", 4, 4, "**Edit** this", 6, 6)]
    [InlineData("**Edit** this", 6, 6, "Edit this", 4, 4)]
    [InlineData("ab", 0, 2, "**ab**", 2, 4)]
    public void InlineCommandsApplyPerBlockAndKeepACollapsedCaret(string text, int start, int end, string expected, int selectionStart, int selectionEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(MarkdownEditorCommands.Bold, start, end, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((selectionStart, selectionEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Theory]
    [InlineData("ab\n\ncd", 2, "ab\n\n---\n\n\n\ncd", 9)]
    [InlineData("ab", 2, "ab\n\n---\n\n", 9)]
    [InlineData("", 0, "---\n\n", 5)]
    public void HorizontalRuleGoesAfterTheCurrentBlock(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(MarkdownEditorCommands.HorizontalRule, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("# ab\n\ncd", 3, "```\nab\n```\n\ncd", 5, 5)]
    [InlineData("ab", 1, "```\nab\n```", 5, 5)]
    public void CodeBlockWithACollapsedCaretConvertsTheBlock(string text, int caret, string expected, int selectionStart, int selectionEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(MarkdownEditorCommands.CodeBlock, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((selectionStart, selectionEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Theory]
    [InlineData("quote", "ab", 1, "> ab", 3, 3)]
    [InlineData("unorderedList", "ab", 0, "- ab", 2, 2)]
    [InlineData("orderedList", "- ab", 3, "1. ab", 4, 4)]
    [InlineData("taskList", "- ab", 3, "- [ ] ab", 7, 7)]
    [InlineData("unorderedList", "- [ ] ab", 7, "- ab", 3, 3)]
    [InlineData("formatBlock", "ab", 1, "## ab", 4, 4)]
    public void BlockCommandsKeepACollapsedCaretInTheContent(string command, string text, int caret, string expected, int selectionStart, int selectionEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(command, caret, caret, command == "formatBlock" ? "h2" : null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((selectionStart, selectionEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void BlockCommandOnASelectionSelectsTheContentWithoutTheMarkers()
    {
        var engine = new MarkdownEditorEngine("ab\ncd");

        var update = engine.Command(MarkdownEditorCommands.Quote, 0, 5, null, null);

        Assert.Equal("> ab\n> cd", engine.Text);
        Assert.Equal((2, 9), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void EnterOnAnEmptyItemInTheMiddleOfAListSplitsTheList()
    {
        var engine = new MarkdownEditorEngine("- a\n- [ ] \n- c");

        var update = engine.InsertParagraph(10, 10);

        Assert.Equal("- a\n\n\n\n* c", engine.Text);
        Assert.Equal(5, update!.SelectionStart);
    }

    [Fact]
    public void EnterOnAnEmptyLastItemBeforeAParagraphAddsOneEmptyLine()
    {
        var engine = new MarkdownEditorEngine("- a\n- \n\nP");

        var update = engine.InsertParagraph(6, 6);

        Assert.Equal("- a\n\n\n\nP", engine.Text);
        Assert.Equal(5, update!.SelectionStart);
    }

    [Theory]
    [InlineData("unorderedList", "p\n\n- a", "* p\n\n- a")]
    [InlineData("unorderedList", "- a\n\np", "- a\n\n* p")]
    [InlineData("orderedList", "1. a\n\np", "1. a\n\n1) p")]
    [InlineData("unorderedList", "p", "- p")]
    public void ListCommandsUseAnAlternateMarkerNextToAnExistingList(string command, string text, string expected)
    {
        var engine = new MarkdownEditorEngine(text);
        var paragraph = text.IndexOf('p');

        engine.Command(command, paragraph, paragraph, null, null);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void SelectAllThenBackspaceClearsADocumentWithSealedBlocks()
    {
        var text = "# h\n\n| a |\n| - |\n\n```\nc\n```";
        var engine = new MarkdownEditorEngine(text);

        engine.Delete(2, text.Length);

        Assert.Equal("", engine.Text);
    }

    [Fact]
    public void UndoAfterABlockCommandRestoresTheCollapsedCaret()
    {
        var engine = new MarkdownEditorEngine("ab cd");

        engine.Command(MarkdownEditorCommands.Quote, 3, 3, null, null);
        var update = engine.Undo();

        Assert.Equal("ab cd", engine.Text);
        Assert.Equal((3, 3), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void TypingAHeadingMarkerEscapesIt()
    {
        var engine = new MarkdownEditorEngine("Start");

        engine.InsertText(0, 0, "#", literal: true);
        Assert.Equal("#Start", engine.Text);

        var update = engine.InsertText(1, 1, " ", literal: true);
        Assert.Equal("\\# Start", engine.Text);
        Assert.Equal(3, update.SelectionStart);
    }

    [Theory]
    [InlineData("1", ".", " ", "1\\. x")]
    [InlineData("-", " ", "", "\\- x")]
    public void TypingAListMarkerEscapesIt(string first, string second, string third, string expected)
    {
        var engine = new MarkdownEditorEngine("x");

        var position = 0;
        foreach (var typed in new[] { first, second, third }.Where(t => t.Length > 0))
        {
            var update = engine.InsertText(position, position, typed, literal: true);
            position = update.SelectionStart;
        }

        Assert.Equal(expected, engine.Text);
    }

    [Theory]
    [InlineData("| a |\n| - |\n\n```\nc\n```", 12, "| a |\n| - |\n\nx\n\n```\nc\n```", 14)]
    [InlineData("| a |\n| - |\n```\nc\n```", 12, "| a |\n| - |\n\nx\n\n```\nc\n```", 14)]
    [InlineData("a\n\nb", 2, "a\n\nx\n\nb", 4)]
    public void TypingOnASeparatorLineStartsAParagraphBetweenTheBlocks(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertText(caret, caret, "x", literal: true);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update.SelectionStart);
    }

    [Fact]
    public void BackspaceAtTheStartOfANestedItemOutdentsAndKeepsTheCaretThere()
    {
        var engine = new MarkdownEditorEngine("- one\n  - nested\n- two");

        var update = engine.Delete(9, 10);

        Assert.Equal("- one\n- nested\n- two", engine.Text);
        Assert.Equal(8, update!.SelectionStart);
    }

    [Fact]
    public void TypingAListMarkerInsideAListItemEscapesIt()
    {
        var engine = new MarkdownEditorEngine("- Undo");

        engine.InsertText(2, 2, "-", literal: true);
        var update = engine.InsertText(3, 3, " ", literal: true);

        Assert.Equal("- \\- Undo", engine.Text);
        Assert.Equal(5, update.SelectionStart);
    }

    [Fact]
    public void UndoAfterTypingAMarkerRestoresACollapsedCaret()
    {
        var engine = new MarkdownEditorEngine("Start");

        engine.InsertText(0, 0, "#", literal: true);
        engine.InsertText(1, 1, " ", literal: true);
        var update = engine.Undo();

        Assert.Equal("#Start", engine.Text);
        Assert.Equal((1, 1), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void HorizontalRuleInsideAParagraphSplitsIt()
    {
        var engine = new MarkdownEditorEngine("ab cd");

        var update = engine.Command(MarkdownEditorCommands.HorizontalRule, 3, 3, null, null);

        Assert.Equal("ab\n\n---\n\ncd", engine.Text);
        Assert.Equal(9, update!.SelectionStart);
    }

    [Fact]
    public void ConvertingAnItemBackToBulletsRejoinsTheAdjacentList()
    {
        var engine = new MarkdownEditorEngine("- a\n1. b");

        engine.Command(MarkdownEditorCommands.UnorderedList, 7, 7, null, null);

        Assert.Equal("- a\n- b", engine.Text);
    }

    [Fact]
    public void EnterTwiceAtTheEndOfACodeBlockExitsIt()
    {
        var engine = new MarkdownEditorEngine("```\ncode\n```");

        engine.InsertParagraph(9, 9);
        Assert.Equal("```\ncode\n\n```", engine.Text);

        var update = engine.InsertParagraph(10, 10);

        Assert.Equal("```\ncode\n```\n\n", engine.Text);
        Assert.Equal(14, update!.SelectionStart);
    }

    [Theory]
    [InlineData("strikethrough", "to *markdown*. **x**", 6, "to *~~markdown~~*. **x**", 8)]
    [InlineData("bold", "to *markdown*. **x**", 6, "to ***markdown***. **x**", 8)]
    [InlineData("bold", "say hello, world", 6, "say **hello**, world", 8)]
    [InlineData("bold", "a **b** c", 1, "**a** **b** c", 3)]
    public void CollapsedCaretCommandsUseTheWordOrTheEnclosingInline(string command, string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(command, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceAtTheStartOfAnItemWithChildrenDedentsThem()
    {
        var engine = new MarkdownEditorEngine("- one\n- two\n    - three");

        var update = engine.Delete(7, 8);

        Assert.Equal("- one\n\ntwo\n\n- three", engine.Text);
        Assert.Equal(7, update!.SelectionStart);
    }

    [Fact]
    public void OutdentingAnItemMovesItsChildrenWithIt()
    {
        var engine = new MarkdownEditorEngine("- one\n  - two\n    - three");

        engine.Delete(9, 10);

        Assert.Equal("- one\n- two\n  - three", engine.Text);
    }

    [Fact]
    public void EnterAtTheEndOfACodeLineAddsOneLine()
    {
        var engine = new MarkdownEditorEngine("```\ncode\n```");

        engine.InsertParagraph(8, 8);
        var update = engine.InsertText(9, 9, "C", literal: true);

        Assert.Equal("```\ncode\nC\n```", engine.Text);
        Assert.Equal(10, update.SelectionStart);
    }

    [Fact]
    public void QuoteWithACaretInTheMiddleOfALineDoesNotDrift()
    {
        var engine = new MarkdownEditorEngine("it round");

        var update = engine.Command(MarkdownEditorCommands.Quote, 3, 3, null, null);

        Assert.Equal("> it round", engine.Text);
        Assert.Equal(5, update!.SelectionStart);
    }

    [Fact]
    public void PastedLinesBecomeParagraphs()
    {
        var engine = new MarkdownEditorEngine("p");

        engine.InsertText(1, 1, "a\nb\n\nc", literal: true, paragraphs: true);

        Assert.Equal("pa\n\nb\n\nc", engine.Text);
    }

    [Fact]
    public void EnterTrimsTrailingSpacesBeforeTheBreak()
    {
        var engine = new MarkdownEditorEngine("it round");

        var update = engine.InsertParagraph(3, 3);

        Assert.Equal("it\n\nround", engine.Text);
        Assert.Equal(4, update!.SelectionStart);
    }

    [Fact]
    public void UndoAfterReplacingASelectionRestoresIt()
    {
        var engine = new MarkdownEditorEngine("abc");

        engine.InsertText(0, 3, "T", literal: true, selection: true);
        var update = engine.Undo();

        Assert.Equal((0, 3), (update!.SelectionStart, update.SelectionEnd));
    }

    [Theory]
    [InlineData("bold", "The quick **brown** fox", 12, 23, "The quick **brown fox**", 12, 21)]
    [InlineData("bold", "fox **juZZmps**", 0, 10, "**fox juZZmps**", 2, 10)]
    [InlineData("italic", "a *b* c *d* e", 3, 10, "a *b c d* e", 3, 8)]
    public void InlineCommandsAbsorbPartiallySelectedInlines(string command, string text, int start, int end, string expected, int expectedStart, int expectedEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(command, start, end, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((expectedStart, expectedEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void ForwardDeleteBeforeATableMovesIntoTheTable()
    {
        var engine = new MarkdownEditorEngine("> quote\n\n| a |\n| - |\n| b |");

        var update = engine.Delete(7, 8, forward: true);

        Assert.Equal("> quote\n\n| a |\n| - |\n| b |", engine.Text);
        Assert.Equal(11, update!.SelectionStart);
    }

    [Fact]
    public void EnterOnAnEmptyQuoteLineLeavesTheQuote()
    {
        var engine = new MarkdownEditorEngine("> quote\n>\n> \n\n| a |\n| - |");

        var update = engine.InsertParagraph(12, 12);

        Assert.Equal("> quote\n\n\n\n| a |\n| - |", engine.Text);
        Assert.Equal(9, update!.SelectionStart);
    }

    [Fact]
    public void CodeBlockCommandInsideACodeBlockConvertsItBack()
    {
        var engine = new MarkdownEditorEngine("```\nThe quick\n```");

        var update = engine.Command(MarkdownEditorCommands.CodeBlock, 8, 8, null, null);

        Assert.Equal("The quick", engine.Text);
        Assert.Equal(4, update!.SelectionStart);
    }

    [Fact]
    public void EnterOnTheTrailingEmptyLineOfACodeBlockLeavesIt()
    {
        var engine = new MarkdownEditorEngine("```\ncode\n\n```");

        var update = engine.InsertParagraph(9, 9);

        Assert.Equal("```\ncode\n```\n\n", engine.Text);
        Assert.Equal(14, update!.SelectionStart);
    }

    [Fact]
    public void TypingOverASelectionAndContinuingIsOneUndoStep()
    {
        var engine = new MarkdownEditorEngine("abc");

        engine.InsertText(0, 3, "R", literal: true, key: "insert", merge: false, selection: true);
        engine.InsertText(1, 1, "E", literal: true, key: "insert", merge: true);
        var update = engine.Undo();

        Assert.Equal("abc", engine.Text);
        Assert.Equal((0, 3), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void BackspaceAtTheStartOfTheFirstItemKeepsTheRestOfTheList()
    {
        var engine = new MarkdownEditorEngine("1. first\n2. mid\n3. second\n\nEnd.");

        engine.Delete(2, 3);

        Assert.Equal("first\n\n1. mid\n2. second\n\nEnd.", engine.Text);
    }

    [Theory]
    [InlineData("taskList", "- [x] a\n- [ ] b\n- c", 9, "- [x] a\n\nb\n\n- c")]
    [InlineData("quote", "- a\n- b", 5, "> - a\n> - b")]
    public void BlockCommandsSeparateTheResultFromNeighbouringListLines(string command, string text, int caret, string expected)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.Command(command, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void EnterOnAnEmptyNestedItemOutdentsIt()
    {
        var engine = new MarkdownEditorEngine("- one\n  - a\n  - \n- two");

        var update = engine.InsertParagraph(16, 16);

        Assert.Equal("- one\n  - a\n- \n- two", engine.Text);
        Assert.Equal(14, update!.SelectionStart);
    }

    [Fact]
    public void EnterInAnOrderedListRenumbersTheFollowingItems()
    {
        var engine = new MarkdownEditorEngine("1. first\n2. second\n3. third");

        engine.InsertParagraph(8, 8);

        Assert.Equal("1. first\n2. \n3. second\n4. third", engine.Text);
    }

    [Fact]
    public void TabIndentsAnItemUnderThePreviousOneAndShiftTabOutdentsIt()
    {
        var engine = new MarkdownEditorEngine("- one\n- two\n  - three");

        var update = engine.Indent(8, 8, outdent: false);

        Assert.Equal("- one\n  - two\n    - three", engine.Text);
        Assert.Equal(10, update!.SelectionStart);

        update = engine.Indent(10, 10, outdent: true);

        Assert.Equal("- one\n- two\n  - three", engine.Text);
        Assert.Equal(8, update!.SelectionStart);
        Assert.Null(engine.Indent(2, 2, outdent: false));
    }

    [Theory]
    [InlineData("bold", "Edit **directly** now", 9, 13, "Edit **di**rect**ly** now", 11, 15)]
    [InlineData("bold", "Edit **directly** now", 7, 9, "Edit di**rectly** now", 5, 7)]
    [InlineData("italic", "a *b c* d", 4, 6, "a *b* c d", 5, 7)]
    public void RemovingAFormatFromPartOfARunKeepsTheRest(string command, string text, int start, int end, string expected, int expectedStart, int expectedEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(command, start, end, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((expectedStart, expectedEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void EnterInATableCellAddsARowAndEnterOnAnEmptyLastRowLeavesTheTable()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| - | - |\n| c | d |");

        var update = engine.InsertParagraph(23, 23);

        Assert.Equal("| a | b |\n| - | - |\n| c | d |\n|  |  |", engine.Text);
        Assert.Equal(32, update!.SelectionStart);

        update = engine.InsertParagraph(32, 32);

        Assert.Equal("| a | b |\n| - | - |\n| c | d |\n\n", engine.Text);
        Assert.Equal(31, update!.SelectionStart);
    }

    [Fact]
    public void EnterInTheHeaderRowAddsTheRowBelowTheDelimiter()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| - | - |\n| c | d |");

        engine.InsertParagraph(3, 3);

        Assert.Equal("| a | b |\n| - | - |\n|  |  |\n| c | d |", engine.Text);
    }

    [Fact]
    public void BackspaceOnAnEmptyParagraphAfterAQuoteRemovesIt()
    {
        var engine = new MarkdownEditorEngine("> q\n\n\n\n| a |\n| - |");

        var update = engine.Delete(4, 5);

        Assert.Equal("> q\n\n| a |\n| - |", engine.Text);
        Assert.NotNull(update);
    }

    [Fact]
    public void BackspaceOnAnEmptyParagraphDoesNotLeaveExtraBlankLines()
    {
        var engine = new MarkdownEditorEngine("a\n\nb");

        engine.InsertParagraph(1, 1);
        Assert.Equal("a\n\n\n\nb", engine.Text);
        engine.Delete(2, 3);

        Assert.Equal("a\n\nb", engine.Text);
    }

    [Fact]
    public void InlineCommandsAreIgnoredInsideCodeBlocks()
    {
        var engine = new MarkdownEditorEngine("```\nvar x;\n```");

        Assert.Null(engine.Command(MarkdownEditorCommands.Bold, 6, 6, null, null));
        Assert.Null(engine.Command(MarkdownEditorCommands.Code, 4, 7, null, null));
    }

    [Fact]
    public void ConvertingSeveralParagraphsToAListMakesATightList()
    {
        var engine = new MarkdownEditorEngine("a\n\nb\n\nc");

        engine.Command(MarkdownEditorCommands.UnorderedList, 0, 9, null, null);

        Assert.Equal("- a\n- b\n- c", engine.Text);
    }

    [Fact]
    public void ChangingTheListTypeFromAnItemChangesTheWholeList()
    {
        var engine = new MarkdownEditorEngine("- a\n- b\n- c");

        engine.Command(MarkdownEditorCommands.OrderedList, 6, 6, null, null);
        Assert.Equal("1. a\n2. b\n3. c", engine.Text);

        engine.Command(MarkdownEditorCommands.UnorderedList, 7, 7, null, null);
        Assert.Equal("- a\n- b\n- c", engine.Text);

        engine.Command(MarkdownEditorCommands.UnorderedList, 6, 6, null, null);
        Assert.Equal("- a\n\nb\n\n- c", engine.Text);
    }

    [Fact]
    public void TabJoinsThePreviousItemsNestedList()
    {
        var engine = new MarkdownEditorEngine("1. a\n   - x\n2. b");

        var update = engine.Indent(13, 13, outdent: false);

        Assert.Equal("1. a\n   - x\n   - b", engine.Text);
        Assert.Equal(17, update!.SelectionStart);
    }

    [Fact]
    public void TabInAnOrderedListStartsTheNestedListAtOne()
    {
        var engine = new MarkdownEditorEngine("1. a\n2. b\n   c");

        engine.Indent(8, 8, outdent: false);

        Assert.Equal("1. a\n   1. b\n      c", engine.Text);
    }

    [Fact]
    public void DeleteMergingOrderedItemsRenumbersTheRest()
    {
        var engine = new MarkdownEditorEngine("1. first\n2. second\n3. mid");

        engine.Delete(8, 12, forward: true);

        Assert.Equal("1. firstsecond\n2. mid", engine.Text);
    }

    [Theory]
    [InlineData("a **Des|ign** b", "a **Des**\n\n**ign** b")]
    [InlineData("a *x|y* b", "a *x*\n\n*y* b")]
    [InlineData("[li|nks](https://x.io)", "[li](https://x.io)\n\n[nks](https://x.io)")]
    [InlineData("- `co|de` x", "- `co`\n- `de` x")]
    [InlineData("**a| b**", "**a**\n\n**b**")]
    public void EnterInsideAnInlineClosesAndReopensIt(string text, string expected)
    {
        var caret = text.IndexOf('|');
        var engine = new MarkdownEditorEngine(text.Replace("|", ""));

        engine.InsertParagraph(caret, caret);

        Assert.Equal(expected, engine.Text);
    }

    [Theory]
    [InlineData("italic", "*~~Toggle~~*", 3, 9, "~~Toggle~~", 2, 8)]
    [InlineData("bold", "**~~Source~~**", 4, 10, "~~Source~~", 2, 8)]
    [InlineData("strikethrough", "~~*WYSIWYG*~~", 3, 10, "*WYSIWYG*", 1, 8)]
    [InlineData("bold", "***Design***", 3, 9, "*Design*", 1, 7)]
    public void RemovingAFormatFromANestedRunRemovesOnlyThatFormat(string command, string text, int start, int end, string expected, int expectedStart, int expectedEnd)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(command, start, end, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal((expectedStart, expectedEnd), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void BoldingTextBetweenTwoBoldRunsMergesThem()
    {
        var engine = new MarkdownEditorEngine("**di**rect**ly**");

        engine.Command(MarkdownEditorCommands.Bold, 6, 10, null, null);

        Assert.Equal("**directly**", engine.Text);
    }

    [Fact]
    public void OutdentingANestedItemJoinsTheParentListWithItsType()
    {
        var engine = new MarkdownEditorEngine("1. one\n2. two\n   - nested\n3. three");

        engine.Indent(20, 20, outdent: true);

        Assert.Equal("1. one\n2. two\n3. nested\n4. three", engine.Text);
    }

    [Fact]
    public void BackspaceAtTheStartOfANestedItemJoinsTheParentListWithItsType()
    {
        var engine = new MarkdownEditorEngine("1. one\n2. two\n   - nested\n3. three");

        engine.Delete(18, 19);

        Assert.Equal("1. one\n2. two\n3. nested\n4. three", engine.Text);
    }

    [Fact]
    public void BackspaceMergingAParagraphIntoAListItemKeepsTheListTight()
    {
        var engine = new MarkdownEditorEngine("- a\n\nb\n\n- c");

        engine.Delete(3, 5);

        Assert.Equal("- ab\n- c", engine.Text);
    }

    [Theory]
    [InlineData("# Hello, Markdown", 7, "# Hello\n\n# , Markdown", 11)]
    [InlineData("# Hello", 2, "\n\n# Hello", 4)]
    [InlineData("# Hello", 7, "# Hello\n\n", 9)]
    public void EnterInsideAHeadingKeepsTheTailAHeading(string text, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.InsertParagraph(caret, caret);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void RemovingTheListFromTheFirstOrderedItemRenumbersTheRest()
    {
        var engine = new MarkdownEditorEngine("1. a\n2. b\n3. c");

        engine.Command(MarkdownEditorCommands.OrderedList, 3, 3, null, null);

        Assert.Equal("a\n\n1. b\n2. c", engine.Text);
    }

    [Fact]
    public void BackspaceOnAnEmptyTableRowRemovesIt()
    {
        var engine = new MarkdownEditorEngine("| a |\n| - |\n| b |\n|  |");

        var update = engine.Delete(20, 21);

        Assert.Equal("| a |\n| - |\n| b |", engine.Text);
        Assert.NotNull(update);
    }

    [Fact]
    public void BackspaceAtTheStartOfTheParagraphAfterATableMovesIntoTheTable()
    {
        var engine = new MarkdownEditorEngine("| a |\n| - |\n\nAfter");

        var update = engine.Delete(12, 13);

        Assert.Equal("| a |\n| - |\n\nAfter", engine.Text);
        Assert.Equal(3, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceRemovesOneEmptyParagraphAtATime()
    {
        var engine = new MarkdownEditorEngine("a\n\nb");

        engine.InsertParagraph(1, 1);
        engine.InsertParagraph(3, 3);
        Assert.Equal("a\n\n\n\n\nb", engine.Text);

        engine.Delete(3, 4);
        Assert.Equal("a\n\n\n\nb", engine.Text);

        engine.Delete(2, 3);
        Assert.Equal("a\n\nb", engine.Text);
    }

    [Fact]
    public void TabRenumbersTheFollowingOrderedItems()
    {
        var engine = new MarkdownEditorEngine("1. a\n2. b\n3. c");

        engine.Indent(8, 8, outdent: false);

        Assert.Equal("1. a\n   1. b\n2. c", engine.Text);
    }

    [Fact]
    public void EnterInATableCellKeepsTheColumn()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| - | - |\n| c | d |");

        var update = engine.InsertParagraph(27, 27);

        Assert.Equal("| a | b |\n| - | - |\n| c | d |\n|  |  |", engine.Text);
        Assert.Equal(35, update!.SelectionStart);
    }

    [Fact]
    public void StateReportsInlineFormatsInsideTableCells()
    {
        var engine = new MarkdownEditorEngine("| **a** | b |\n| - | - |");

        Assert.Contains(MarkdownEditorCommands.Bold, engine.State(5, 5).Formats!);
    }

    [Fact]
    public void DeletingASelectionFromAParagraphIntoAListKeepsTheBlankLine()
    {
        var engine = new MarkdownEditorEngine("toolbar. New\n\n- [x] A task list item\n- [ ] Bold");

        engine.Delete(8, 22, selection: true);

        Assert.Equal("toolbar.task list item\n\n- [ ] Bold", engine.Text);
    }

    [Fact]
    public void BackspaceAtTheStartOfAHeadingBelowAnEmptyParagraphRemovesTheParagraph()
    {
        var engine = new MarkdownEditorEngine("# Hello");

        var update = engine.InsertParagraph(2, 2);
        Assert.Equal("\n\n# Hello", engine.Text);

        update = engine.Delete(update!.SelectionStart - 1, update.SelectionStart);

        Assert.Equal("# Hello", engine.Text);
        Assert.Equal(2, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceAtTheStartOfAHeadingAfterAParagraphAndAnEmptyLineRemovesTheEmptyLine()
    {
        var engine = new MarkdownEditorEngine("a\n\n\n\n# Hello");

        engine.Delete(6, 7);

        Assert.Equal("a\n\n# Hello", engine.Text);
    }

    [Fact]
    public void EnterOnAnEmptyNestedItemRenumbersTheOuterList()
    {
        var engine = new MarkdownEditorEngine("1. one\n2. two\n   - sub\n   - \n3. three");

        engine.InsertParagraph(28, 28);

        Assert.Equal("1. one\n2. two\n   - sub\n3. \n4. three", engine.Text);
    }

    [Fact]
    public void AnEmptyParagraphReportsTheParagraphBlock()
    {
        var engine = new MarkdownEditorEngine("a\n\n\n\nb");

        Assert.Equal("p", engine.State(3, 3).Block);
    }

    [Fact]
    public void BoldingTextThatTouchesABoldRunKeepsTheUserSelection()
    {
        var engine = new MarkdownEditorEngine("**Design** and **Source**");

        var update = engine.Command(MarkdownEditorCommands.Bold, 10, 15, null, null);

        Assert.Equal("**Design and Source**", engine.Text);
        Assert.Equal((8, 13), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void EnterThenBackspaceBeforeAListRestoresTheText()
    {
        var engine = new MarkdownEditorEngine("toolbar.\n\n- [x] A task");

        var update = engine.InsertParagraph(8, 8);
        engine.Delete(update!.SelectionStart - 1, update.SelectionStart);

        Assert.Equal("toolbar.\n\n- [x] A task", engine.Text);
    }

    [Fact]
    public void ListOnASelectionWithAHeadingIncludesTheHeadingAndTogglesBackExactly()
    {
        var engine = new MarkdownEditorEngine("# Hello\n\nEdit this\n\n- [x] A");

        var update = engine.Command(MarkdownEditorCommands.OrderedList, 3, 14, null, null);
        Assert.Equal("1. # Hello\n2. Edit this\n\n- [x] A", engine.Text);

        engine.Command(MarkdownEditorCommands.OrderedList, update!.SelectionStart, update.SelectionEnd, null, null);
        Assert.Equal("# Hello\n\nEdit this\n\n- [x] A", engine.Text);
    }

    [Fact]
    public void HeadingOnTaskItemsDropsTheTaskBox()
    {
        var engine = new MarkdownEditorEngine("- [ ] A task\n- [x] B");

        engine.Command(MarkdownEditorCommands.FormatBlock, 0, 18, "h2", null);

        Assert.Equal("- ## A task\n- ## B", engine.Text);
    }

    [Fact]
    public void EmptyTableCellsRenderAPlaceholderAndEnterPutsTheCaretThere()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| - | - |\n| c | d |");

        var update = engine.InsertParagraph(27, 27);

        Assert.Equal(35, update!.SelectionStart);
        Assert.Contains("<tr><td>\u200B</td><td>\u200B</td></tr>", update.Html);
    }

    [Fact]
    public void BackspaceMergingAParagraphBetweenTwoListsRejoinsThem()
    {
        var engine = new MarkdownEditorEngine("- a\n\nmid\n\n* b\n* c");

        engine.Delete(3, 5);

        Assert.Equal("- amid\n- b\n- c", engine.Text);
    }

    [Fact]
    public void StateReportsOnlyTheInnermostList()
    {
        var engine = new MarkdownEditorEngine("1. a\n   - b");

        var state = engine.State(10, 10);

        Assert.Contains(MarkdownEditorCommands.UnorderedList, state.Formats!);
        Assert.DoesNotContain(MarkdownEditorCommands.OrderedList, state.Formats!);
    }

    [Fact]
    public void TheParagraphAfterACodeBlockReportsTheParagraphBlock()
    {
        var engine = new MarkdownEditorEngine("```\nx\n```");

        Assert.Equal("p", engine.State(9, 9).Block);
    }

    [Fact]
    public void TabInTheLastCellAppendsARowWithTheCaretInTheFirstCell()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| - | - |\n| c | d |");

        var update = engine.AppendRow(27);

        Assert.Equal("| a | b |\n| - | - |\n| c | d |\n|  |  |", engine.Text);
        Assert.Equal(32, update!.SelectionStart);

        engine.InsertText(32, 32, "T", literal: true);
        Assert.Equal("| a | b |\n| - | - |\n| c | d |\n| T |  |", engine.Text);
    }

    [Fact]
    public void UndoAfterAWholeListConversionRestoresACollapsedCaret()
    {
        var engine = new MarkdownEditorEngine("- a\n- b");

        engine.Command(MarkdownEditorCommands.OrderedList, 6, 6, null, null);
        var update = engine.Undo();

        Assert.Equal((6, 6), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void LiftingAMiddleOrderedItemOutRenumbersTheRestFromOne()
    {
        var engine = new MarkdownEditorEngine("1. a\n2. b\n3. c");

        engine.Delete(7, 8);

        Assert.Equal("1. a\n\nb\n\n1. c", engine.Text);
    }

    [Fact]
    public void BoldAcrossAnItalicBoundaryAndIntoABoldRunStaysValid()
    {
        var engine = new MarkdownEditorEngine("Edit *this text* **directly** now");

        var update = engine.Command(MarkdownEditorCommands.Bold, 11, 21, null, null);

        Assert.Equal("Edit *this **text*** **directly** now", engine.Text);
        Assert.Equal((13, 25), (update!.SelectionStart, update.SelectionEnd));

        update = engine.Undo();
        Assert.Equal("Edit *this text* **directly** now", engine.Text);
        Assert.Equal((11, 21), (update!.SelectionStart, update.SelectionEnd));
    }

    [Fact]
    public void BackspaceAtTheStartOfAHeadingAfterAHeadingJoinsThem()
    {
        var engine = new MarkdownEditorEngine("# Hello, Ma\n\n# rkdown");

        var update = engine.Delete(14, 15);

        Assert.Equal("# Hello, Markdown", engine.Text);
        Assert.Equal(11, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceAtTheStartOfAHeadingAfterAListRemovesTheMarker()
    {
        var engine = new MarkdownEditorEngine("- a\n\n# Hello");

        engine.Delete(6, 7);

        Assert.Equal("- a\n\nHello", engine.Text);
    }

    [Fact]
    public void JoiningAParagraphIntoANestedItemRejoinsTheOuterListWithContinuedNumbering()
    {
        var engine = new MarkdownEditorEngine("1. one\n2. two\n   - nb\n\nnew3\n\n1. three");

        engine.Delete(21, 23);

        Assert.Equal("1. one\n2. two\n   - nbnew3\n3. three", engine.Text);
    }

    [Fact]
    public void ForwardDeleteOfAnEmptyParagraphPutsTheCaretAtTheEndOfThePreviousBlock()
    {
        var engine = new MarkdownEditorEngine("> q\n\n\n\n| a |\n| - |");

        var update = engine.Delete(5, 6, forward: true);

        Assert.Equal("> q\n\n| a |\n| - |", engine.Text);
        Assert.Equal(3, update!.SelectionStart);
        Assert.Contains(MarkdownEditorCommands.Quote, update.State!.Formats!);
    }

    [Theory]
    [InlineData("unorderedList", "- ")]
    [InlineData("orderedList", "1. ")]
    [InlineData("taskList", "- [ ] ")]
    public void ListCommandsOnAnEmptyLineInsertTheMarker(string command, string marker)
    {
        var engine = new MarkdownEditorEngine("a\n\n\n\nb");

        var update = engine.Command(command, 3, 3, null, null);

        Assert.Equal("a\n\n" + marker + "\n\nb", engine.Text);
        Assert.Equal(3 + marker.Length, update!.SelectionStart);

        var empty = new MarkdownEditorEngine("");
        empty.Command(command, 0, 0, null, null);
        Assert.Equal(marker, empty.Text);
    }

    [Theory]
    [InlineData(MarkdownEditorCommands.UnorderedList, "a\n\n```csharp\nvar x;\n```\n\n- ")]
    [InlineData(MarkdownEditorCommands.TaskList, "a\n\n```csharp\nvar x;\n```\n\n- [ ] ")]
    [InlineData(MarkdownEditorCommands.Quote, "a\n\n```csharp\nvar x;\n```\n\n> ")]
    public void BlockCommandsOnTheTrailingEmptyLineAfterACodeBlockLeaveTheCodeBlockAlone(string command, string expected)
    {
        var engine = new MarkdownEditorEngine("a\n\n```csharp\nvar x;\n```");

        var update = engine.Command(command, engine.Text.Length, engine.Text.Length, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expected.Length, update!.SelectionStart);
    }

    [Fact]
    public void OutdentingAPlainItemFromATaskListKeepsItPlain()
    {
        var engine = new MarkdownEditorEngine("- [x] A\n- [ ] B\n  - C");

        engine.Indent(21, 21, outdent: true);

        Assert.Equal("- [x] A\n- [ ] B\n- C", engine.Text);
    }

    [Fact]
    public void ListCommandOnAnEmptyLineNextToAListUsesTheOtherMarker()
    {
        var engine = new MarkdownEditorEngine("a\n\n\n\n- [x] b");

        engine.Command(MarkdownEditorCommands.UnorderedList, 3, 3, null, null);

        Assert.Equal("a\n\n* \n\n- [x] b", engine.Text);
    }

    [Fact]
    public void DeletingInsideACodeBlockWorks()
    {
        var engine = new MarkdownEditorEngine("```\nabc\n```");

        engine.Delete(5, 6);

        Assert.Equal("```\nac\n```", engine.Text);
    }

    [Fact]
    public void CommandsGoThroughTheFormatterAndSelectTheResult()
    {
        var engine = new MarkdownEditorEngine("hello world");

        var update = engine.Command(MarkdownEditorCommands.Bold, 0, 5, null, null);

        Assert.Equal("**hello** world", engine.Text);
        Assert.Equal((2, 7), (update!.SelectionStart, update.SelectionEnd));
        Assert.Contains("bold", update.State!.Formats!);

        engine.Undo();
        Assert.Equal("hello world", engine.Text);
    }

    [Theory]
    [InlineData("**bold** plain", 3, "bold", "p")]
    [InlineData("**bold** plain", 10, "", "p")]
    [InlineData("# head", 3, "", "h1")]
    [InlineData("> - *x*", 5, "quote,unorderedList,italic", "p")]
    [InlineData("- [ ] t", 6, "taskList", "p")]
    [InlineData("```\nc\n```", 5, "codeBlock", null)]
    [InlineData("a\n\nb", 2, "", "p")]
    public void StateReflectsTheBlocksAndInlinesAtTheCaret(string text, int caret, string formats, string? block)
    {
        var state = new MarkdownEditorEngine(text).State(caret, caret);

        Assert.Equal(formats, string.Join(",", state.Formats!));
        Assert.Equal(block, state.Block);
    }

    [Fact]
    public void StateReportsMixedBlocksAsEmpty()
    {
        var state = new MarkdownEditorEngine("# h\n\np").State(0, 5);

        Assert.Equal("", state.Block);
    }

    [Theory]
    [InlineData("", "<p>\u200B</p>", "0-0:1")]
    [InlineData("a\n\n", "<p>a</p><p>\u200B</p>", "0-1:1 3-3:1")]
    [InlineData("a\n\n\n", "<p>a</p><p>\u200B</p><p>\u200B</p>", "0-1:1 3-3:1 4-4:1")]
    [InlineData("a\n\nb", "<p>a</p><p>b</p>", "0-1:1 3-4:1")]
    [InlineData("a\n\n\n\nb", "<p>a</p><p>\u200B</p><p>b</p>", "0-1:1 3-3:1 5-6:1")]
    [InlineData("\n\nb", "<p>\u200B</p><p>b</p>", "0-0:1 2-3:1")]
    [InlineData("- a\n- ", "<ul><li>a</li><li>\u200B</li></ul>", "2-3:1 6-6:1")]
    [InlineData("> a\n>\n> ", "<blockquote><p>a</p><p>\u200B</p></blockquote>", "2-3:1 8-8:1")]
    [InlineData("#", "<h1>\u200B</h1>", "1-1:1")]
    [InlineData("a", "<p>a</p>", "0-1:1")]
    [InlineData("---", "<p>\u200B</p><hr data-gap=\"0\"><p>\u200B</p>", "0-0:1 3-3:1")]
    [InlineData("a\n\n```\nc\n```", "<p>a</p><pre data-gap=\"2\"><code>c</code></pre><p>\u200B</p>", "0-1:1 7-8:1 12-12:1")]
    [InlineData("a\n\n```\nc\n```\n\n", "<p>a</p><pre data-gap=\"2\"><code>c</code></pre><p>\u200B</p>", "0-1:1 7-8:1 14-14:1")]
    [InlineData("| a |\n| - |", "<p>\u200B</p><table data-gap=\"0\"><thead><tr><th>a</th></tr></thead><tbody></tbody></table><p>\u200B</p>", "0-0:1 2-3:1 11-11:1")]
    public void RenderingPutsAPlaceholderOnEveryBlankLineThatIsNotASeparator(string text, string html, string segments)
    {
        var update = new MarkdownEditorEngine(text).Render(0, 0);

        Assert.Equal(html, update.Html);
        Assert.Equal(segments, string.Join(" ", System.Linq.Enumerable.Range(0, update.Segments!.Length / 3).Select(i => $"{update.Segments[i * 3]}-{update.Segments[i * 3 + 1]}:{update.Segments[i * 3 + 2]}")));
    }
}

public class MarkdownEditorEngineReviewRegressionTests
{
    [Theory]
    [InlineData("ab", 2, 2, "ab![alt](u.png)", 15)]
    [InlineData("ab", 1, 1, "a![alt](u.png)b", 14)]
    [InlineData("hello world", 6, 11, "hello ![alt](u.png)", 19)]
    public void InsertingAnImageKeepsTheSurroundingText(string text, int start, int end, string expected, int caret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(MarkdownEditorCommands.Image, start, end, "u.png", "alt");

        Assert.Equal(expected, engine.Text);
        Assert.Equal(caret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("![i](j)abcd", 9, "![i](j)**abcd**")]
    [InlineData("foo  \nbar", 8, "foo  \n**bar**")]
    public void BoldAtACaretAfterAnAtomFormatsTheWholeWord(string text, int caret, string expected)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.Command(MarkdownEditorCommands.Bold, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
    }

    [Theory]
    [InlineData("a **directly** b", 14, "Z", "a **directly**Z b")]
    [InlineData("a **directly** b", 12, "Z", "a **directlyZ** b")]
    [InlineData("a **directly** b", 4, "Z", "a **Zdirectly** b")]
    [InlineData("a **directly** b", 2, "Z", "a Z**directly** b")]
    public void TypingAtAMarkerDecidesTheSideByTheCaretPosition(string text, int caret, string typed, string expected)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.InsertText(caret, caret, typed, literal: true);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void EmptyingTheParagraphBetweenTwoListsKeepsThemApart()
    {
        var engine = new MarkdownEditorEngine("- one\n- two\n\nMiddle\n\n- three\n- four");

        engine.Delete(13, 19, selection: true);

        Assert.Equal("- one\n- two\n\n\n\n* three\n* four", engine.Text);
    }

    [Fact]
    public void DeletingTheSpaceAfterALinkKeepsTheCaretOutsideTheLink()
    {
        var engine = new MarkdownEditorEngine("a [links](https://x) and");

        var update = engine.Delete(21, 21);
        engine.InsertText(update!.SelectionStart, update.SelectionStart, "Q", literal: true);

        Assert.Equal("a [links](https://x)Qand", engine.Text);
    }

    [Theory]
    [InlineData("- item\n\n---\n\nEnd", 13, false, "- item\n\nEnd", 8)]
    [InlineData("- item\n\n---\n\nEnd", 6, true, "- item\n\nEnd", 6)]
    public void BackspaceAndDeleteNextToARuleRemoveIt(string text, int caret, bool forward, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(caret, caret, forward);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData("```js\nx\n```\n\nEnd", 13, false, 7)]
    [InlineData("End\n\n```js\nx\n```", 3, true, 11)]
    public void BackspaceAndDeleteNextToACodeBlockMoveTheCaretIntoIt(string text, int caret, bool forward, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Delete(caret, caret, forward);

        Assert.Equal(text, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceIntoAListLeavesTheFollowingBlocksInPlace()
    {
        var engine = new MarkdownEditorEngine("- a\n- b\n\nPara\n\n> quote\n\nlast");

        engine.Delete(9, 9);

        Assert.Equal("- a\n- bPara\n\n> quote\n\nlast", engine.Text);
    }

    [Theory]
    [InlineData("it round-trips to", 8, 14, MarkdownEditorCommands.Bold, "it round-**trips** to")]
    [InlineData("it round-trips to", 3, 9, MarkdownEditorCommands.Italic, "it *round*-trips to")]
    [InlineData("say (hi) now", 4, 8, MarkdownEditorCommands.Bold, "say **(hi)** now")]
    public void MarksNextToPunctuationLeaveThePunctuationOutside(string text, int start, int end, string command, string expected)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.Command(command, start, end, null, null);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void BackspaceInAnEmptyCodeBlockRemovesIt()
    {
        var engine = new MarkdownEditorEngine("> A quote line.\n\n```js\n```\n\n---");

        var update = engine.Delete(23, 23);

        Assert.Equal("> A quote line.\n\n---", engine.Text);
        Assert.Equal(15, update!.SelectionStart);
    }

    [Fact]
    public void BackspaceAtTheStartOfACodeBlockWithContentDoesNothing()
    {
        var engine = new MarkdownEditorEngine("End\n\n```js\nx\n```");

        Assert.Null(engine.Delete(11, 11));
    }

    [Fact]
    public void DeletingAcrossTwoListItemsKeepsTheNestedListWithTheMergedItem()
    {
        var engine = new MarkdownEditorEngine("1. First item\n2. Second item\n   - nested one\n   - nested two\n3. Third item");

        engine.Delete(11, 24, selection: true);

        Assert.Equal("1. First ititem\n   - nested one\n   - nested two\n2. Third item", engine.Text);
    }

    [Fact]
    public void BoldAfterATrailingSpaceDoesNotEatTheSpace()
    {
        var engine = new MarkdownEditorEngine("alpha *gamma* ");

        var update = engine.Command(MarkdownEditorCommands.Bold, 14, 14, null, null);

        Assert.Null(update);
        engine.InsertText(14, 14, "X", literal: true);
        Assert.Equal("alpha *gamma* X", engine.Text);
    }

    [Fact]
    public void QuoteOnASelectionInsideAQuoteUnquotesIt()
    {
        var engine = new MarkdownEditorEngine("> Quoted one.\n>\n> Quoted two.");

        engine.Command(MarkdownEditorCommands.Quote, 4, 20, null, null);

        Assert.Equal("Quoted one.\n\nQuoted two.", engine.Text);
    }

    [Fact]
    public void ADelimiterLookingLineIsEscapedOnlyWhenItWouldFormATable()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| --- |\n| 1 | 2 | 3 |");

        engine.InsertText(7, 7, "Y", literal: true);

        Assert.Equal("| a | bY |\n| --- |\n| 1 | 2 | 3 |", engine.Text);
    }

    [Fact]
    public void TabInTheEmptyLastRowAppendsARow()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| --- | --- |\n|  |  |");

        engine.AppendRow(24);

        Assert.Equal("| a | b |\n| --- | --- |\n|  |  |\n|  |  |", engine.Text);
    }

    [Fact]
    public void LinkingASelectionAcrossParagraphsLinksEachPart()
    {
        var engine = new MarkdownEditorEngine("one\n\ntwo");

        engine.Command(MarkdownEditorCommands.Link, 1, 7, "http://x", "L");

        Assert.Equal("o[ne](http://x)\n\n[tw](http://x)o", engine.Text);
    }

    [Theory]
    [InlineData(false, 3, "ab", 1)]
    [InlineData(true, 1, "ab", 1)]
    public void DeletingNextToAnEmojiRemovesTheWholeCodePoint(bool forward, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine("a\U0001F600b");

        var update = engine.Delete(caret, caret, forward);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void EditingNextToAnEmailAutolinkKeepsItAnAutolink()
    {
        var engine = new MarkdownEditorEngine("<foo@bar.com> x");

        engine.InsertText(15, 15, "y", literal: true);

        Assert.Equal("<foo@bar.com> xy", engine.Text);
    }

    [Fact]
    public void ALinkDestinationWithALineBreakIsEncoded()
    {
        var engine = new MarkdownEditorEngine("abc");

        engine.Command(MarkdownEditorCommands.Link, 0, 3, "a\nb", null);

        Assert.Equal("[abc](a%0Ab)", engine.Text);
    }
}

public class MarkdownEditorEngineTableTests
{
    private const string Table = "| a | b |\n| - | - |\n| c | d |";

    [Theory]
    [InlineData("", 0, "2x3", "|  |  |  |\n| --- | --- | --- |\n|  |  |  |", 2)]
    [InlineData("a\n\nb", 1, "2x2", "a\n\n|  |  |\n| --- | --- |\n|  |  |\n\nb", 5)]
    [InlineData("a\n\n\n\nb", 3, "1x1", "a\n\n|  |\n| --- |\n\nb", 5)]
    [InlineData("ab", 1, null, "ab\n\n|  |  |  |\n| --- | --- | --- |\n|  |  |  |\n|  |  |  |", 6)]
    public void InsertTableAddsAnEmptyTableWithAHeaderRow(string text, int caret, string? size, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(text);

        var update = engine.Command(MarkdownEditorCommands.InsertTable, caret, caret, size, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Theory]
    [InlineData(MarkdownEditorCommands.TableRowAfter, 22, "| a | b |\n| - | - |\n| c | d |\n|  |  |", 32)]
    [InlineData(MarkdownEditorCommands.TableRowBefore, 22, "| a | b |\n| - | - |\n|  |  |\n| c | d |", 22)]
    [InlineData(MarkdownEditorCommands.TableRowBefore, 2, "| a | b |\n| - | - |\n|  |  |\n| c | d |", 22)]
    [InlineData(MarkdownEditorCommands.TableColumnAfter, 2, "| a |  | b |\n| --- | --- | --- |\n| c |  | d |", 6)]
    [InlineData(MarkdownEditorCommands.TableColumnBefore, 6, "| a |  | b |\n| --- | --- | --- |\n| c |  | d |", 6)]
    [InlineData(MarkdownEditorCommands.TableDeleteRow, 22, "| a | b |\n| - | - |", 2)]
    [InlineData(MarkdownEditorCommands.TableDeleteColumn, 6, "| a |\n| --- |\n| c |", 2)]
    public void TableCommandsEditRowsAndColumnsAtTheCaret(string command, int caret, string expected, int expectedCaret)
    {
        var engine = new MarkdownEditorEngine(Table);

        var update = engine.Command(command, caret, caret, null, null);

        Assert.Equal(expected, engine.Text);
        Assert.Equal(expectedCaret, update!.SelectionStart);
    }

    [Fact]
    public void TheHeaderRowCannotBeDeleted()
    {
        var engine = new MarkdownEditorEngine(Table);

        Assert.Null(engine.Command(MarkdownEditorCommands.TableDeleteRow, 2, 2, null, null));
        Assert.Equal(Table, engine.Text);
    }

    [Fact]
    public void DeletingTheLastColumnDeletesTheTable()
    {
        var engine = new MarkdownEditorEngine("| a |\n| - |\n| c |");

        engine.Command(MarkdownEditorCommands.TableDeleteColumn, 2, 2, null, null);

        Assert.Equal("", engine.Text);
    }

    [Fact]
    public void DeletingATableMovesTheCaretToTheNextBlock()
    {
        var engine = new MarkdownEditorEngine("x\n\n" + Table + "\n\ny");

        var update = engine.Command(MarkdownEditorCommands.TableDelete, 25, 25, null, null);

        Assert.Equal("x\n\ny", engine.Text);
        Assert.Equal(3, update!.SelectionStart);
    }

    [Fact]
    public void DeletingTheOnlyTableLeavesAnEmptyParagraph()
    {
        var engine = new MarkdownEditorEngine(Table);

        var update = engine.Command(MarkdownEditorCommands.TableDelete, 2, 2, null, null);

        Assert.Equal("", engine.Text);
        Assert.Equal(0, update!.SelectionStart);
    }

    [Fact]
    public void DeletingATableBeforeACodeBlockMovesTheCaretToThePreviousBlock()
    {
        var engine = new MarkdownEditorEngine("x\n\n" + Table + "\n\n```\nc\n```");

        var update = engine.Command(MarkdownEditorCommands.TableDelete, 25, 25, null, null);

        Assert.Equal("x\n\n```\nc\n```", engine.Text);
        Assert.Equal(1, update!.SelectionStart);
    }

    [Theory]
    [InlineData(Table, 6, "right", "| a | b |\n| --- | --: |\n| c | d |")]
    [InlineData("| a | b |\n| :- | -: |\n| c | d |", 2, "center", "| a | b |\n| :-: | --: |\n| c | d |")]
    [InlineData("| a | b |\n| :- | -: |\n| c | d |", 2, "none", "| a | b |\n| --- | --: |\n| c | d |")]
    [InlineData(Table, 22, "left", "| a | b |\n| :-- | --- |\n| c | d |")]
    public void TableAlignChangesTheColumnAtTheCaret(string text, int caret, string alignment, string expected)
    {
        var engine = new MarkdownEditorEngine(text);

        engine.Command(MarkdownEditorCommands.TableAlign, caret, caret, alignment, null);

        Assert.Equal(expected, engine.Text);
    }

    [Fact]
    public void TableCommandsOutsideATableDoNothing()
    {
        var engine = new MarkdownEditorEngine("text");

        Assert.Null(engine.Command(MarkdownEditorCommands.TableRowAfter, 2, 2, null, null));
        Assert.Null(engine.Command(MarkdownEditorCommands.TableAlign, 2, 2, "left", null));
    }

    [Fact]
    public void StateReportsTheTablePositionAndAlignment()
    {
        var engine = new MarkdownEditorEngine("| a | b |\n| :-- | --: |\n| c | d |");

        var state = engine.State(30, 30);

        Assert.Equal(1, state.TableRow);
        Assert.Equal(1, state.TableColumn);
        Assert.Equal(2, state.TableRows);
        Assert.Equal(2, state.TableColumns);
        Assert.Equal("right", state.TableAlignment);

        var outside = new MarkdownEditorEngine("text").State(1, 1);

        Assert.Equal(-1, outside.TableRow);
        Assert.Equal(-1, outside.TableColumn);
    }
}
