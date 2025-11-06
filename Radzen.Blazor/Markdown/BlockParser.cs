using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

#nullable enable

class BlockParser
{
    private static readonly string tagName = @"[A-Za-z][A-Za-z0-9-]*";
    private static readonly string attributeName = @"[a-zA-Z_:][a-zA-Z0-9:._-]*";
    private static readonly string unquotedValue = @"[^""'=<>`\x00-\x20]+";
    private static readonly string singleQuotedValue = @"'[^']*'";
    private static readonly string doubleQuotedValue = @"""[^""]*""";
    private static readonly string attributeValue = @$"(?:{unquotedValue}|{singleQuotedValue}|{doubleQuotedValue})";
    private static readonly string attributeValueSpec = @$"(?:\s*=\s*{attributeValue})";
    private static readonly string attribute = @$"(?:\s+{attributeName}{attributeValueSpec}?)";

    private static readonly string OpenTag = @$"<{tagName}{attribute}*\s*/?>";
    private static readonly string CloseTag = @$"</{tagName}\s*[>]";
    private static readonly string htmlComment = @"<!-->|<!--->|<!--[\s\S]*?-->";
    private static readonly string processingInstruction = @"<\?[ \s\S]*?\?>";
    private static readonly string declaration = @$"<![A-Za-z]+[^>]*>";
    private static readonly string cdata = @"<!\[CDATA\[[\s\S]*?\]\]>";

    public static readonly Regex HtmlRegex = new(@$"^(?:{OpenTag}|{CloseTag}|{htmlComment}|{processingInstruction}|{declaration}|{cdata})");

    private BlockParser()
    {
        Tip = document;

        OldTip = document;

        lastMatchedContainer = document;
    }

    public static Document Parse(string markdown)
    {
        var parser = new BlockParser();

        var document = parser.ParseBlocks(markdown);

        parser.ParseInlines(document);

        return document;
    }

    public static readonly Regex NewLineRegex = new(@"\r\n|\r|\n");

    private readonly Document document = new();

    private void ParseInlines(Document document)
    {
        var visitor = new InlineVisitor(linkReferences);
        document.Accept(visitor);
    }

    public char Peek()
    {
        return CurrentLine.Peek(Offset);
    }

    public char PeekNonSpace(int offset = 0)
    {
        return CurrentLine.Peek(NextNonSpace + offset);
    }

    public void AdvanceOffset(int count, bool columns)
    {
        var currentLine = CurrentLine;
        char c;

        while (count > 0 && Offset < currentLine.Length != default)
        {
            c = currentLine[Offset];

            if (c == '\t')
            {
                var charsToTab = 4 - (Column % 4);

                if (columns)
                {
                    PartiallyConsumedTab = charsToTab > count;
                    var charsToAdvance = charsToTab > count ? count : charsToTab;
                    Column += charsToAdvance;
                    Offset += PartiallyConsumedTab ? 0 : 1;
                    count -= charsToAdvance;
                }
                else
                {
                    PartiallyConsumedTab = false;
                    Column += charsToTab;
                    Offset += 1;
                    count -= 1;
                }
            }
            else
            {
                PartiallyConsumedTab = false;
                Offset += 1;
                Column += 1;
                count -= 1;
            }
        }
    }

    private Document ParseBlocks(string markdown)
    {
        LineNumber = 0;

        lastMatchedContainer = document;

        var lines = NewLineRegex.Split(markdown);

        var length = lines.Length;

        if (markdown.EndsWith(InlineParser.LineFeed))
        {
            length--;
        }

        for (var index = 0; index < length; index++)
        {
            IncorporateLine(lines[index]);
        }

        while (Tip != null)
        {
            Close(Tip, length);
        }

        return document;
    }

