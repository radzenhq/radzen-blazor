using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using System;

namespace Radzen.Blazor.Markdown;

class InlineParser
{
    class Delimiter
    {
        public char Char { get; set; }
        public int Length { get; set; }
        public int Position { get; set; }
        public Text Node { get; set; }
        public bool CanOpen { get; set; }
        public bool CanClose { get; set; }
        public bool Active { get; set; } = true;
    }

    private const char Asterisk = '*';
    private const char Underscore = '_';
    internal const char Backslash = '\\';
    private const char Null = '\0';
    private const char Backtick = '`';
    internal const char Space = ' ';
    internal const char LineFeed = '\n';
    private const char CarrigeReturn = '\r';
    internal const char OpenBracket = '[';
    internal const char CloseBracket = ']';
    private const char OpenParenthesis = '(';
    private const char CloseParenthesis = ')';
    internal const char Quote = '"';
    internal const char OpenAngleBracket = '<';
    internal const char CloseAngleBracket = '>';
    private const char SingleQuote = '\'';
    private const char Exclamation = '!';
    internal const char Colon = ':';

    private readonly List<Inline> inlines = [];
    private readonly List<Delimiter> delimiters = [];
    private readonly StringBuilder buffer = new();

    enum LinkState
    {
        Text,
        Destination,
        Title
    }

    private void AddTextNode(bool trim = false)
    {
        if (buffer.Length > 0)
        {
            var output = new StringBuilder();

            for (var index = 0; index < buffer.Length; index++)
            {
                var ch = buffer[index];

                if (ch is Backslash && index < buffer.Length - 1 && !buffer[index + 1].IsPunctuation())
                {
                    continue;
                }
                else
                {
                    output.Append(ch);
                }
            }
            var value = output.ToString();

            if (trim)
            {
                value = value.TrimEnd();
            }

            inlines.Add(new Text(value));

            buffer.Clear();
        }
    }

    private bool TryParseCode(string text, int index, out int newIndex)
    {
        if (text[index] is not Backtick)
        {
            newIndex = index;
            return false;
        }

        AddTextNode();

        // Count opening backticks
        var openingCount = 0;
        var position = index;
        while (position < text.Length && text[position] is Backtick)
        {
            openingCount++;
            position++;
        }

        // Find matching closing backticks
        var searchStart = position;
        var bestMatch = -1;

        while (position < text.Length)
        {
            // Count consecutive backticks
            var count = 0;
            var closingStart = position;
            while (position < text.Length && text[position] is Backtick)
            {
                count++;
                position++;
            }

            if (count == openingCount)
            {
                bestMatch = closingStart;
                break;
            }

            if (position < text.Length)
            {
                position++;
            }
        }

        if (bestMatch >= 0)
        {
            var content = text[searchStart..bestMatch];

            content = BlockParser.NewLineRegex.Replace(content, $"{Space}");

            if (content.StartsWith(Space) && content.EndsWith(Space) && !string.IsNullOrWhiteSpace(content))
            {
                content = content[1..^1];
            }

            inlines.Add(new Code(content));

            newIndex = bestMatch + openingCount;

            return true;
        }

        inlines.Add(new Text($"{new string(Backtick, openingCount)}"));

        newIndex = index + openingCount;

        return true;
    }

    private bool TryParseBackslash(string text, int index, char next, out int newIndex)
    {
        if (text[index] is not Backslash)
        {
            newIndex = index;
            return false;
        }

        if (next.IsPunctuation())
        {
            AddTextNode();
            inlines.Add(new Text(text[index + 1].ToString()));
            newIndex = index + 2;
            return true;
        }

        buffer.Append(text[index]);
        newIndex = index + 1;
        return true;
    }

