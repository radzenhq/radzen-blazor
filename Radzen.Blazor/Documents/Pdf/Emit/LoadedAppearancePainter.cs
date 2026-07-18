using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

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
        string subject)
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

        var width = right - x0;
        var height = top - y0;
        if (width == 0 || height == 0)
        {
            return strict
                ? throw new DocumentParseException("An annotation appearance /BBox must have nonzero dimensions.", -1)
                : false;
        }

        var xobjects = PrivateXObjects(reader, loaded, page, owned);
        var name = namePrefix;
        while (xobjects.ContainsKey(name))
        {
            name += "z";
        }

        xobjects[name] = appearanceReference;
        var scaleX = target.Width / width;
        var scaleY = target.Height / height;
        page.Content.Add(new XObjectContent(name)
        {
            Transform = Matrix.FromComponents(
                scaleX, 0, 0, scaleY, target.Left - x0 * scaleX, target.Bottom - y0 * scaleY),
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

        var copy = new DictionaryObject();
        var xobjects = new DictionaryObject();
        if (resources is not null)
        {
            foreach (var key in resources.Keys)
            {
                copy[key] = resources[key];
            }

            if (reader.GetDictionary(resources, "XObject") is { } shared)
            {
                foreach (var key in shared.Keys)
                {
                    xobjects[key] = shared[key];
                }
            }
        }

        copy["XObject"] = xobjects;
        loaded.SourceResources[page] = copy;
        return xobjects;
    }
}
