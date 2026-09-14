using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Radzen.Blazor;
using Radzen.Documents.Markdown;
using Radzen.Documents.Markdown.Tests;
using Xunit;

namespace Radzen.Blazor.Tests;

public class MarkdownEditorEngineFuzzTests
{
    private static readonly string[] Seeds =
    [
        "",
        "plain text",
        "# Hello, Markdown\n\nEdit this text **directly** - it round-trips to *markdown*. Toggle ~~WYSIWYG~~ **Design** and **Source** mode.\n\n- [x] A task list item\n- [ ] Bold, italic and `code` via the toolbar\n- Undo and redo\n\n> Blockquotes, [links](https://blazor.radzen.com) and tables work too:\n\n| Feature | Mode |\n| --- | --- |\n| WYSIWYG | Design |\n| Raw markdown | Source |\n\n```csharp\nvar editor = new RadzenMarkdownEditor();\n```",
        "1. one\n2. two\n   - nested a\n   - nested b\n3. three\n\nafter",
        "> quote line\n>\n> - item in quote\n> - second\n\n---\n\nSetext\n======\n\ntail",
        "a\n\n\n\nb\n\n| h1 | h2 | h3 |\n| :- | :-: | -: |\n| 1 | 2 | 3 |\n\n    indented code\n\n<div>html</div>\n\n[ref]: https://example.com",
        "Line one  \nline two\\\nline three\n\n- [ ] task\n\n  loose paragraph\n\n- [x] done",
    ];

    private static readonly string[] Snippets =
    [
        "x", " ", "ab", "*", "_", "#", "1.", "-", "`", "[", "]", "|", "\\", "> ", "\n", "\n\n", "**bold**", "- item", "é", "1) ", "~~s~~", "<b>", "&amp;",
    ];

    private static readonly string[] Commands =
    [
        MarkdownEditorCommands.Bold, MarkdownEditorCommands.Italic, MarkdownEditorCommands.Strikethrough, MarkdownEditorCommands.Code,
        MarkdownEditorCommands.CodeBlock, MarkdownEditorCommands.Quote, MarkdownEditorCommands.UnorderedList, MarkdownEditorCommands.OrderedList,
        MarkdownEditorCommands.TaskList, MarkdownEditorCommands.HorizontalRule, MarkdownEditorCommands.FormatBlock, MarkdownEditorCommands.Link,
        MarkdownEditorCommands.Image, MarkdownEditorCommands.InsertTable, MarkdownEditorCommands.TableRowBefore, MarkdownEditorCommands.TableRowAfter,
        MarkdownEditorCommands.TableColumnBefore, MarkdownEditorCommands.TableColumnAfter, MarkdownEditorCommands.TableDeleteRow,
        MarkdownEditorCommands.TableDeleteColumn, MarkdownEditorCommands.TableDelete, MarkdownEditorCommands.TableAlign,
    ];

    public static IEnumerable<object[]> Runs()
    {
        for (var seed = 0; seed < Seeds.Length; seed++)
        {
            for (var run = 0; run < 100; run++)
            {
                yield return [seed, run];
            }
        }
    }

