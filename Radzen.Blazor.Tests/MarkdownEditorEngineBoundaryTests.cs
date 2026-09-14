using System;
using Xunit;

namespace Radzen.Blazor.Tests;

public class MarkdownEditorEngineBoundaryTests
{
    private static string Run(string from, Func<MarkdownEditorEngine, int, MarkdownEditorUpdate?> command)
    {
        var caret = from.IndexOf('|', StringComparison.Ordinal);
        var engine = new MarkdownEditorEngine(from.Remove(caret, 1));
        var update = command(engine, caret);
        var position = update?.SelectionStart ?? caret;

        return engine.Text.Insert(position, "|");
    }

    [Theory]
    [InlineData("> one\n>\n> |", "> one\n\n|")]
    [InlineData("+ one|", "+ one\n+ |")]
    [InlineData("2)  one|", "2)  one\n3)  |")]
    [InlineData("> - one|", "> - one\n> - |")]
    [InlineData(" - |", "|")]
    [InlineData("> 1. one\n>\n> 2. two|", "> 1. one\n>\n> 2. two\n>\n> 3. |")]
    [InlineData("1. one|\n2. two", "1. one\n2. |\n3. two")]
    [InlineData("1. a\n\n   1. b\n\n   2. |", "1. a\n\n   1. b\n\n2. |")]
    [InlineData("1. a\n\n   1. b\n\n   2. |\n\n2. d", "1. a\n\n   1. b\n\n2. |\n\n3. d")]
    [InlineData("- [ ] item 1\n  - [ ] item 1.1|", "- [ ] item 1\n  - [ ] item 1.1\n  - [ ] |")]
    [InlineData("- [ ] item 1\n  - [ ] item 1.1\n    - [ ] item 1.1.1|", "- [ ] item 1\n  - [ ] item 1.1\n    - [ ] item 1.1.1\n    - [ ] |")]
    public void EnterContinuesMarkupLikeCodeMirror(string from, string to)
    {
        Assert.Equal(to, Run(from, (engine, caret) => engine.InsertParagraph(caret, caret)));
    }

    [Theory]
    [InlineData("> |", "|")]
    [InlineData("> > |", "> |")]
    [InlineData(" - |", "|")]
    public void BackspaceDeletesMarkupLikeCodeMirror(string from, string to)
    {
        Assert.Equal(to, Run(from, (engine, caret) => engine.Delete(caret, caret, forward: false)));
    }

    [Theory]
    [InlineData("a\n\n> > |", "a\n\n> |")]
    [InlineData("- a\n  > > |", "- a\n  > |")]
    [InlineData("- one\n-    |", "- one\n\n|")]
    [InlineData("> - one\n> -    |", "> - one\n>\n>|")]
    public void BackspaceAfterAnEmptyContainerUnwrapsIt(string from, string to)
    {
        Assert.Equal(to, Run(from, (engine, caret) => engine.Delete(caret, caret, forward: false)));
    }

    [Theory]
    [InlineData("> |", "|")]
    [InlineData("- > |", "- |")]
    [InlineData("> a\n>\n> |", "> a\n\n|")]
    public void EnterOnTheEmptyLastParagraphOfAQuoteLeavesTheQuote(string from, string to)
    {
        Assert.Equal(to, Run(from, (engine, caret) => engine.InsertParagraph(caret, caret)));
    }

    [Theory]
    [InlineData("para|", "para  \nx|")]
    [InlineData("> quote|", "> quote  \n> x|")]
    [InlineData("- item|", "- item  \n  x|")]
    [InlineData("**b|**", "**b**  \nx|")]
    [InlineData("para|\n\nnext", "para  \nx|\n\nnext")]
    public void TypingAfterATrailingHardBreakContinuesTheParagraph(string from, string to)
    {
        Assert.Equal(to, Run(from, (engine, caret) =>
        {
            var position = engine.InsertLineBreak(caret, caret)!.SelectionStart;
            return engine.InsertText(position, position, "x", literal: true);
        }));
    }

    [Fact]
    public void TypingAfterATrailingBackslashBreakContinuesTheParagraph()
    {
        Assert.Equal("a\\\nx|", Run("a\\\n|", (engine, caret) => engine.InsertText(caret, caret, "x", literal: true)));
    }
}
