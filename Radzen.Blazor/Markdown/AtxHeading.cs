using System.Text.RegularExpressions;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a markdown ATX heading: <c># Heading</c>.
/// </summary>
public class AtxHeading : Heading
{
    private static readonly Regex MarkerRegex = new(@"^#{1,6}(?:[ \t]+|$)");

    private static readonly Regex StartRegex = new(@"^[ \t]*#+[ \t]*$");

    private static readonly Regex EndRegex = new(@"[ \t]+#+[ \t]*$");

    internal static BlockStart Start(BlockParser parser, Block block)
    {
        if (parser.Indented)
        {
            return BlockStart.Skip;
        }

        var line = parser.CurrentLine[parser.NextNonSpace..];

        var match = MarkerRegex.Match(line);

        if (match.Success) 
        {
            parser.AdvanceNextNonSpace();
            parser.AdvanceOffset(match.Length, false);
            parser.CloseUnmatchedBlocks();
            var container = parser.AddChild<AtxHeading>(parser.NextNonSpace);
            container.Level = match.Value.Trim().Length;

            // remove trailing ###s:
            line = parser.CurrentLine[parser.Offset..];

            container.Value = EndRegex.Replace(StartRegex.Replace(line, ""), "");

            parser.AdvanceOffset(parser.CurrentLine.Length - parser.Offset, false);

            return BlockStart.Leaf;
        }

        return BlockStart.Skip;
    }
}