    private bool TryParseDelimiter(string text, int index, char next, char prev, out int newIndex)
    {
        var ch = text[index];

        if (ch is not (Asterisk or Underscore or OpenBracket) && (ch is not Exclamation || next is not OpenBracket))
        {
            newIndex = index;
            return false;
        }

        AddTextNode();

        var position = index;

        while (position < text.Length && text[position] == ch)
        {
            buffer.Append(ch);

            position++;
        }

        if (ch is Exclamation)
        {
            buffer.Append(OpenBracket);
            position++;
        }

        next = position < text.Length ? text[position] : Null;

        if (buffer.Length > 0)
        {
            var node = new Text(buffer.ToString());
            var leftFlanking = LeftFlanking(prev, next);
            var rightFlanking = RightFlanking(prev, next);

            var canOpen = false;
            var canClose = false;

            if (ch is Asterisk)
            {
                canOpen = leftFlanking;
                canClose = rightFlanking;
            }

            if (ch is Underscore)
            {
                canClose = rightFlanking && (!leftFlanking || next.IsPunctuation());
                canOpen = leftFlanking && (!rightFlanking || prev.IsPunctuation());
            }

            var delimiter = new Delimiter
            {
                Node = node,
                Char = ch,
                Length = buffer.Length,
                Position = index,
                CanClose = canClose,
                CanOpen = canOpen
            };

            delimiters.Add(delimiter);
            inlines.Add(node);
            buffer.Clear();
        }

        newIndex = position;
        return true;
    }

    private static bool RightFlanking(char prev, char next)
    {
        /*
        that is (1) not preceded by Unicode whitespace, and either (2a) not preceded by a Unicode punctuation character, 
        or (2b) preceded by a Unicode punctuation character and followed by Unicode whitespace or a Unicode punctuation character.
        */

        return !prev.IsNullOrWhiteSpace() && (!prev.IsPunctuation() || next.IsNullOrWhiteSpace() || next.IsPunctuation());
    }

    private static bool LeftFlanking(char prev, char next)
    {
        /*
        that is (1) not followed by Unicode whitespace, and either (2a) not followed by a Unicode punctuation character, 
        or (2b) followed by a Unicode punctuation character and preceded by Unicode whitespace or a Unicode punctuation character.
        */

        return !next.IsNullOrWhiteSpace() && (!next.IsPunctuation() || prev.IsNullOrWhiteSpace() || prev.IsPunctuation());
    }

    public static List<Inline> Parse(string text, Dictionary<string, LinkReference> linkReferences)
    {
        var parser = new InlineParser();

        return parser.ParseInlines(text.Trim(), linkReferences);
    }

    private static readonly Regex EmailRegex = new(@"^([a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*)");

    private bool TryParseAutoLink(string text, int index, out int newIndex)
    {
        newIndex = index;

        if (text[index] is not OpenAngleBracket)
        {
            return false;
        }

        var destination = new StringBuilder();

        var position = index + 1;

        while (position < text.Length && text[position] is not CloseAngleBracket)
        {
            destination.Append(text[position]);
            position++;
        }

        if (position >= text.Length || text[position] is not CloseAngleBracket)
        {
            return false;
        }

        var url = destination.ToString();

        if (url.Contains(Space))
        {
            return false;
        }

        var content = url;

        if (EmailRegex.IsMatch(url))
        {
            url = $"mailto:{url}";
        }
        else if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return false;
        }

        var link = new Link { Destination = url };

        link.Add(new Text(content));

        inlines.Add(link);

        newIndex = position + 1;

