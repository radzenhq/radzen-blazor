using System;
using System.Collections.Generic;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentEditor
{
    internal sealed record SourceElement(ContentElement Element, int Start, int End, byte[] Snapshot);

    private readonly record struct Candidate(CandidateKind Kind, int Start, int End, byte[] TextBytes);

    private enum CandidateKind
    {
        Text,
        Path,
        XObject,
        Raw,
    }

    public static IReadOnlyList<SourceElement> Map(byte[] content, ContentCollection elements)
    {
        var candidates = Candidates(content);
        var result = new List<SourceElement>(elements.Count);
        var candidateIndex = 0;
        foreach (var element in elements)
        {
            if (candidateIndex >= candidates.Count)
            {
                throw new NotSupportedException("The content stream cannot be mapped safely to editable elements.");
            }

            var candidate = candidates[candidateIndex];
            var expected = Kind(element);
            if (candidate.Kind != expected)
            {
                throw new NotSupportedException("The content stream contains an operator sequence that cannot be edited safely.");
            }

            var start = candidate.Start;
            var end = candidate.End;
            if (element is TextContent { SourceBytes: { } sourceBytes })
            {
                var combined = new List<byte>();
                while (candidateIndex < candidates.Count && candidates[candidateIndex].Kind == CandidateKind.Text)
                {
                    var part = candidates[candidateIndex++];
                    combined.AddRange(part.TextBytes);
                    end = part.End;
                    if (Same(combined, sourceBytes))
                    {
                        break;
                    }
                }

                if (!Same(combined, sourceBytes))
                {
                    throw new NotSupportedException("The text-show operators cannot be mapped safely to the materialized text run.");
                }
            }
            else
            {
                candidateIndex++;
            }

            result.Add(new SourceElement(element, start, end, Emit(element)));
        }

        if (candidateIndex != candidates.Count)
        {
            throw new NotSupportedException("The content stream contains painting operators that were not materialized safely.");
        }

        return result;
    }

    public static ContentEmissionResult Reemit(byte[] source, ContentCollection current, IReadOnlyList<SourceElement> original,
        string fontPrefix, string imagePrefix, string extGStatePrefix)
    {
        var byElement = new Dictionary<ContentElement, SourceElement>();
        foreach (var item in original)
        {
            byElement.Add(item.Element, item);
        }

        var surviving = new HashSet<ContentElement>();
        var insertsBefore = new Dictionary<ContentElement, List<ContentElement>>();
        var tail = new List<ContentElement>();
        SourceElement? previous = null;
        var pending = new List<ContentElement>();
        foreach (var element in current)
        {
            if (!byElement.TryGetValue(element, out var mapped))
            {
                pending.Add(element);
                continue;
            }

            if (previous is not null && mapped.Start <= previous.Start)
            {
                throw new NotSupportedException("Reordering materialized content is not supported. Remove and insert a new element instead.");
            }

            surviving.Add(element);
            insertsBefore[element] = [.. pending];
            pending.Clear();
            previous = mapped;
        }

        tail.AddRange(pending);
        using var writer = new ContentWriter(fontPrefix, imagePrefix, extGStatePrefix);
        var cursor = 0;
        foreach (var item in original)
        {
            writer.WriteBytes(source.AsSpan(cursor, item.Start - cursor));
            cursor = item.End;
            if (!surviving.Contains(item.Element))
            {
                ValidateRemoval(item.Element);
                continue;
            }

            foreach (var inserted in insertsBefore[item.Element])
            {
                inserted.Emit(writer);
            }

            var emitted = Emit(item.Element);
            if (Same(emitted, item.Snapshot))
            {
                writer.WriteBytes(source.AsSpan(item.Start, item.End - item.Start));
            }
            else
            {
                ValidateModification(item.Element);
                item.Element.Emit(writer);
            }
        }

        foreach (var inserted in tail)
        {
            inserted.Emit(writer);
        }

        writer.WriteBytes(source.AsSpan(cursor));
        return writer.DetachResult();
    }

    private static List<Candidate> Candidates(byte[] content)
    {
        var tokens = ContentTokenizer.Tokenize(content);
        var result = new List<Candidate>();
        var operands = new List<Token>();
        var array = new List<Token>();
        var frameStart = -1;
        var pathStart = -1;
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.Number or TokenKind.Name or TokenKind.String)
            {
                frameStart = frameStart < 0 ? token.Start : frameStart;
                operands.Add(token);
                continue;
            }

            if (token.Kind == TokenKind.ArrayStart)
            {
                frameStart = frameStart < 0 ? token.Start : frameStart;
                array.Clear();
                continue;
            }

            if (token.Kind == TokenKind.ArrayEnd)
            {
                array.AddRange(operands);
                operands.Clear();
                continue;
            }

            if (token.Kind != TokenKind.Operator)
            {
                continue;
            }

            var start = frameStart < 0 ? token.Start : frameStart;
            var op = token.Text!;
            if (op is "m" or "l" or "c" or "v" or "y" or "re" or "h" or "W" or "W*")
            {
                pathStart = pathStart < 0 ? start : pathStart;
            }
            else if (op is "S" or "s" or "f" or "F" or "f*" or "B" or "B*" or "b" or "b*" or "n")
            {
                if (pathStart >= 0)
                {
                    result.Add(new Candidate(CandidateKind.Path, pathStart, token.End, []));
                }

                pathStart = -1;
            }
            else if (op == "Do")
            {
                result.Add(new Candidate(CandidateKind.XObject, start, token.End, []));
            }
            else if (op is "Tj" or "TJ" or "'" or "\"")
            {
                var bytes = new List<byte>();
                foreach (var operand in op == "TJ" ? array : operands)
                {
                    if (operand.Kind == TokenKind.String && operand.Bytes is not null)
                    {
                        bytes.AddRange(operand.Bytes);
                    }
                }

                result.Add(new Candidate(CandidateKind.Text, start, token.End, [.. bytes]));
            }
            else if (!KnownNonElement(op))
            {
                result.Add(new Candidate(CandidateKind.Raw, start, token.End, []));
            }

            operands.Clear();
            array.Clear();
            frameStart = -1;
        }

        return result;
    }

    private static CandidateKind Kind(ContentElement element) => element switch
    {
        TextContent => CandidateKind.Text,
        PathContent => CandidateKind.Path,
        XObjectContent => CandidateKind.XObject,
        RawContent => CandidateKind.Raw,
        _ => throw new NotSupportedException($"Loaded content element '{element.GetType().Name}' cannot be mapped safely."),
    };

    private static bool KnownNonElement(string op) => op is
        "q" or "Q" or "cm" or "w" or "rg" or "RG" or "g" or "G" or "k" or "K"
        or "cs" or "CS" or "scn" or "sc" or "SCN" or "SC" or "d" or "BT" or "ET"
        or "Tf" or "TL" or "TD" or "Td" or "Tm" or "T*" or "Tc" or "Tw" or "Tz"
        or "Ts" or "Tr" or "BDC" or "BMC" or "EMC";

    private static void ValidateRemoval(ContentElement element)
    {
        if (element is RawContent)
        {
            throw new NotSupportedException("Removing an unmodeled content operator is not supported.");
        }

        if (element is PathContent { Clip: not PathClipMode.None })
        {
            throw new NotSupportedException("Removing a clipping path would change surrounding graphics state and is not supported.");
        }
    }

    private static void ValidateModification(ContentElement element)
    {
        if (element is RawContent or XObjectContent)
        {
            throw new NotSupportedException($"Modifying loaded {element.GetType().Name} is not supported.");
        }
    }

    private static byte[] Emit(ContentElement element)
    {
        using var writer = new ContentWriter();
        element.Emit(writer);
        return writer.ToArray();
    }

    private static bool Same(IReadOnlyList<byte> left, IReadOnlyList<byte> right)
    {
        if (left.Count != right.Count)
        {
            return false;
        }

        for (var i = 0; i < left.Count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }
}
