using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Markdown;

internal enum MarkKind
{
    Link,
    Strong,
    Emphasis,
    Strikethrough,
    Code
}

internal sealed class Mark : IEquatable<Mark>
{
    public Mark(MarkKind kind, Inline? origin = null, string? destination = null, string? title = null)
    {
        Kind = kind;
        Origin = origin;
        Destination = destination;
        Title = title;
    }

    public MarkKind Kind { get; }

    public Inline? Origin { get; }

    public string? Destination { get; }

    public string? Title { get; }

    public bool Equals(Mark? other) => other != null && Kind == other.Kind && Destination == other.Destination && Title == other.Title;

    public override bool Equals(object? obj) => Equals(obj as Mark);

    public override int GetHashCode() => HashCode.Combine(Kind, Destination, Title);
}

internal sealed class Run
{
    public Run(string text, IReadOnlyList<Mark> marks, Inline? origin)
    {
        Text = text;
        Marks = marks.OrderBy(mark => (int)mark.Kind).ToList();
        Origin = origin;
    }

    public Run(Inline atom, IReadOnlyList<Mark> marks)
    {
        Text = "￼";
        Marks = marks.OrderBy(mark => (int)mark.Kind).ToList();
        Atom = atom;
    }

    public string Text { get; set; }

    public List<Mark> Marks { get; }

    public Inline? Origin { get; set; }

    public Inline? Atom { get; }

    public int Length => Text.Length;

    public bool HasMark(MarkKind kind) => Marks.Any(mark => mark.Kind == kind);

    public Run Slice(int start, int end)
    {
        if (Atom != null)
        {
            return this;
        }

        var origin = start == 0 && end == Text.Length ? Origin : null;
        return new Run(Text[start..end], Marks, origin);
    }

    public Run WithMarks(IEnumerable<Mark> marks) => Atom != null ? new Run(Atom, marks.ToList()) : new Run(Text, marks.ToList(), Origin);
}

internal static class InlineContent
{
    public static List<Run> Flatten(IReadOnlyList<Inline> inlines)
    {
        var runs = new List<Run>();
        Flatten(inlines, [], runs);
        return runs;
    }