        return true;
    }

    private List<Inline> ParseInlines(string text, Dictionary<string, LinkReference> references)
    {
        var index = 0;

        while (index < text.Length)
        {
            if (TryParseHtml(text, index, out index))
            {
                continue;
            }

            if (TryParseAutoLink(text, index, out index))
            {
                continue;
            }

            if (TryParseCode(text, index, out index))
            {
                continue;
            }

            if (TryParseLineBreak(text, index, out index))
            {
                continue;
            }

            if (TryParseSoftLineBreak(text, index, out index))
            {
                continue;
            }

            char next = index < text.Length - 1 ? text[index + 1] : Null;

            if (TryParseBackslash(text, index, next, out index))
            {
                continue;
            }

            char prev = index > 0 ? text[index - 1] : Null;

            if (TryParseDelimiter(text, index, next, prev, out index))
            {
                continue;
            }

            if (TryParseLinkFromReference(text, index, references, out index))
            {
                continue;
            }

            if (TryParseLinkOrImage(text, index, out index))
            {
                continue;
            }

            buffer.Append(text[index]);

            index++;
        }

        AddTextNode();

        ParseEmphasisAndStrong();

        NormalizeText();

        return inlines;
    }

    private void NormalizeText()
    {
        if (inlines.Count > 0)
        {
            if (inlines[0] is Text first)
            {
                first.Value = first.Value.TrimStart();
            }

            if (inlines[^1] is Text last)
            {
                last.Value = last.Value.TrimEnd();
            }
        }
    }

    private bool TryParseSoftLineBreak(string text, int index, out int newIndex)
    {
        newIndex = index;

        if (TryParseNewLine(text, index, out var position))
        {
            AddTextNode(trim: true);

            inlines.Add(new SoftLineBreak());

            newIndex = position;

            return true;
        }

        return false;
    }

    private bool TryParseLineBreak(string text, int index, out int newIndex)
    {
        newIndex = index;

        if (text[index] is not Space && text[index] is not Backslash)
        {
            return false;
        }

        var position = index + 1;

        if (position < text.Length && text[position] is Space)
        {
            while (position < text.Length && text[position] is Space)
            {
                position++;
            }
        }

        if (text[index] is Space && position == index + 1)
        {
            return false;
        }

        if (position < text.Length && TryParseNewLine(text, position, out position))
        {
            AddTextNode(trim: true);

            inlines.Add(new LineBreak());

            newIndex = position;

            return true;
        }

        return false;
    }

    private static bool TryParseNewLine(string text, int position, out int newIndex)
    {
        newIndex = position;

        if (position >= text.Length)
        {
            return false;
        }

        if (text[position] is LineFeed)
        {
            newIndex = position + 1;
            return true;
        }

        if (text[position] is CarrigeReturn && position < text.Length - 1 && text[position + 1] is LineFeed)
        {
            newIndex = position + 2;
            return true;
        }

        return false;
    }

    private bool TryParseHtml(string text, int index, out int newIndex)
    {
        newIndex = index;

        var match = BlockParser.HtmlRegex.Match(text[index..]);

        if (match.Success)
        {
            AddTextNode();

            var value = text[index..(index + match.Length)];

            inlines.Add(new HtmlInline { Value = value });

            newIndex = index + match.Length;

            return true;
        }

        return false;
    }

    internal static bool TryParseDestinationAndTitle(string text, int position, out string destination, out string title, out int newPosition)
    {
        newPosition = position;
        destination = string.Empty;
        title = string.Empty;

        // Skip whitespace
        while (position < text.Length && text[position] is Space or LineFeed)
        {
            position++;
        }

        if (position >= text.Length)
        {
            return false;
        }

        // Parse destination
        var destinationBuilder = new StringBuilder();

        var angleBrackets = position < text.Length && text[position] is OpenAngleBracket;

        if (angleBrackets)
        {
            position++;
        }

        var parentheses = 0;

        while (position < text.Length)
        {
            var ch = text[position];
            var prev = position > 0 ? text[position - 1] : Null;
            var next = position < text.Length - 1 ? text[position + 1] : Null;

            if (angleBrackets && ch is CloseAngleBracket && prev is not Backslash)
            {
                position++;
                break;
            }

            if (!angleBrackets)
            {
                if (ch is OpenParenthesis && prev is not Backslash)
                {
                    parentheses++;
                }
                else if (ch is CloseParenthesis && prev is not Backslash)
                {
                    if (parentheses == 0)
                    {
                        break;
                    }
                    parentheses--;
                }

                if (ch is Space or LineFeed)
                {
                    break;
                }
            }

            if (ch is Backslash && next.IsPunctuation())
            {
                position++;
                continue;
            }

            destinationBuilder.Append(ch);
            position++;
        }

        if (angleBrackets)
        {
            // Skip whitespace after angle brackets
            while (position < text.Length && text[position] is Space)
            {
                position++;
            }
        }

        // Parse title if present
        var titleBuilder = new StringBuilder();
        if (position < text.Length && text[position].IsNullOrWhiteSpace())
        {
            var lines = 0;

            while (position < text.Length && text[position].IsNullOrWhiteSpace())
            {
                if (text[position] is LineFeed)
                {
                    lines++;
                }

                position++;
            }

            var titleStart = position;

            if (position < text.Length)
            {
                var titleDelimiter = text[position];

                if (titleDelimiter is Quote or SingleQuote or OpenParenthesis)
                {
                    position++; // Skip opening delimiter

                    char closingDelimiter = titleDelimiter is OpenParenthesis ? CloseParenthesis : titleDelimiter;

                    while (position < text.Length && (text[position] != closingDelimiter || text[position - 1] is Backslash))
                    {
                        if (text[position] is LineFeed && titleBuilder.Length > 0 && titleBuilder[^1] is LineFeed)
                        {
                            return false;
                        }

                        titleBuilder.Append(text[position]);
                        position++;
                    }

                    if (position < text.Length && text[position] == closingDelimiter)
                    {
                        position++; // Skip closing delimiter

                        // Skip whitespace after title
                        while (position < text.Length && text[position] is Space)
                        {
                            position++;
                        }

                        if (position < text.Length)
                        {
                            if (position < text.Length && text[position] is not (CloseParenthesis or LineFeed))
                            {
                                if (lines > 0)
                                {
                                    // non-white space characters after title
                                    newPosition = titleStart;
                                    destination = destinationBuilder.ToString();
                                    title = string.Empty;

                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }

                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        if (position < text.Length)
        {
            if (text[position] is LineFeed)
            {
                position++;
            }
            else if (text[position] is Quote or SingleQuote or OpenParenthesis)
            {
                return false;
            }
        }

        destination = destinationBuilder.ToString();
        title = titleBuilder.ToString();
        newPosition = position;
        return true;
    }

    private bool TryParseLinkOrImage(string text, int index, out int newIndex)
    {
        newIndex = index;

        if (!TryGetOpenerIndex(text, index, out var openerIndex, out var position))
        {
            return false;
        }

        if (!TryParseDestinationAndTitle(text, position, out var destination, out var title, out position))
        {
            return false;
        }

        if (position >= text.Length || text[position] is not CloseParenthesis)
        {
            return false;
        }

        var opener = delimiters[openerIndex];

        InlineContainer container = opener.Char == Exclamation ? new Image { Destination = destination, Title = title } : new Link { Destination = destination, Title = title };

        ReplaceOpener(openerIndex, container);

        newIndex = position + 1;

        if (container is Link)
        {
            for (var delimiterIndex = 0; delimiterIndex < openerIndex; delimiterIndex++)
            {
                if (delimiters[delimiterIndex].Char == OpenBracket)
                {
                    delimiters[delimiterIndex].Active = false;
                }
            }
        }

        delimiters.Remove(opener);

        return true;
    }


    private bool TryGetOpenerIndex(string text, int index, out int openerIndex, out int position)
    {
        position = index;
        openerIndex = -1;

        if (text[index] is not CloseBracket)
        {
            return false;
        }
        var di = delimiters.Count - 1;

        while (di >= 0)
        {
            var delimiter = delimiters[di];

            if ((delimiter.Active && delimiter.Char is OpenBracket) || delimiter.Char is Exclamation)
            {
                openerIndex = di;
                break;
            }

            di--;
        }

        if (di < 0)
        {
            return false;
        }

        AddTextNode();

        position = index + 1;

        // Skip if not followed by opening parenthesis
        if (position >= text.Length || text[position] is not OpenParenthesis)
        {
            delimiters.RemoveAt(openerIndex);
            return false;
        }

        position++; // Skip opening parenthesis

        return true;
    }

    private void ReplaceOpener(int openerIndex, InlineContainer parent)
    {
        var startIndex = inlines.FindIndex(delimiters[openerIndex].Node.Equals);

        ParseEmphasisAndStrong(openerIndex);

        var endIndex = inlines.Count - startIndex;

        var children = inlines.GetRange(startIndex + 1, endIndex - 1);

        inlines.RemoveRange(startIndex, endIndex);

        foreach (var child in children)
        {
            parent.Add(child);
        }

        inlines.Insert(startIndex, parent);
    }


    private bool TryParseLinkFromReference(string text, int index, Dictionary<string, LinkReference> references, out int newIndex)
    {
        newIndex = index;

        if (references.Count == 0 || text[index] is not CloseBracket)
        {
            return false;
        }

        var openerIndex = FindOpenBracketIndex(OpenBracket);

        if (openerIndex < 0)
        {
            return false;
        }

        AddTextNode();

        var startIndex = inlines.FindIndex(delimiters[openerIndex].Node.Equals);

        var endIndex = inlines.Count - startIndex;

        var children = inlines.GetRange(startIndex + 1, endIndex - 1);

        var id = new StringBuilder();

        foreach (var child in children)
        {
            if (child is Text textNode)
            {
                id.Append(textNode.Value);
            }
        }

        if (!references.TryGetValue(id.ToString().ToLowerInvariant(), out var reference))
        {
            return false;
        }

        var link = new Link { Destination = reference.Destination, Title = reference.Title };

        foreach (var child in children)
        {
            link.Add(child);
        }

        inlines.RemoveRange(startIndex, endIndex);
        inlines.Insert(startIndex, link);

        link.Destination = reference.Destination;
        link.Title = reference.Title;
        newIndex = index + 1;

        return true;
    }

    private int FindOpenBracketIndex(char ch)
    {
        for (var index = delimiters.Count - 1; index >= 0; index--)
        {
            if (delimiters[index].Char == ch)
            {
                return index;
            }
        }

        return -1;
    }

    private void ParseEmphasisAndStrong(int index = -1)
    {
        var closerIndex = 0;

        while ((closerIndex = FindCloserIndex()) > 0)
        {
            var openerIndex = FindOpenerIndex(closerIndex, index);

            if (openerIndex >= 0)
            {
                var closer = delimiters[closerIndex];
                var opener = delimiters[openerIndex];
                var startIndex = inlines.FindIndex(opener.Node.Equals);
                var endIndex = inlines.FindIndex(closer.Node.Equals);

                if (startIndex >= 0 && endIndex >= 0)
                {
                    var innerInlines = inlines.GetRange(startIndex + 1, endIndex - startIndex - 1);

                    var charsToConsume = closer.Length == opener.Length && closer.Length > 1 ? 2 : 1;

                    InlineContainer parent = charsToConsume == 1 ? new Emphasis() : new Strong();

                    foreach (var child in innerInlines)
                    {
                        parent.Add(child);
                    }

                    opener.Length -= charsToConsume;

                    if (opener.Length > 0)
                    {
                        opener.Node.Value = opener.Node.Value[..^charsToConsume];
                        startIndex += charsToConsume;
                    }

                    closer.Length -= charsToConsume;

                    if (closer.Length > 0)
                    {
                        closer.Node.Value = closer.Node.Value[..^charsToConsume];
                        endIndex -= charsToConsume;
                    }

                    inlines.RemoveRange(startIndex, endIndex - startIndex + 1);

                    inlines.Insert(startIndex, parent);
                }

                delimiters.RemoveAt(closerIndex);
                delimiters.RemoveAt(openerIndex);
            }
            else
            {
                break;
            }
        }
    }

    private int FindCloserIndex()
    {
        for (var index = 1; index < delimiters.Count; index++)
        {
            var delimiter = delimiters[index];

            if (delimiter.CanClose && (delimiter.Char is Asterisk or Underscore))
            {
                return index;
            }
        }

        return -1;
    }

    private int FindOpenerIndex(int startIndex, int endIndex)
    {
        var closer = delimiters[startIndex];

        for (var index = startIndex - 1; index > endIndex; index--)
        {
            var delimiter = delimiters[index];

            if (delimiter.CanOpen && delimiter.Char == closer.Char)
            {
                return index;
            }
        }

        return -1;
    }
}