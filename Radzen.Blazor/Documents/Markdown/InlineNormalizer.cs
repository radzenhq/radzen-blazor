using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Markdown;

internal static class InlineNormalizer
{
    public static List<Inline> Normalize(IReadOnlyList<Inline> inlines, bool lineStart)
    {
        var normalized = Normalize(inlines.Select(Clone).ToList(), lineStart ? '\n' : ' ', '\n', []);

        while (normalized.Count > 0 && (normalized[0] is SoftLineBreak || normalized.All(inline => inline is LineBreak or SoftLineBreak)))
        {
            normalized.RemoveAt(0);
        }

        while (normalized.Count > 0 && normalized[^1] is SoftLineBreak)
        {
            normalized.RemoveAt(normalized.Count - 1);
        }

        while (normalized.Count > 1 && normalized[^1] is LineBreak && normalized[^2] is LineBreak)
        {
            normalized.RemoveAt(normalized.Count - 2);
        }

        return normalized;
    }

    private static Inline Clone(Inline inline) => inline switch
    {
        Text text => new Text(text.Value),
        Code code => new Code(code.Value) { Ticks = code.Ticks },
        Emphasis emphasis => Fill(new Emphasis { Marker = emphasis.Marker }, emphasis),
        Strong strong => Fill(new Strong { Marker = strong.Marker }, strong),
        Strikethrough strikethrough => Fill(new Strikethrough { Tildes = strikethrough.Tildes }, strikethrough),
        Link link => Fill(new Link { Destination = link.Destination, Title = link.Title, Suffix = link.Suffix, Autolink = link.Autolink }, link),
        Image image => Fill(new Image { Destination = image.Destination, Title = image.Title, Suffix = image.Suffix }, image),
        HtmlInline html => new HtmlInline { Value = html.Value },
        LineBreak lineBreak => new LineBreak { Backslash = lineBreak.Backslash },
        _ => new SoftLineBreak()
    };

    private static InlineContainer Fill(InlineContainer container, InlineContainer source)
    {
        foreach (var child in source.Children)
        {
            container.Add(Clone(child));
        }

        return container;
    }

    private static List<Inline> Normalize(List<Inline> inlines, char before, char after, List<Type> open)
    {
        var result = Merge(inlines, open);

        for (var index = 0; index < result.Count; index++)
        {
            if (result[index] is Link or Image)
            {
                var link = (InlineContainer)result[index];
                link.ReplaceInlines(Normalize(link.Children.ToList(), '[', ']', []));
                continue;
            }

            if (result[index] is not (Emphasis or Strong or Strikethrough))
            {
                continue;
            }

            var mark = (InlineContainer)result[index];
            var marker = mark is Strikethrough ? '~' : (mark as Emphasis)?.Marker ?? (mark as Strong)?.Marker ?? '*';
            var body = Normalize(mark.Children.ToList(), marker, marker, [.. open, mark.GetType()]);
            var breaks = new List<Inline>();
            var leadingBreaks = new List<Inline>();

            while (body.Count > 0 && body[^1] is LineBreak)
            {
                breaks.Insert(0, body[^1]);
                body.RemoveAt(body.Count - 1);
            }

            while (body.Count > 0 && body[0] is LineBreak)
            {
                leadingBreaks.Add(body[0]);
                body.RemoveAt(0);
            }

            var outsideBefore = index > 0 ? LastChar(result[index - 1], before) : before;
            var outsideAfter = breaks.Count > 0 ? FirstChar(breaks[0], after) : index + 1 < result.Count ? FirstChar(result[index + 1], after) : after;
            var leading = Expel(body, outsideBefore, leading: true);
            var trailing = Expel(body, outsideAfter, leading: false);
            mark.ReplaceInlines(body);
            var replacement = new List<Inline>(leadingBreaks);

            if (leading != null)
            {
                replacement.Add(leading);
            }

            if (!Blank(body))
            {
                replacement.Add(mark);
            }

            if (trailing != null)
            {
                replacement.Add(trailing);
            }

            replacement.AddRange(breaks);
            result.RemoveAt(index);
            result.InsertRange(index, replacement);
            index += replacement.Count - 1;
        }

        Trim(result, before == '\n');
        return result;
    }

    private static void Trim(List<Inline> result, bool lineStart)
    {
        for (var index = 0; index < result.Count; index++)
        {
            if (result[index] is not Text text)
            {
                lineStart = result[index] is LineBreak or SoftLineBreak;
                continue;
            }

            var value = text.Value;

            if (lineStart)
            {
                value = value.TrimStart(' ', '\t');
            }

            if (index + 1 < result.Count && result[index + 1] is SoftLineBreak)
            {
                value = value.TrimEnd(' ', '\t');
            }

            text.Value = value;
            lineStart = lineStart && value.Length == 0;
        }

        result.RemoveAll(inline => inline is Text { Value.Length: 0 });
    }

