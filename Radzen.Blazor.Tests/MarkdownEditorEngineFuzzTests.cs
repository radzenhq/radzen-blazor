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
        var engine = new MarkdownEditorEngine(Seeds[seed]) { RenderHtml = true };
        var log = new StringBuilder();
        var history = new Stack<string>();
        var caret = 0;

        for (var step = 0; step < 40; step++)
        {
            var text = engine.Text;
            var size = engine.Length;
            var start = random.Next(size + 1);
            var end = random.Next(4) == 0 ? Math.Min(size, start + random.Next(12)) : start;
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
                        update = engine.InsertText(start, end, snippet, literal, selection: end > start);
                        break;
                    case 3 or 4:
                        var forward = random.Next(2) == 0;
                        var at = end > start ? end : Math.Clamp(caret, 0, size);
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

            caret = update.SelectionStart;
            Assert.True(update.SelectionStart >= 0 && update.SelectionStart <= update.SelectionEnd && update.SelectionEnd <= engine.Length, $"{description} produced selection {update.SelectionStart}-{update.SelectionEnd} outside 0-{engine.Length}\n{log}");
            AssertStable(engine.Text, description, log);
            AssertReachable(update, description, log, engine);

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

    private static string Dump(MarkdownEditorEngine engine)
    {
        var document = typeof(MarkdownEditorEngine).GetField("document", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(engine) as Document;
        var dump = new StringBuilder();

        void Walk(INode node, int depth)
        {
            var label = node switch
            {
                Text text => $"Text[{Show(text.Value)}] {text.SourceStart}-{text.SourceEnd}",
                Inline inline => $"{inline.GetType().Name} {inline.SourceStart}-{inline.SourceEnd}",
                Block block => $"{block.GetType().Name} {block.SourceStart}-{block.SourceEnd}" + (block.Pristine ? " pristine" : "") + (block is Paragraph { Virtual: true } ? " virtual" : ""),
                TableCell => "cell",
                _ => node.GetType().Name
            };
            dump.Append(' ', depth * 2).AppendLine(label);
            IEnumerable<INode> children = node switch
            {
                Table table => table.Rows.SelectMany(row => row.Cells),
                BlockContainer container => container.Children,
                IBlockInlineContainer content => content.Children,
                InlineContainer inlineContainer => inlineContainer.Children,
                _ => []
            };

            foreach (var child in children)
            {
                Walk(child, depth + 1);
            }
        }

        if (document != null)
        {
            Walk(document, 0);
        }

        return dump.ToString();
    }

    private static void AssertReachable(MarkdownEditorUpdate update, string description, StringBuilder log, MarkdownEditorEngine engine)
    {
        if (update.Segments == null)
        {
            return;
        }

        var segments = update.Segments.Chunk(3).Select(chunk => (Start: chunk[0], End: chunk[1], Length: chunk[2])).ToList();
        var position = 0;

        foreach (var segment in segments)
        {
            var linear = segment.End - segment.Start == segment.Length;
            Assert.True(segment.Start >= position && (linear || segment.Start == segment.End || segment.End == segment.Start + 1), $"After {description} the segment {segment.Start}-{segment.End}:{segment.Length} is out of order\nSegments: {string.Join(" ", segments.Select(s => $"{s.Start}-{s.End}:{s.Length}"))}\nModel:\n{Dump(engine)}{log}");
            position = segment.End;
        }

        foreach (var offset in new[] { update.SelectionStart, update.SelectionEnd })
        {
            var covered = segments.Any(segment => segment.Start <= offset && offset <= segment.End);
            Assert.True(covered || engine.HostAt(offset) is ThematicBreak, $"After {description} the caret {offset} has no place in the rendered html\nSegments: {string.Join(" ", segments.Select(s => $"{s.Start}-{s.End}:{s.Length}"))}\nModel:\n{Dump(engine)}{log}");
        }
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
