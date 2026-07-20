using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal static class AnnotationFlattener
{
    public static void Flatten(Document document)
    {
        var owned = new HashSet<Page>();
        foreach (var page in document.Pages)
        {
            foreach (var entry in page.Annotations.Entries)
            {
                if (entry.Annotation is not { } annotation)
                {
                    continue;
                }

                if (AnnotationValidator.IsHidden(annotation))
                {
                    continue;
                }

                if (TryFlattenLoadedAppearance(document, page, entry, annotation, owned))
                {
                    continue;
                }

                if (entry.Original is null)
                {
                    AnnotationValidator.Validate(annotation);
                }

                var appearance = AnnotationAppearanceBuilder.Build(annotation);
                if (appearance.Count > 0)
                {
                    page.Content.Add(new FlattenedAnnotationContent(appearance)
                    {
                        Transform = Matrix.Translate(annotation.Bounds.Left, annotation.Bounds.Bottom),
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
        Annotation annotation,
        HashSet<Page> owned)
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

        if (document.Loaded is not { } loaded)
        {
            throw new InvalidOperationException("A loaded annotation appearance has no loaded document state.");
        }

        return LoadedAppearancePainter.TryPaint(
            reader, loaded, page, owned, normal!, stream, annotation.Bounds, "AFlatten",
            strict: true, subject: $"/{annotation.Subtype} annotation");
    }

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
