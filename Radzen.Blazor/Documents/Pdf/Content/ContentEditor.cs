using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static Radzen.Documents.Pdf.Content.ContentOperands;
using TokenKind = Radzen.Documents.Pdf.Content.ContentTokenizer.TokenKind;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentEditor
{
    internal sealed record SourceElement(ContentElement Element, int Start, int End, Matrix Ambient, bool InsideTextObject);

    private readonly record struct Candidate(CandidateKind Kind, int Start, int End, byte[] TextBytes, Matrix Ambient, bool InsideTextObject = false);

    private enum CandidateKind
    {
        Text,
        Path,
        XObject,
        InlineImage,
        Raw,
    }

    public static IReadOnlyList<SourceElement> Map(byte[] content, ContentCollection elements, ContentTokenizer.Cache? cache = null)
    {
        var candidates = Candidates(content, cache);
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

            result.Add(new SourceElement(element, start, end, candidate.Ambient, candidate.InsideTextObject));
        }

        if (candidateIndex != candidates.Count)
        {
            throw new NotSupportedException("The content stream contains painting operators that were not materialized safely.");
        }

        return result;
    }

    public static ContentEmissionResult Reemit(byte[] source, ContentCollection current, IReadOnlyList<SourceElement> original,
        Fonts.FontScope scope, string fontPrefix, string imagePrefix, string extGStatePrefix, string patternPrefix)
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
        using var writer = new ContentWriter(scope, fontPrefix, imagePrefix, extGStatePrefix, patternPrefix);
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

            var spliced = false;
            foreach (var inserted in insertsBefore[item.Element])
            {
                writer.EnsureSeparated();
                inserted.Emit(writer);
                spliced = true;
            }

            if (!item.Element.IsModified)
            {
                if (spliced)
                {
                    writer.EnsureSeparated();
                }

                writer.WriteBytes(source.AsSpan(item.Start, item.End - item.Start));
            }
            else
            {
                ValidateModification(item.Element);
                if (item.Element is TextContent run)
                {
                    run.InsideTextObject = item.InsideTextObject;
                }

                writer.EnsureSeparated();
                item.Element.Emit(writer, Relative(item.Element.Transform, item.Ambient));
            }
        }

        var appended = false;
        foreach (var inserted in tail)
        {
            writer.EnsureSeparated();
            inserted.Emit(writer);
            appended = true;
        }

        if (appended)
        {
            writer.EnsureSeparated();
        }

        writer.WriteBytes(source.AsSpan(cursor));
        return writer.DetachResult();
    }

    private static List<Candidate> Candidates(byte[] content, ContentTokenizer.Cache? cache)
    {
        var tokens = ContentTokenizer.Tokenize(content, cache);
        var result = new List<Candidate>();
        var pathStart = -1;
        var clipPending = false;
        var machine = new ContentStateMachine();
        var pathCtm = Matrix.Identity;
        foreach (var frame in ContentOperandScan.Scan(tokens))
        {
            if (frame.IsInlineImage)
            {
                result.Add(new Candidate(CandidateKind.InlineImage, frame.InlineImage.Start, frame.InlineImage.End, [], machine.Ctm));
                continue;
            }

            var op = frame.Operator.Text!;
            var start = frame.FrameStart < 0 ? frame.Operator.Start : frame.FrameStart;

            machine.Apply(op, frame.Operands);

            if (ContentOperatorClass.IsPathConstruction(op))
            {
                if (pathStart < 0)
                {
                    pathStart = start;
                    pathCtm = machine.Ctm;
                }

                clipPending |= ContentOperatorClass.IsClip(op);
            }
            else if (ContentOperatorClass.IsPathPainting(op))
            {
                if (pathStart >= 0 && (op != "n" || clipPending))
                {
                    result.Add(new Candidate(CandidateKind.Path, pathStart, frame.Operator.End, [], pathCtm));
                }

                pathStart = -1;
                clipPending = false;
            }
            else if (op == "Do")
            {
                result.Add(new Candidate(CandidateKind.XObject, start, frame.Operator.End, [], machine.Ctm));
            }
            else if (ContentShows.IsShow(op))
            {
                if (op == "TJ" || LastStringToken(frame.Operands) is not null)
                {
                    var bytes = new List<byte>();
                    foreach (var operand in op == "TJ" ? frame.Array : frame.Operands)
                    {
                        if (operand.Kind == TokenKind.String && operand.Bytes is not null)
                        {
                            bytes.AddRange(operand.Bytes);
                        }
                    }

                    var insideText = machine.TextObjectDepth > 0;
                    var ambient = insideText ? machine.TextMatrix * machine.Ctm : machine.Ctm;
                    result.Add(new Candidate(CandidateKind.Text, start, frame.Operator.End, [.. bytes], ambient, insideText));
                }
            }
            else if (!ContentOperatorClass.IsStateOperator(op))
            {
                result.Add(new Candidate(CandidateKind.Raw, start, frame.Operator.End, [], machine.Ctm));
            }
        }

        return result;
    }

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
        if (element is RawContent or XObjectContent or InlineImageContent)
        {
            throw new NotSupportedException($"Modifying loaded {element.GetType().Name} is not supported.");
        }
    }

    private static bool Same(List<byte> left, ReadOnlySpan<byte> right)
        => CollectionsMarshal.AsSpan(left).SequenceEqual(right);
}
