using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal static class LoadedAppearancePainter
{
    private enum AppearanceRejection
    {
        None,
        NonIdentityMatrix,
        MissingBBox,
        BBoxIsNotFourNumbers,
        BBoxCoordinateIsNotNumeric,
        BBoxIsDegenerate,
    }

    public static bool PaintOrThrow(
        DocumentReader reader,
        LoadedState loaded,
        Page page,
        HashSet<Page> owned,
        DocumentObject appearanceReference,
        StreamObject appearance,
        PdfRect target,
        string namePrefix,
        string subject,
        double opacity = 1)
    {
        if (Paint(reader, loaded, page, owned, appearanceReference, appearance, target, namePrefix, opacity, out var rejection))
        {
            return true;
        }

        throw Rejected(rejection, subject);
    }

    public static bool TryPaint(
        DocumentReader reader,
        LoadedState loaded,
        Page page,
        HashSet<Page> owned,
        DocumentObject appearanceReference,
        StreamObject appearance,
        PdfRect target,
        string namePrefix,
        double opacity = 1)
        => Paint(reader, loaded, page, owned, appearanceReference, appearance, target, namePrefix, opacity, out _);

    private static Exception Rejected(AppearanceRejection rejection, string subject)
        => rejection switch
        {
            AppearanceRejection.NonIdentityMatrix => new NotSupportedException(
                $"Cannot flatten a {subject} whose appearance has a non-identity matrix."),
            AppearanceRejection.MissingBBox => new NotSupportedException(
                $"Cannot flatten a {subject} appearance without a /BBox."),
            AppearanceRejection.BBoxIsNotFourNumbers => new DocumentParseException(
                "An annotation appearance /BBox must contain four numbers.", -1),
            AppearanceRejection.BBoxCoordinateIsNotNumeric => new DocumentParseException(
                "An annotation appearance coordinate is not numeric.", -1),
            AppearanceRejection.BBoxIsDegenerate => new DocumentParseException(
                "An annotation appearance /BBox must have nonzero dimensions.", -1),
            _ => new InvalidOperationException($"A {subject} appearance was painted without a rejection."),
        };

    private static bool Paint(
        DocumentReader reader,
        LoadedState loaded,
        Page page,
        HashSet<Page> owned,
        DocumentObject appearanceReference,
        StreamObject appearance,
        PdfRect target,
        string namePrefix,
        double opacity,
        out AppearanceRejection rejection)
    {
        rejection = AppearanceRejection.None;
        if (!MatrixIsIdentity(reader, appearance))
        {
            rejection = AppearanceRejection.NonIdentityMatrix;
            return false;
        }

        if (reader.GetArray(appearance.Dictionary, "BBox") is not { } box)
        {
            rejection = AppearanceRejection.MissingBBox;
            return false;
        }

        if (box.Count != 4)
        {
            rejection = AppearanceRejection.BBoxIsNotFourNumbers;
            return false;
        }

        if (!TryNumber(reader, box[0], out var x0)
            || !TryNumber(reader, box[1], out var y0)
            || !TryNumber(reader, box[2], out var right)
            || !TryNumber(reader, box[3], out var top))
        {
            rejection = AppearanceRejection.BBoxCoordinateIsNotNumeric;
            return false;
        }

        var bbox = PdfRect.Normalize([x0, y0, right, top]);
        if (bbox.Width == 0 || bbox.Height == 0)
        {
            rejection = AppearanceRejection.BBoxIsDegenerate;
            return false;
        }

        var xobjects = PrivateXObjects(reader, loaded, page, owned);
        var name = ResourceNameAllocator.Reserve(namePrefix, xobjects.Keys);

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

    private static bool MatrixIsIdentity(DocumentReader reader, StreamObject appearance)
    {
        if (!appearance.Dictionary.TryGetValue("Matrix", out var value))
        {
            return true;
        }

        return reader.AsArray(value!) is { Count: 6 } matrix
            && reader.AsNumber(matrix[0]) == 1 && reader.AsNumber(matrix[1]) == 0
            && reader.AsNumber(matrix[2]) == 0 && reader.AsNumber(matrix[3]) == 1
            && reader.AsNumber(matrix[4]) == 0 && reader.AsNumber(matrix[5]) == 0;
    }

    private static bool TryNumber(DocumentReader reader, DocumentObject value, out double number)
    {
        if (reader.AsNumber(value) is { } resolved)
        {
            number = resolved;
            return true;
        }

        number = 0;
        return false;
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