    private void IncorporateLine(string line)
    {
        Offset = 0;
        Column = 0;
        Blank = false;
        PartiallyConsumedTab = false;
        LineNumber++;

        Block container = document;
        OldTip = Tip;
        Block? tail;
        var allMatched = true;
        CurrentLine = line;

        while ((tail = container.LastChild) != null && tail.Open)
        {
            container = tail;

            FindNextNonSpace();

            switch (container.Matches(this))
            {
                case BlockMatch.Match: // we've matched, keep going
                    break;
                case BlockMatch.Skip: // we've failed to match a block
                    allMatched = false;
                    break;
                case BlockMatch.Break: // we've hit end of line for fenced code close and can return
                    return;
                default: 
                    throw new InvalidOperationException("Invalid continue result");
            }

            if (!allMatched)
            {
                container = container.Parent;
                break;
            }
        }

        AllClosed = container == OldTip;
        lastMatchedContainer = container;

        var matchedLeaf = container is not (Paragraph or Table) && container is Leaf;

        while (!matchedLeaf)
        {
            FindNextNonSpace();

            int blockIndex;

            for (blockIndex = 0; blockIndex < blockStarts.Length; blockIndex++)
            {
                var blockStart = blockStarts[blockIndex];

                var result = blockStart(this, container);

                if (result == BlockStart.Container)
                {
                    container = Tip;
                    break;
                }
                else if (result == BlockStart.Leaf)
                {
                    container = Tip;
                    matchedLeaf = true;
                    break;
                }
            }

            if (blockIndex == blockStarts.Length) 
            {
                AdvanceNextNonSpace();
                break;
            }
        }

        // What remains at the offset is a text line.  Add the text to the
        // appropriate container.

        if (!AllClosed && !Blank && this.Tip is Paragraph or Table) 
        {
            // lazy paragraph continuation
            if (Tip is Paragraph paragraph)
            {
                paragraph.AddLine(this);
            }
            else if (Tip is Table table)
            {
                table.AddLine(this);
            }
        }
        else 
        {
            // not a lazy continuation

            // finalize any blocks not matched
            CloseUnmatchedBlocks();

            if (container is Leaf leaf)
            {
                leaf.AddLine(this);

                if (container is HtmlBlock block && block.Type >= 1 && block.Type <= 5 && HtmlBlockCloseRegex[block.Type].IsMatch(line[Offset..]))
                {
                    LastLineLength = line.Length;
                    Close(container, LineNumber);
                }
            }
            else if (Offset < line.Length && !Blank)
            {
                var paragraph = AddChild<Paragraph>(Offset);
                AdvanceNextNonSpace();
                paragraph.AddLine(this);
            }
        }
        LastLineLength = line.Length;
    }

    public int LastLineLength { get; set; }

    public void Close(Block block, int lineNumber)
    {
        var above = block.Parent;
        block.Range.End.Line = lineNumber;
        block.Range.End.Column = LastLineLength;
        block.Close(this);
        Tip = above;
    }

    public T AddChild<T>(int offset) where T : Block, new()
    {
        var node = new T();

        AddChild(node, offset);

        return node;
    }

    public void AddChild(Block node, int offset)
    {
        while (Tip is not BlockContainer container || !container.CanContain(node))
        {
            Close(Tip, LineNumber - 1);
        }

        if (Tip is BlockContainer parent)
        {
            parent.Add(node);
        }

        var columnNumber = offset + 1; // offset 0 = column 1

        node.Range.Start.Line = LineNumber;
        node.Range.Start.Column = columnNumber;

        Tip = node;
    }

    public void CloseUnmatchedBlocks()
    {
        if (!AllClosed)
        {
            while (OldTip != lastMatchedContainer)
            {
                var parent = OldTip.Parent;
                Close(OldTip, LineNumber - 1);
                OldTip = parent;
            }

            AllClosed = true;
        }
    }

    public void AdvanceNextNonSpace()
    {
        Offset = NextNonSpace;
        Column = NextNonSpaceColumn;
        PartiallyConsumedTab = false;
    }

    private static readonly Func<BlockParser, Block, BlockStart>[] blockStarts =
    [
        BlockQuote.Start,
        AtxHeading.Start,
        FencedCodeBlock.Start,
        HtmlBlock.Start,
        SetExtHeading.Start,
        ThematicBreak.Start,
        ListItem.Start,
        IndentedCodeBlock.Start,
        Table.Start,
    ];

    public bool AllClosed { get; private set; }

    private Block lastMatchedContainer;