    private static Text? Expel(List<Inline> body, char outside, bool leading)
    {
        var text = EdgeText(body, leading);

        if (text == null)
        {
            return null;
        }

        var value = text.Value;
        var count = value.Length - (leading ? value.TrimStart() : value.TrimEnd()).Length;

        if (count == 0 && value.Length > 0 && !Flanks(outside, leading ? value[0] : value[^1]))
        {
            count = Punctuation(value, leading);
        }

        if (count == 0)
        {
            return null;
        }

        var expelled = new Text(leading ? value[..count] : value[^count..]);
        text.Value = leading ? value[count..] : value[..^count];
        return expelled;
    }

    // CommonMark 0.31.2, 6.2 Emphasis and 6.1 Code spans: nested or adjacent runs of one kind cannot be told apart from a longer run, and they render the same as a single node
    private static List<Inline> Merge(List<Inline> inlines, List<Type> open)
    {
        var result = new List<Inline>();

        foreach (var inline in inlines)
        {
            Add(inline);
        }

        return result;

        Inline? Last() => result.LastOrDefault(candidate => candidate is not Text { Value.Length: 0 });

        void Add(Inline inline)
        {
            if (inline is Emphasis or Strong or Strikethrough && open.Contains(inline.GetType()))
            {
                foreach (var child in ((InlineContainer)inline).Children.ToList())
                {
                    Add(child);
                }

                return;
            }

            if (inline is Code code && Last() is Code previous)
            {
                result[result.IndexOf(previous)] = new Code(previous.Value + code.Value) { Ticks = previous.Ticks };
                return;
            }

            if (inline is Emphasis or Strong or Strikethrough && Last() is { } last && last.GetType() == inline.GetType())
            {
                var container = (InlineContainer)last;

                foreach (var child in ((InlineContainer)inline).Children.ToList())
                {
                    container.Add(child);
                }

                return;
            }

            result.Add(inline);
        }
    }

    private static Text? EdgeText(IReadOnlyList<Inline> body, bool leading)
    {
        while (true)
        {
            if (body.Count == 0)
            {
                return null;
            }

            var edge = leading ? body[0] : body[^1];

            switch (edge)
            {
                case Text text:
                    return text;
                case Emphasis or Strong or Strikethrough:
                    body = ((InlineContainer)edge).Children;
                    break;
                default:
                    return null;
            }
        }
    }

    public static bool Blank(IReadOnlyList<Inline> body) => body.All(child => child switch
    {
        Text text => text.Value.Length == 0,
        Emphasis or Strong or Strikethrough => Blank(((InlineContainer)child).Children),
        _ => false
    });

    private static char LastChar(Inline inline, char fallback) => inline switch
    {
        Text { Value.Length: > 0 } text => text.Value[^1],
        Emphasis emphasis => emphasis.Marker ?? '*',
        Strong strong => strong.Marker ?? '*',
        Strikethrough => '~',
        Code => '`',
        Link or Image => ')',
        LineBreak or SoftLineBreak => '\n',
        _ => fallback
    };

    public static char FirstChar(Inline inline, char fallback) => inline switch
    {
        Text { Value.Length: > 0 } text => text.Value[0],
        Emphasis emphasis => Lead(emphasis, emphasis.Marker ?? '*'),
        Strong strong => Lead(strong, strong.Marker ?? '*'),
        Strikethrough strikethrough => Lead(strikethrough, '~'),
        Code => '`',
        Link { Autolink: true } => '<',
        Link => '[',
        Image => '!',
        HtmlInline { Value.Length: > 0 } html => html.Value[0],
        LineBreak lineBreak => lineBreak.Backslash == true ? '\\' : ' ',
        SoftLineBreak => '\n',
        _ => fallback
    };

    private static char Lead(InlineContainer container, char marker) => container.Children is [Text { Value.Length: > 0 } first, ..] && char.IsWhiteSpace(first.Value[0]) ? first.Value[0] : marker;

    // CommonMark 0.31.2, 6.2 Emphasis: a delimiter run next to punctuation is only flanking when the other side is whitespace or punctuation
    private static bool Flanks(char outside, char inside) => !(inside.IsPunctuation() && !char.IsWhiteSpace(outside) && !outside.IsPunctuation() && outside != '\0');

    private static int Punctuation(string value, bool leading)
    {
        var count = 0;

        while (count < value.Length && (leading ? value[count] : value[^(count + 1)]).IsPunctuation())
        {
            count++;
        }

        return count;
    }
}
