using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Token = Radzen.Documents.Pdf.Content.ContentTokenizer.Token;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentEditor
{
    internal sealed record SourceElement(ContentElement Element, int Start, int End, Matrix Ambient);

    private readonly record struct Candidate(CandidateKind Kind, int Start, int End, byte[] TextBytes, Matrix Ambient);

    private enum CandidateKind
    {
        Text,
        Path,
        XObject,
        InlineImage,
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
                    if (Same(combined, sourceBytes.Span))
                    {
                        break;
                    }
                }

                if (!Same(combined, sourceBytes.Span))
                {
                    throw new NotSupportedException("The text-show operators cannot be mapped safely to the materialized text run.");
                }
            }
            else
            {
                candidateIndex++;
            }

            result.Add(new SourceElement(element, start, end, candidate.Ambient));
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

            if (!item.Element.IsModified)
            {
                writer.WriteBytes(source.AsSpan(item.Start, item.End - item.Start));
            }
            else
            {
                ValidateModification(item.Element);
                item.Element.Emit(writer, Relative(item.Element.Transform, item.Ambient));
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
        var ctm = Matrix.Identity;
        var ctmStack = new Stack<Matrix>();
        var pathCtm = Matrix.Identity;
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

            if (token.Kind == TokenKind.InlineImage)
            {
                result.Add(new Candidate(CandidateKind.InlineImage, token.Start, token.End, [], ctm));
                operands.Clear();
                array.Clear();
                frameStart = -1;
                continue;
            }

            if (token.Kind != TokenKind.Operator)
            {
                continue;
            }

            var start = frameStart < 0 ? token.Start : frameStart;
            var op = token.Text!;
            if (op == "q")
            {
                ctmStack.Push(ctm);
            }
            else if (op == "Q")
            {
                ctm = ctmStack.Count > 0 ? ctmStack.Pop() : Matrix.Identity;
            }
            else if (op == "cm")
            {
                ctm = Components(operands) * ctm;
            }

            if (op is "m" or "l" or "c" or "v" or "y" or "re" or "h" or "W" or "W*")
            {
                if (pathStart < 0)
                {
                    pathStart = start;
                    pathCtm = ctm;
                }
            }
            else if (op is "S" or "s" or "f" or "F" or "f*" or "B" or "B*" or "b" or "b*" or "n")
            {
                if (pathStart >= 0)
                {
                    result.Add(new Candidate(CandidateKind.Path, pathStart, token.End, [], pathCtm));
                }

                pathStart = -1;
            }
            else if (op == "Do")
            {
                result.Add(new Candidate(CandidateKind.XObject, start, token.End, [], ctm));
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

                result.Add(new Candidate(CandidateKind.Text, start, token.End, [.. bytes], ctm));
            }
            else if (!KnownNonElement(op))
            {
                result.Add(new Candidate(CandidateKind.Raw, start, token.End, [], ctm));
            }

            operands.Clear();
            array.Clear();
            frameStart = -1;
        }

        return result;
    }

    private static Matrix Components(List<Token> operands)
    {
        var numbers = new List<double>(6);
        foreach (var operand in operands)
        {
            if (operand.Kind == TokenKind.Number)
            {
                numbers.Add(operand.Number);
            }
        }

        if (numbers.Count < 6)
        {
            return Matrix.Identity;
        }

        var first = numbers.Count - 6;
        return Matrix.FromComponents(numbers[first], numbers[first + 1], numbers[first + 2],
            numbers[first + 3], numbers[first + 4], numbers[first + 5]);
    }

    // The interpreter bakes the absolute CTM into every element, but a re-emitted element is
    // spliced back where the source cm scope is still active, so its own cm has to undo it.
    private static Matrix Relative(Matrix transform, Matrix ambient)
    {
        if (ambient == Matrix.Identity)
        {
            return transform;
        }

        if (!ambient.TryInvert(out var inverse))
        {
            throw new NotSupportedException("Modifying content under a degenerate transformation matrix is not supported.");
        }

        return transform * inverse;
    }

    private static CandidateKind Kind(ContentElement element) => element switch
    {
        TextContent => CandidateKind.Text,
        PathContent => CandidateKind.Path,
        XObjectContent => CandidateKind.XObject,
        InlineImageContent => CandidateKind.InlineImage,
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

    // Reachable for every element type: these three declare no tracked member of their own but
    // still inherit the Transform and IsArtifact doors from ContentElement.
    private static void ValidateModification(ContentElement element)
    {
        if (element is RawContent or XObjectContent or InlineImageContent)
        {
            throw new NotSupportedException($"Modifying loaded {element.GetType().Name} is not supported.");
        }
    }

    private static bool Same(List<byte> left, ReadOnlySpan<byte> right)
        => CollectionsMarshal.AsSpan(left).SequenceEqual(right);
}
