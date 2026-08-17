using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Content;

internal static class ContentEditor
{
    internal sealed record SourceElement(ContentElement Element, int Start, int End, Matrix Ambient, bool InsideTextObject);

    public static ContentEmissionResult Reemit(byte[] source, ContentCollection current, IReadOnlyList<SourceElement> original,
        Fonts.FontScope scope, ContentResourcePrefixes prefixes, System.Collections.Generic.IEnumerable<string>? reserved,
        ImageDecoders? decoders = null)
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
        using var writer = new ContentWriter(scope, prefixes, reserved, decoders);
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
        if (element is PathContent { Clip: not PathClipMode.None })
        {
            throw new NotSupportedException("Modifying a clipping path would change surrounding graphics state and is not supported.");
        }

        if (element is TextContent { SourceShowOperator: "'" or "\"" } text)
        {
            throw new NotSupportedException($"Modifying text from the '{text.SourceShowOperator}' show operator is not supported safely.");
        }

        if (element is RawContent or XObjectContent or InlineImageContent)
        {
            throw new NotSupportedException($"Modifying loaded {element.GetType().Name} is not supported.");
        }
    }
}
