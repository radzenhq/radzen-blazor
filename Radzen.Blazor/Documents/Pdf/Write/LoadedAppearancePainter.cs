using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal static class LoadedAppearancePainter
{
    public static bool TryPaint(
        DocumentReader reader,
        LoadedState loaded,
        Page page,
        HashSet<Page> owned,
        DocumentObject appearanceReference,
        StreamObject appearance,
        PdfRect target,
        string namePrefix,
        bool strict,
        string subject,
        double opacity = 1)
    {
        if (!MatrixIsIdentity(reader, appearance, strict, subject))
        {
            return false;
        }

        if (reader.GetArray(appearance.Dictionary, "BBox") is not { } box)
        {
            return strict
                ? throw new NotSupportedException($"Cannot flatten a {subject} appearance without a /BBox.")
                : false;
        }

        if (box.Count != 4)
        {
            return strict
                ? throw new DocumentParseException("An annotation appearance /BBox must contain four numbers.", -1)
                : false;
        }

        if (!TryNumber(reader, box[0], strict, out var x0)
            || !TryNumber(reader, box[1], strict, out var y0)
            || !TryNumber(reader, box[2], strict, out var right)
            || !TryNumber(reader, box[3], strict, out var top))
        {
            return false;
        }

        var bbox = PdfRect.Normalize([x0, y0, right, top]);
        if (bbox.Width == 0 || bbox.Height == 0)
        {
            return strict
                ? throw new DocumentParseException("An annotation appearance /BBox must have nonzero dimensions.", -1)
                : false;
        }

        var xobjects = PrivateXObjects(reader, loaded, page, owned);
        var name = ResourceNameAllocator.Available(namePrefix, xobjects.Keys, false);

        xobjects[name] = appearanceReference;
        var scaleX = target.Width / bbox.Width;
        var scaleY = target.Height / bbox.Height;
        page.Content.Add(new XObjectContent(name)
        {
            Transform = Matrix.FromRawComponents(
                scaleX, 0, 0, scaleY, target.Left - bbox.Left * scaleX, target.Bottom - bbox.Bottom * scaleY),
            Opacity = opacity < 1 ? opacity : null,
        });
        return true;
    }

    private static bool MatrixIsIdentity(DocumentReader reader, StreamObject appearance, bool strict, string subject)
    {
        if (!appearance.Dictionary.TryGetValue("Matrix", out var value))
        {
            return true;
        }

        if (reader.AsArray(value!) is { Count: 6 } matrix
            && reader.AsNumber(matrix[0]) == 1 && reader.AsNumber(matrix[1]) == 0
            && reader.AsNumber(matrix[2]) == 0 && reader.AsNumber(matrix[3]) == 1
            && reader.AsNumber(matrix[4]) == 0 && reader.AsNumber(matrix[5]) == 0)
        {
            return true;
        }

        return strict
            ? throw new NotSupportedException($"Cannot flatten a {subject} whose appearance has a non-identity matrix.")
            : false;
    }

    private static bool TryNumber(DocumentReader reader, DocumentObject value, bool strict, out double number)
    {
        if (reader.AsNumber(value) is { } resolved)
        {
            number = resolved;
            return true;
        }

        number = 0;
        return strict
            ? throw new DocumentParseException("An annotation appearance coordinate is not numeric.", -1)
            : false;
    }

    private static DictionaryObject PrivateXObjects(DocumentReader reader, LoadedState loaded, Page page, HashSet<Page> owned)
    {
        loaded.SourceResources.TryGetValue(page, out var resources);
        if (!owned.Add(page))
        {
            return (DictionaryObject)resources!["XObject"]!;
        }

        var copy = resources?.Copy() ?? new DictionaryObject();
        var xobjects = resources is not null && reader.GetDictionary(resources, "XObject") is { } shared
            ? shared.Copy()
            : new DictionaryObject();

        copy["XObject"] = xobjects;
        loaded.SourceResources[page] = copy;
        return xobjects;
    }
}