    private static void Flatten(IReadOnlyList<Inline> inlines, List<Mark> marks, List<Run> runs)
    {
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case Text text:
                    runs.Add(new Run(text.Value, marks, text));
                    break;
                case Code code:
                    runs.Add(new Run(code.Value, [.. marks, new Mark(MarkKind.Code, code)], code));
                    break;
                case Emphasis emphasis:
                    Flatten(emphasis.Children, [.. marks, new Mark(MarkKind.Emphasis, emphasis)], runs);
                    break;
                case Strong strong:
                    Flatten(strong.Children, [.. marks, new Mark(MarkKind.Strong, strong)], runs);
                    break;
                case Strikethrough strikethrough:
                    Flatten(strikethrough.Children, [.. marks, new Mark(MarkKind.Strikethrough, strikethrough)], runs);
                    break;
                case Link link:
                    Flatten(link.Children, [.. marks, new Mark(MarkKind.Link, link, link.Destination, link.Title)], runs);
                    break;
                default:
                    runs.Add(new Run(inline, marks));
                    break;
            }
        }
    }

    public static List<Inline> Rebuild(IReadOnlyList<Run> runs, string source)
    {
        var merged = Merge(runs);
        var result = new List<Inline>();
        var stack = new List<(Mark Mark, InlineContainer Container)>();

        foreach (var run in merged)
        {
            var marks = stack.Select(entry => entry.Mark).Where(run.Marks.Contains).Concat(run.Marks.Where(mark => !stack.Any(entry => entry.Mark.Equals(mark)))).ToList();
            var common = 0;

            while (common < stack.Count && common < marks.Count && stack[common].Mark.Equals(marks[common]) && marks[common].Kind != MarkKind.Code)
            {
                common++;
            }

            stack.RemoveRange(common, stack.Count - common);

            for (var index = common; index < marks.Count; index++)
            {
                var mark = marks[index];

                if (mark.Kind == MarkKind.Code)
                {
                    break;
                }

                var container = CreateContainer(mark, source);
                Append(stack, result, container);
                stack.Add((mark, container));
            }

            Append(stack, result, CreateNode(run, source));
        }

        return result;
    }

    private static void Append(List<(Mark Mark, InlineContainer Container)> stack, List<Inline> result, Inline node)
    {
        if (stack.Count > 0)
        {
            stack[^1].Container.Add(node);
        }
        else
        {
            result.Add(node);
        }
    }

    private static List<Run> Merge(IReadOnlyList<Run> runs)
    {
        var merged = new List<Run>();

        foreach (var run in runs)
        {
            if (run.Atom == null && run.Text.Length == 0)
            {
                continue;
            }

            if (merged.Count > 0 && merged[^1].Atom == null && run.Atom == null && merged[^1].Marks.SequenceEqual(run.Marks))
            {
                var previous = merged[^1];
                Inline? origin = null;

                if (previous.Origin is Text left && run.Origin is Text right && left.Pristine && right.Pristine && left.Value == previous.Text && right.Value == run.Text && left.SourceEnd == right.SourceStart)
                {
                    origin = new Text(left.Value + right.Value) { SourceStart = left.SourceStart, SourceEnd = right.SourceEnd, Pristine = true };
                }

                merged[^1] = new Run(previous.Text + run.Text, previous.Marks, origin);
                continue;
            }

            merged.Add(run);
        }

        return merged;
    }

    private static Inline CreateNode(Run run, string source)
    {
        if (run.Atom != null)
        {
            return run.Atom;
        }

        if (run.HasMark(MarkKind.Code))
        {
            if (run.Origin is Code code && code.Value == run.Text)
            {
                return code;
            }

            var mark = run.Marks.First(mark => mark.Kind == MarkKind.Code);
            return new Code(run.Text) { Ticks = mark.Origin is Code original ? TickCount(original, source) : null };
        }

        if (run.Origin is Text text && text.Value == run.Text)
        {
            return text;
        }

        return new Text(run.Text);
    }

    private static int? TickCount(Code code, string source)
    {
        if (code.Ticks != null)
        {
            return code.Ticks;
        }

        var count = 0;

        while (code.SourceStart + count < source.Length && code.SourceStart + count < code.SourceEnd && source[code.SourceStart + count] == '`')
        {
            count++;
        }

        return count > 0 ? count : null;
    }

    private static InlineContainer CreateContainer(Mark mark, string source)
    {
        switch (mark.Kind)
        {
            case MarkKind.Emphasis:
                return new Emphasis { Marker = mark.Origin is Emphasis emphasis ? emphasis.Marker ?? MarkerAt(emphasis, source) : null };
            case MarkKind.Strong:
                return new Strong { Marker = mark.Origin is Strong strong ? strong.Marker ?? MarkerAt(strong, source) : null };
            case MarkKind.Strikethrough:
                return new Strikethrough { Tildes = mark.Origin is Strikethrough strikethrough ? strikethrough.Tildes ?? RunAt(strikethrough, source, '~') : null };
            default:
                var link = new Link { Destination = mark.Destination, Title = mark.Title };

                if (mark.Origin is Link original)
                {
                    link.Suffix = original.Suffix ?? SuffixOf(original, source);
                    link.Autolink = original.Autolink || (original.SourceEnd > original.SourceStart && original.SourceStart < source.Length && source[original.SourceStart] == '<');
                }

                return link;
        }
    }

    private static char? MarkerAt(Inline inline, string source)
    {
        return inline.SourceEnd > inline.SourceStart && inline.SourceStart < source.Length && source[inline.SourceStart] is '*' or '_' ? source[inline.SourceStart] : null;
    }

    private static int? RunAt(Inline inline, string source, char ch)
    {
        var count = 0;

        while (inline.SourceStart + count < source.Length && inline.SourceStart + count < inline.SourceEnd && source[inline.SourceStart + count] == ch)
        {
            count++;
        }

        return count > 0 ? count : null;
    }

    private static string? SuffixOf(InlineContainer link, string source)
    {
        if (link.Children.Count == 0 || link.SourceEnd > source.Length || link.SourceEnd <= link.SourceStart || source[link.SourceEnd - 1] != ']')
        {
            return null;
        }

        return source[link.Children[^1].SourceEnd..link.SourceEnd];
    }

    public static (int Run, int Offset) Locate(IReadOnlyList<Run> runs, int contentOffset)
    {
        var position = 0;

        for (var index = 0; index < runs.Count; index++)
        {
            var length = runs[index].Length;

            if (contentOffset <= position + length && (contentOffset < position + length || index == runs.Count - 1 || runs[index].Atom == null))
            {
                return (index, contentOffset - position);
            }

            position += length;
        }

        return (Math.Max(0, runs.Count - 1), runs.Count > 0 ? runs[^1].Length : 0);
    }

    public static int Length(IReadOnlyList<Run> runs) => runs.Sum(run => run.Length);

    public static List<Run> Delete(IReadOnlyList<Run> runs, int start, int end)
    {
        var result = new List<Run>();
        var position = 0;

        foreach (var run in runs)
        {
            var runStart = position;
            var runEnd = position + run.Length;
            position = runEnd;

            if (runEnd <= start || runStart >= end)
            {
                result.Add(run);
                continue;
            }

            if (run.Atom != null)
            {
                continue;
            }

            var keepStart = Math.Max(0, start - runStart);
            var keepEnd = Math.Min(run.Length, end - runStart);

            if (keepStart > 0)
            {
                result.Add(run.Slice(0, keepStart));
            }

            if (keepEnd < run.Length)
            {
                result.Add(run.Slice(keepEnd, run.Length));
            }
        }

        return result;
    }

    public static List<Run> Insert(IReadOnlyList<Run> runs, int offset, string text, IReadOnlyList<Mark>? marks = null)
    {
        var result = new List<Run>();
        var position = 0;
        var inserted = false;
        var total = Length(runs);

        for (var index = 0; index < runs.Count; index++)
        {
            var run = runs[index];
            var runStart = position;
            var runEnd = position + run.Length;
            position = runEnd;

            if (inserted || offset > runEnd)
            {
                result.Add(run);
                continue;
            }

            var within = offset - runStart;
            var atEnd = within == run.Length;

            if (atEnd && index < runs.Count - 1 && run.Atom != null)
            {
                result.Add(run);
                continue;
            }

            if (run.Atom != null)
            {
                var atomMarks = marks ?? [];
                result.Add(within == 0 ? new Run(text, atomMarks, null) : run);
                result.Add(within == 0 ? run : new Run(text, atomMarks, null));
                inserted = true;
                continue;
            }

            IReadOnlyList<Mark> inherit;

            if (marks != null)
            {
                inherit = marks;
            }
            else if (within > 0 && within < run.Length)
            {
                inherit = run.Marks;
            }
            else
            {
                var left = within == run.Length ? run.Marks : runStart > 0 ? runs[index - 1].Marks : [];
                var right = within == run.Length ? index + 1 < runs.Count ? runs[index + 1].Marks : [] : run.Marks;
                inherit = text.Length > 0 && char.IsWhiteSpace(text[0]) ? left.Intersect(right).ToList() : within == 0 && runStart == 0 ? [] : left;
            }

            if (within > 0)
            {
                result.Add(run.Slice(0, within));
            }

            result.Add(new Run(text, inherit, null));

            if (within < run.Length)
            {
                result.Add(run.Slice(within, run.Length));
            }

            inserted = true;
        }

        if (!inserted)
        {
            result.Add(new Run(text, marks ?? (total == 0 ? [] : runs[^1].Marks), null));
        }

        return result;
    }

    public static List<Run> Split(IReadOnlyList<Run> runs, int offset, out List<Run> tail)
    {
        var head = new List<Run>();
        tail = [];
        var position = 0;

        foreach (var run in runs)
        {
            var runStart = position;
            var runEnd = position + run.Length;
            position = runEnd;

            if (runEnd <= offset)
            {
                head.Add(run);
            }
            else if (runStart >= offset)
            {
                tail.Add(run);
            }
            else
            {
                head.Add(run.Slice(0, offset - runStart));
                tail.Add(run.Slice(offset - runStart, run.Length));
            }
        }

        return head;
    }

    public static bool AllHave(IReadOnlyList<Run> runs, int start, int end, MarkKind kind)
    {
        var position = 0;
        var any = false;

        foreach (var run in runs)
        {
            var runStart = position;
            var runEnd = position + run.Length;
            position = runEnd;

            if (runEnd <= start || runStart >= end || run.Atom != null || run.Text.Trim().Length == 0)
            {
                continue;
            }

            any = true;

            if (!run.HasMark(kind))
            {
                return false;
            }
        }

        return any;
    }

    public static List<Run> ToggleMark(IReadOnlyList<Run> runs, int start, int end, Mark mark)
    {
        var remove = AllHave(runs, start, end, mark.Kind);
        var result = new List<Run>();
        var position = 0;

        foreach (var run in runs)
        {
            var runStart = position;
            var runEnd = position + run.Length;
            position = runEnd;

            if (runEnd <= start || runStart >= end)
            {
                result.Add(run);
                continue;
            }

            var from = Math.Max(0, start - runStart);
            var to = Math.Min(run.Length, end - runStart);

            if (run.Atom != null)
            {
                result.Add(run);
                continue;
            }

            if (from > 0)
            {
                result.Add(run.Slice(0, from));
            }

            var middle = run.Slice(from, to);
            var marks = remove ? middle.Marks.Where(existing => existing.Kind != mark.Kind).ToList() : middle.Marks.Where(existing => existing.Kind != mark.Kind).Append(mark).ToList();
            result.Add(middle.WithMarks(marks));

            if (to < run.Length)
            {
                result.Add(run.Slice(to, run.Length));
            }
        }

        return result;
    }

    public static string PlainText(IReadOnlyList<Run> runs) => string.Concat(runs.Select(run => run.Atom != null ? string.Empty : run.Text));
}
