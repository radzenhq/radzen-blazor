using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class AnnotationFlattener
{
    public static void Flatten(Document document)
    {
        foreach (var page in document.Pages)
        {
            foreach (var entry in page.Annotations.Entries)
            {
                if (entry.Annotation is not { } annotation)
                {
                    continue;
                }

                if (TryFlattenLoadedAppearance(document, page, entry, annotation))
                {
                    continue;
                }

                var appearance = AnnotationAppearanceBuilder.Build(annotation);
                if (appearance.Count > 0)
                {
                    page.Content.Add(new FlattenedAnnotationContent(appearance)
                    {
                        Transform = Matrix.Translate(annotation.Bounds.X, annotation.Bounds.Y),
                    });
                }
            }

            page.Annotations.Clear();
        }
    }

    private static bool TryFlattenLoadedAppearance(
        Document document,
        Page page,
        AnnotationCollection.Entry entry,
        Annotation annotation)
    {
        if (entry.Reader is not { } reader || entry.Dictionary is not { } dictionary
            || reader.GetDictionary(dictionary, "AP") is not { } appearances)
        {
            return false;
        }

        if (!appearances.TryGetValue("N", out var normal) || reader.AsStream(normal!) is not { } stream)
        {
            throw new NotSupportedException($"Cannot flatten a /{annotation.Subtype} annotation with a non-stream normal appearance.");
        }

        if (stream.Dictionary.TryGetValue("Matrix", out var matrixValue))
        {
            var matrix = reader.AsArray(matrixValue!);
            if (matrix is null || matrix.Count != 6 || !Identity(reader, matrix))
            {
                throw new NotSupportedException($"Cannot flatten a /{annotation.Subtype} annotation whose appearance has a non-identity matrix.");
            }
        }

        var box = reader.GetArray(stream.Dictionary, "BBox")
            ?? throw new NotSupportedException($"Cannot flatten a /{annotation.Subtype} annotation appearance without a /BBox.");
        if (box.Count != 4)
        {
            throw new DocumentParseException("An annotation appearance /BBox must contain four numbers.", -1);
        }

        var x0 = Number(reader, box[0]);
        var y0 = Number(reader, box[1]);
        var width = Number(reader, box[2]) - x0;
        var height = Number(reader, box[3]) - y0;
        if (width == 0 || height == 0)
        {
            throw new DocumentParseException("An annotation appearance /BBox must have nonzero dimensions.", -1);
        }

        if (document.Loaded is not { } loaded)
        {
            throw new InvalidOperationException("A loaded annotation appearance has no loaded document state.");
        }

        if (!loaded.SourceResources.TryGetValue(page, out var resources))
        {
            resources = new DictionaryObject();
            loaded.SourceResources[page] = resources;
        }

        var xobjects = reader.GetDictionary(resources, "XObject");
        if (xobjects is null)
        {
            xobjects = new DictionaryObject();
            resources["XObject"] = xobjects;
        }

        var name = "AFlatten";
        while (xobjects.ContainsKey(name))
        {
            name += "z";
        }

        xobjects[name] = normal!;
        var scaleX = annotation.Bounds.Width / width;
        var scaleY = annotation.Bounds.Height / height;
        page.Content.Add(new XObjectContent(name)
        {
            Transform = Matrix.FromComponents(
                scaleX,
                0,
                0,
                scaleY,
                annotation.Bounds.X - x0 * scaleX,
                annotation.Bounds.Y - y0 * scaleY),
        });
        return true;
    }

    private static bool Identity(DocumentReader reader, ArrayObject matrix)
        => Number(reader, matrix[0]) == 1 && Number(reader, matrix[1]) == 0
            && Number(reader, matrix[2]) == 0 && Number(reader, matrix[3]) == 1
            && Number(reader, matrix[4]) == 0 && Number(reader, matrix[5]) == 0;

    private static double Number(DocumentReader reader, DocumentObject value)
        => reader.AsNumber(value)
            ?? throw new DocumentParseException("An annotation appearance coordinate is not numeric.", -1);

    private sealed class FlattenedAnnotationContent(IReadOnlyList<ContentElement> elements) : ContentElement
    {
        protected override void EmitBody(ContentWriter writer)
        {
            foreach (var element in elements)
            {
                element.Emit(writer);
            }
        }
    }
}
