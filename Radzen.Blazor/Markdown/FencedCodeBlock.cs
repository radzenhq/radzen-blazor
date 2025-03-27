using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a fenced code block in a markdown document: <c>```</c> or <c>~~~</c>.
/// </summary>
public class FencedCodeBlock : Leaf
{
    /// <summary>
    /// The delimiter used to start and end the code block.
    /// </summary>
    public string Delimiter { get; private set; }
    internal int Indent { get; private set; }

    /// <summary>
    /// The info string of the code block. This is the first line of the code block and is used to specify the language of the code block.
    /// </summary>
    public string Info { get; private set; }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitFencedCodeBlock(this);
    }

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        // first line becomes info string
        var newlinePos = Value.IndexOf('\n');
        var firstLine = Value[..newlinePos];
        Info = firstLine.Trim();
        Value = Value[(newlinePos + 1)..];
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        var line = parser.CurrentLine[parser.NextNonSpace..];

        var indent = parser.Indent;

        var match = ClosingFenceRegex.Match(line);

        if (indent <= 3 && parser.PeekNonSpace() == Delimiter[0] && match.Success && match.Length >= Delimiter.Length)
        {
            // closing fence - we're at end of line, so we can return
            parser.LastLineLength = parser.Offset + indent + match.Length;
            parser.Close(this, parser.LineNumber);
            return BlockMatch.Break;
        }
        else
        {
            // skip optional spaces of fence offset
            var i = Indent;

            while (i > 0 && parser.Peek().IsSpaceOrTab())
            {
                parser.AdvanceOffset(1, true);
                i--;
            }
        }

        return BlockMatch.Match;
    }


    private static readonly Regex ClosingFenceRegex = new(@"^(?:`{3,}|~{3,})(?=[ \t]*$)");

    private static readonly Regex OpeningFenceRegex = new(@"^`{3,}(?!.*`)|^~{3,}");

    internal static BlockStart Start(BlockParser parser, Block node)
    {
        if (parser.Indented)
        {
            return BlockStart.Skip;
        }

        var line = parser.CurrentLine[parser.NextNonSpace..];

        var match = OpeningFenceRegex.Match(line);

        if (match.Success) 
        {
            parser.CloseUnmatchedBlocks();

            var container = parser.AddChild<FencedCodeBlock>(parser.NextNonSpace);
            container.Delimiter = match.Value;
            container.Indent = parser.Indent;
            parser.AdvanceNextNonSpace();
            parser.AdvanceOffset(match.Value.Length, false);
            return BlockStart.Leaf;
        }

        return BlockStart.Skip;
    }
}