    [Theory]
    [MemberData(nameof(Runs))]
    public void RandomEditSequencesKeepTheEngineConsistent(int seed, int run)
    {
        var random = new Random(seed * 1000 + run);
        var engine = new MarkdownEditorEngine(Seeds[seed]);
        var log = new StringBuilder();
        var history = new Stack<string>();
        var caret = 0;

        for (var step = 0; step < 40; step++)
        {
            var text = engine.Text;
            var start = random.Next(text.Length + 1);
            var end = random.Next(4) == 0 ? Math.Min(text.Length, start + random.Next(12)) : start;
            var choice = random.Next(10);
            var description = string.Empty;
            MarkdownEditorUpdate? update = null;

            try
            {
                switch (choice)
                {
                    case 0 or 1 or 2:
                        var snippet = Snippets[random.Next(Snippets.Length)];
                        var literal = random.Next(4) != 0;
                        description = $"InsertText({start}, {end}, {Show(snippet)}, literal: {literal})";
                        update = engine.InsertText(start, end, snippet, literal, paragraphs: snippet.Contains('\n', StringComparison.Ordinal), selection: end > start);
                        break;
                    case 3 or 4:
                        var forward = random.Next(2) == 0;
                        var at = end > start ? end : Math.Clamp(caret, 0, text.Length);
                        description = $"Delete({(end > start ? start : at)}, {at}, forward: {forward})";
                        update = engine.Delete(end > start ? start : at, at, forward, selection: end > start);
                        break;
                    case 5:
                        description = $"InsertParagraph({start})";
                        update = engine.InsertParagraph(start, start);
                        break;
                    case 6:
                        description = $"InsertLineBreak({start})";
                        update = engine.InsertLineBreak(start, start);
                        break;
                    case 7:
                        var outdent = random.Next(2) == 0;
                        description = $"Indent({start}, {end}, outdent: {outdent})";
                        update = engine.Indent(start, end, outdent);
                        break;
                    default:
                        var command = Commands[random.Next(Commands.Length)];
                        var value = command switch
                        {
                            MarkdownEditorCommands.FormatBlock => new[] { "p", "h1", "h2", "h3" }[random.Next(4)],
                            MarkdownEditorCommands.Link or MarkdownEditorCommands.Image => "https://example.com/a b",
                            MarkdownEditorCommands.InsertTable => $"{random.Next(1, 4)}x{random.Next(1, 4)}",
                            MarkdownEditorCommands.TableAlign => new[] { "left", "center", "right", "none" }[random.Next(4)],
                            _ => null
                        };
                        description = $"Command({command}, {start}, {end}, {Show(value)})";
                        update = engine.Command(command, start, end, value, value != null && command is MarkdownEditorCommands.Link or MarkdownEditorCommands.Image ? "label" : null);
                        break;
                }
            }
            catch (Exception exception)
            {
                Assert.True(false, $"{description} threw {exception.GetType().Name}: {exception.Message}\n{log}\nText before: {Show(text)}\n{exception.StackTrace}");
            }

            log.Append(description).Append(" -> ").Append(Show(engine.Text)).Append('\n');

            if (update == null)
            {
                Assert.True(engine.Text == text, $"{description} returned null but changed the text\n{log}");
                continue;
            }

            if (engine.Text == text && history.Count > 0)
            {
                engine.Undo();
                Assert.True(engine.Text == history.Peek(), $"{description} changed nothing but undo restored {Show(engine.Text)} instead of {Show(history.Peek())}\n{log}");
                engine.Redo();
                Assert.True(engine.Text == text, $"{description} changed nothing but redo produced {Show(engine.Text)} instead of {Show(text)}\n{log}");
            }

            if (engine.Text != text)
            {
                history.Push(text);
                var after = engine.Text;
                engine.Undo();
                Assert.True(engine.Text == text, $"{description}: undo restored {Show(engine.Text)} instead of {Show(text)}\n{log}");
                engine.Redo();
                Assert.True(engine.Text == after, $"{description}: redo produced {Show(engine.Text)} instead of {Show(after)}\n{log}");
            }

            caret = update.SelectionStart;
            Assert.True(update.SelectionStart >= 0 && update.SelectionStart <= update.SelectionEnd && update.SelectionEnd <= engine.Text.Length, $"{description} produced selection {update.SelectionStart}-{update.SelectionEnd} outside 0-{engine.Text.Length}\n{log}");
            AssertStable(engine.Text, description, log);
        }

        while (history.Count > 0)
        {
            var expected = history.Pop();
            var undo = engine.Undo();
            Assert.True(undo != null, $"Undo returned null with {history.Count + 1} states left\n{log}");
            Assert.True(engine.Text == expected, $"Undo restored {Show(engine.Text)} instead of {Show(expected)}\n{log}");
        }

        Assert.Null(engine.Undo());
    }

    private static void AssertStable(string text, string description, StringBuilder log)
    {
        var rewritten = MarkdownWriter.Write(MarkdownParser.Parse(text), text);
        var html = Canonical.Html(text);
        var rewrittenHtml = Canonical.Html(rewritten);
        Assert.True(html == rewrittenHtml, $"After {description} the text does not survive a rewrite\nText: {Show(text)}\nRewritten: {Show(rewritten)}\nHtml: {html}\nRewritten html: {rewrittenHtml}\n{log}");
        Assert.True(!text.Contains('​', StringComparison.Ordinal), $"After {description} the text contains a zero width space placeholder\n{log}");
    }

    private static string Show(string? value) => value == null ? "null" : "\"" + value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\n", "\\n", StringComparison.Ordinal) + "\"";
}