    public void FindNextNonSpace()
    {
        var currentLine = CurrentLine;
        var i = Offset;
        var cols = Column;
        char c = default;

        while (i < currentLine.Length) 
        {
            c = currentLine[i];

            if (c == ' ')
            {
                i++;
                cols++;
            }
            else if (c == '\t')
            {
                i++;
                cols += 4 - (cols % 4);
            }
            else
            {
                break;
            }
        }

        Blank = c == '\n' || c == '\r' || i == currentLine.Length;
        NextNonSpace = i;
        NextNonSpaceColumn = cols;
        Indent = NextNonSpaceColumn - Column;
        Indented = Indent >= CodeIndent;
    }

    public const int CodeIndent = 4;

    public int Indent { get; private set; }
    public bool Indented { get; private set; }
    public int NextNonSpaceColumn { get; private set; }

    public int NextNonSpace { get; private set; }

    public bool Blank { get; private set; }
    public bool PartiallyConsumedTab { get; private set; }
    public Block Tip { get; set; }
    public Block OldTip { get; private set; }
    public string CurrentLine { get; private set; } = string.Empty;
    public int Offset { get; set; }
    public int Column { get; set; }
    public int LineNumber { get; private set; }

    private static readonly Regex LinkReferenceRegex = new(@"^[ \t]{0,3}\[");

    private readonly Dictionary<string, LinkReference> linkReferences = [];

    // https://spec.commonmark.org/0.31.2/#html-blocks
    internal static readonly Regex[] HtmlBlockOpenRegex = [
        new (@"."), // dummy for 1 based indexing
        new (@"^<(?:script|pre|textarea|style)(?:\s|>|$)", RegexOptions.IgnoreCase),
        new (@"^<!--"),
        new (@"^<[?]"),
        new (@"^<![A-Za-z]"),
        new (@"^<!\[CDATA\["),
        new (@"^<[/]?(?:address|article|aside|base|basefont|blockquote|body|caption|center|col|colgroup|dd|details|dialog|dir|div|dl|dt|fieldset|figcaption|figure|footer|form|frame|frameset|h[123456]|head|header|hr|html|iframe|legend|li|link|main|menu|menuitem|nav|noframes|ol|optgroup|option|p|param|section|search|summary|table|tbody|td|tfoot|th|thead|title|tr|track|ul)(?:\s|[/]?[>]|$)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new (@$"^(?:{OpenTag}|{CloseTag})\s*$", RegexOptions.IgnoreCase)
    ];

    private static readonly Regex[] HtmlBlockCloseRegex = [
        new (@"."), // dummy for 1 based indexing
        new (@"</(?:script|pre|textarea|style)>", RegexOptions.IgnoreCase),
        new (@"-->"),
        new (@"\?>"),
        new (@">"),
        new (@"\]\]>")
    ];


    public bool TryParseLinkReference(string markdown, out int newIndex)
    {
        newIndex = 0;

        if (!LinkReferenceRegex.IsMatch(markdown))
        {
            return false;
        }

        var position = 0;

        while (position < markdown.Length - 1 && (markdown[position] is not InlineParser.CloseBracket || (position > 0 && markdown[position - 1] is InlineParser.Backslash)))
        {
            position++;
        }

        if (position >= markdown.Length - 1)
        {
            return false;
        }

        position++;

        if (position >= markdown.Length || markdown[position] is not InlineParser.Colon)
        {
            return false;
        }

        var colonIndex = position;
        var closeIndex = colonIndex - 1;
        var openIndex = 0;

        while (openIndex < closeIndex && markdown[openIndex] is not InlineParser.OpenBracket)
        {
            openIndex++;
        }

        if (openIndex == closeIndex)
        {
            return false;
        }

        var id = new StringBuilder();

        for (var index = openIndex + 1; index < closeIndex; index++)
        {
            var next = index < closeIndex - 1 ? markdown[index + 1] : default;

            if (markdown[index] is not InlineParser.Backslash || !next.IsPunctuation())
            {
                id.Append(markdown[index]);
            }
        }

        if (!InlineParser.TryParseDestinationAndTitle(markdown, colonIndex + 1, out var destination, out var title, out position))
        {
            return false;
        }
        
        var link = new LinkReference { Destination = destination, Title = title };

        var key = id.ToString().ToLowerInvariant();

        if (!linkReferences.ContainsKey(key))
        {
            linkReferences[key] = link;
        }

        newIndex = position;

        return true;
    }
}
