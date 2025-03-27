using System.Linq;
using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a markdown indented code block.
/// </summary>
public class IndentedCodeBlock : Leaf
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitIndentedCodeBlock(this);
    }

    internal override BlockMatch Matches(BlockParser parser)
    {
        if (parser.Indent >= BlockParser.CodeIndent) 
        {
            parser.AdvanceOffset(BlockParser.CodeIndent, true);
        } 
        else if (parser.Blank) 
        {
            parser.AdvanceNextNonSpace();
        }
        else 
        {
            return BlockMatch.Skip;
        }

        return BlockMatch.Match;
    }

    private static readonly Regex TrailingWhiteSpaceRegex = new(@"^[ \t]*$");

    internal override void Close(BlockParser parser)
    {
        base.Close(parser);

        var lines = Value.Split('\n').ToList();;
        // Note that indented code block cannot be empty, so
        // lines.length cannot be zero.

        while (TrailingWhiteSpaceRegex.IsMatch(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        Value = string.Join('\n', lines) + '\n';

        Range.End.Line = Range.Start.Line + lines.Count - 1;
        Range.End.Column = Range.Start.Column + lines[^1].Length - 1;
    }

    internal static BlockStart Start(BlockParser parser, Block container)
    {
        if (parser.Indented && parser.Tip is not Paragraph && !parser.Blank)
        {
            // indented code
            parser.AdvanceOffset(BlockParser.CodeIndent, true);
            parser.CloseUnmatchedBlocks();
            parser.AddChild<IndentedCodeBlock>(parser.Offset);
            return BlockStart.Leaf;
        }

        return BlockStart.Skip;
    }
}
