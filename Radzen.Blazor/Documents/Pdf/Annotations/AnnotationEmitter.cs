using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class AnnotationEmitContext
{
    public required IObjectWriter Writer { get; init; }

    public required Func<DocumentObject, DocumentObject> ImportValue { get; init; }

    public required IReadOnlyList<(Page Page, DictionaryObject Node, ReferenceObject Reference)> Pages { get; init; }

    public required int PageIndex { get; init; }
}

internal static class AnnotationEmitter
{
    private static readonly HashSet<string> ManagedKeys = new(StringComparer.Ordinal)
    {
        "Type", "Subtype", "Rect", "C", "CA", "F", "Contents", "T", "P", "AP",
        "Open", "Name", "QuadPoints", "A", "Dest", "Border", "InkList", "BS", "IC", "DA",
    };

    public static void Write(
        DocumentWriter writer,
        GraphImporter? importer,
        IReadOnlyList<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pages)
    {
        for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
        {
            var (page, node, _) = pages[pageIndex];
            if (!page.Annotations.HasChanges)
            {
                continue;
            }

            var context = new AnnotationEmitContext
            {
                Writer = writer,
                ImportValue = importer is null
                    ? value => throw new InvalidOperationException("A loaded annotation cannot be preserved without its source importer.")
                    : importer.ImportValue,
                Pages = pages,
                PageIndex = pageIndex,
            };
            var emitted = Build(page.Annotations, context);
            if (!page.Annotations.WasLoaded && node.TryGetValue("Annots", out var current) && current is ArrayObject existing)
            {
                foreach (var annotation in emitted)
                {
                    existing.Add(annotation);
                }
            }
            else if (emitted.Count > 0)
            {
                node["Annots"] = emitted;
            }
            else if (page.Annotations.WasLoaded)
            {
                node["Annots"] = new NullObject();
            }
        }
    }

    public static ArrayObject BuildIncremental(
        IncrementalUpdateWriter writer,
        AnnotationCollection annotations,
        IReadOnlyList<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pages,
        int pageIndex)
    {
        var context = new AnnotationEmitContext
        {
            Writer = writer,
            ImportValue = static value => value,
            Pages = pages,
            PageIndex = pageIndex,
        };
        var result = new ArrayObject();
        foreach (var entry in annotations.Entries)
        {
            if (entry.Annotation is null
                || (entry.Original is not null && string.Equals(entry.State, entry.Annotation.State(), StringComparison.Ordinal)))
            {
                result.Add(Preserve(entry));
                continue;
            }

            var dictionary = BuildDictionary(entry.Annotation, entry, context);
            if (entry.Original is ReferenceObject original)
            {
                result.Add(writer.Override(original.ObjectNumber, dictionary));
            }
            else
            {
                result.Add(writer.Add(dictionary));
            }
        }

        return result;
    }

    private static ArrayObject Build(AnnotationCollection annotations, AnnotationEmitContext context)
    {
        var result = new ArrayObject();
        foreach (var entry in annotations.Entries)
        {
            if (entry.Annotation is null)
            {
                result.Add(Import(entry, context));
                continue;
            }

            if (entry.Original is not null && string.Equals(entry.State, entry.Annotation.State(), StringComparison.Ordinal))
            {
                result.Add(Import(entry, context));
                continue;
            }

            result.Add(context.Writer.Add(BuildDictionary(entry.Annotation, entry, context)));
        }

        return result;
    }

    private static DocumentObject Import(AnnotationCollection.Entry entry, AnnotationEmitContext context)
    {
        if (entry.Original is null)
        {
            throw new InvalidOperationException("A loaded annotation cannot be preserved without its source importer.");
        }

        return context.ImportValue(entry.Original);
    }

    private static DocumentObject Preserve(AnnotationCollection.Entry entry)
        => entry.Original ?? throw new InvalidOperationException("A loaded annotation has no original PDF object.");

    private static DictionaryObject BuildDictionary(
        Annotation annotation,
        AnnotationCollection.Entry entry,
        AnnotationEmitContext context)
    {
        Validate(annotation);
        var dictionary = new DictionaryObject();
        if (entry.Dictionary is { } original)
        {
            foreach (var key in original.Keys)
            {
                if (!ManagedKeys.Contains(key))
                {
                    dictionary[key] = context.ImportValue(original[key]);
                }
            }
        }

        dictionary["Type"] = new NameObject("Annot");
        dictionary["Subtype"] = new NameObject(annotation.Subtype);
        dictionary["Rect"] = RectArray(annotation.Bounds);
        dictionary["C"] = ColorArray(annotation.Color);
        dictionary["CA"] = new NumberObject(annotation.Opacity);
        dictionary["F"] = new NumberObject((int)annotation.Flags);
        dictionary["P"] = context.Pages[context.PageIndex].Reference;
        if (annotation.Contents is not null)
        {
            dictionary["Contents"] = new StringObject(annotation.Contents);
        }

        if (annotation.Title is not null)
        {
            dictionary["T"] = new StringObject(annotation.Title);
        }

        Populate(annotation, dictionary, context);
        var appearance = BuildAppearance(annotation, context.Writer, context.Pages[context.PageIndex].Page.FontScope);
        if (appearance is not null)
        {
            dictionary["AP"] = new DictionaryObject { ["N"] = context.Writer.Add(appearance) };
        }

        return dictionary;
    }

    private static void Populate(Annotation annotation, DictionaryObject dictionary, AnnotationEmitContext context)
    {
        switch (annotation)
        {
            case TextAnnotation text:
                dictionary["Open"] = new BooleanObject(text.Open);
                dictionary["Name"] = new NameObject(text.Icon);
                break;
            case MarkupAnnotation markup:
                if (markup.Areas.Count == 0)
                {
                    throw new InvalidOperationException($"A /{annotation.Subtype} annotation requires at least one markup area.");
                }

                var quadPoints = new ArrayObject();
                foreach (var area in markup.Areas)
                {
                    ValidateMarkupArea(markup, area);
                    quadPoints.Add(new NumberObject(area.Left));
                    quadPoints.Add(new NumberObject(area.Top));
                    quadPoints.Add(new NumberObject(area.Right));
                    quadPoints.Add(new NumberObject(area.Top));
                    quadPoints.Add(new NumberObject(area.Left));
                    quadPoints.Add(new NumberObject(area.Bottom));
                    quadPoints.Add(new NumberObject(area.Right));
                    quadPoints.Add(new NumberObject(area.Bottom));
                }

                dictionary["QuadPoints"] = quadPoints;
                break;
            case LinkAnnotation link:
                PopulateLink(link, dictionary, context);
                dictionary["Border"] = new ArrayObject { new NumberObject(0), new NumberObject(0), new NumberObject(0) };
                break;
            case StampAnnotation stamp:
                if (string.IsNullOrWhiteSpace(stamp.Name))
                {
                    throw new InvalidOperationException("A stamp annotation requires a non-empty name.");
                }

                dictionary["Name"] = new NameObject(stamp.Name);
                break;
            case InkAnnotation ink:
                var inkList = new ArrayObject();
                foreach (var stroke in ink.Strokes)
                {
                    if (stroke.Count < 2)
                    {
                        throw new InvalidOperationException("Every ink stroke requires at least two points.");
                    }

                    var points = new ArrayObject();
                    foreach (var point in stroke)
                    {
                        points.Add(new NumberObject(point.X));
                        points.Add(new NumberObject(point.Y));
                    }

                    inkList.Add(points);
                }

                if (inkList.Count == 0)
                {
                    throw new InvalidOperationException("An ink annotation requires at least one stroke.");
                }

                dictionary["InkList"] = inkList;
                dictionary["BS"] = new DictionaryObject { ["W"] = new NumberObject(ink.StrokeWidth) };
                break;
            case FreeTextAnnotation freeText:
                dictionary["DA"] = new StringObject(DefaultAppearance(freeText));
                break;
            case ShapeAnnotation shape:
                dictionary["BS"] = new DictionaryObject { ["W"] = new NumberObject(shape.BorderWidth) };
                if (shape.InteriorColor is { } interior)
                {
                    dictionary["IC"] = ColorArray(interior);
                }

                break;
        }
    }

    private static void PopulateLink(LinkAnnotation link, DictionaryObject dictionary, AnnotationEmitContext context)
    {
        var targets = (link.Uri is null ? 0 : 1) + (link.Destination is null ? 0 : 1) + (link.TargetPageIndex is null ? 0 : 1);
        if (targets != 1)
        {
            throw new InvalidOperationException("A link annotation requires exactly one URI, named destination or target page.");
        }

        if (link.Uri is { } uri)
        {
            dictionary["A"] = new DictionaryObject
            {
                ["S"] = new NameObject("URI"),
                ["URI"] = new StringObject(uri.OriginalString),
            };
        }
        else if (link.Destination is { } destination)
        {
            dictionary["A"] = new DictionaryObject
            {
                ["S"] = new NameObject("GoTo"),
                ["D"] = link.DestinationIsName ? new NameObject(destination) : new StringObject(destination),
            };
        }
        else if (link.TargetPageIndex is { } pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= context.Pages.Count)
            {
                throw new InvalidOperationException($"Link target page index {pageIndex} is out of range; the document has {context.Pages.Count} pages.");
            }

            dictionary["Dest"] = new ArrayObject { context.Pages[pageIndex].Reference, new NameObject("Fit") };
        }
    }

    private static StreamObject? BuildAppearance(Annotation annotation, IObjectWriter writer, Fonts.FontScope scope)
    {
        var elements = AnnotationAppearanceBuilder.Build(annotation);
        if (elements.Count == 0)
        {
            return null;
        }

        using var content = new ContentWriter(scope, "AF", "AIm");
        if (annotation.Opacity < 1)
        {
            content.WriteName("AGS");
            content.WriteRaw(" gs\n");
        }

        foreach (var element in elements)
        {
            element.Emit(content);
        }

        var emitted = content.DetachResult();
        var stream = new StreamObject(emitted.Bytes!);
        stream.Dictionary["Type"] = new NameObject("XObject");
        stream.Dictionary["Subtype"] = new NameObject("Form");
        stream.Dictionary["FormType"] = new NumberObject(1);
        stream.Dictionary["BBox"] = RectArray(AppearanceBounds(annotation));
        var resources = PageResourceBuilder.BuildResources(writer, emitted.Resources) ?? new DictionaryObject();
        if (annotation.Opacity < 1)
        {
            resources["ExtGState"] = new DictionaryObject
            {
                ["AGS"] = PageResourceBuilder.ExtGStateDictionary(annotation.Opacity, annotation.Opacity),
            };
        }

        if (resources.Count > 0)
        {
            stream.Dictionary["Resources"] = resources;
        }

        return stream;
    }

    private static PdfRect AppearanceBounds(Annotation annotation)
    {
        if (annotation is not InkAnnotation ink)
        {
            return PdfRect.FromSize(0, 0, annotation.Bounds.Width, annotation.Bounds.Height);
        }

        var minX = 0.0;
        var minY = 0.0;
        var maxX = annotation.Bounds.Width;
        var maxY = annotation.Bounds.Height;
        foreach (var stroke in ink.Strokes)
        {
            foreach (var point in stroke)
            {
                if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                {
                    throw new InvalidOperationException("Ink stroke points must have finite coordinates.");
                }

                var x = point.X - annotation.Bounds.Left;
                var y = point.Y - annotation.Bounds.Bottom;
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return new PdfRect(minX, minY, maxX, maxY);
    }

    private static void ValidateMarkupArea(MarkupAnnotation annotation, PdfRect area)
    {
        if (!double.IsFinite(area.Left) || !double.IsFinite(area.Bottom) || !double.IsFinite(area.Right)
            || !double.IsFinite(area.Top) || area.Width <= 0 || area.Height <= 0)
        {
            throw new InvalidOperationException("Markup areas must be finite and have positive width and height.");
        }

        var bounds = annotation.Bounds;
        if (area.Left < bounds.Left || area.Bottom < bounds.Bottom || area.Right > bounds.Right || area.Top > bounds.Top)
        {
            throw new InvalidOperationException("Markup areas must be contained within the annotation bounds.");
        }
    }

    private static void Validate(Annotation annotation)
    {
        var bounds = annotation.Bounds;
        if (!double.IsFinite(bounds.Left) || !double.IsFinite(bounds.Bottom) || !double.IsFinite(bounds.Right)
            || !double.IsFinite(bounds.Top) || bounds.Width <= 0 || bounds.Height <= 0)
        {
            throw new InvalidOperationException("Annotation bounds must be finite and have positive width and height.");
        }

        if (!double.IsFinite(annotation.Opacity) || annotation.Opacity < 0 || annotation.Opacity > 1)
        {
            throw new InvalidOperationException("Annotation opacity must be between 0 and 1.");
        }
    }

    private static ArrayObject RectArray(PdfRect bounds) =>
    [
        new NumberObject(bounds.Left),
        new NumberObject(bounds.Bottom),
        new NumberObject(bounds.Right),
        new NumberObject(bounds.Top),
    ];

    private static ArrayObject ColorArray(Color color) =>
    [
        new NumberObject(color.R / 255.0),
        new NumberObject(color.G / 255.0),
        new NumberObject(color.B / 255.0),
    ];

    private static string DefaultAppearance(FreeTextAnnotation annotation)
        => string.Create(CultureInfo.InvariantCulture, $"/{annotation.Font.Name} {annotation.Font.Size:0.###} Tf {annotation.TextColor.R / 255.0:0.###} {annotation.TextColor.G / 255.0:0.###} {annotation.TextColor.B / 255.0:0.###} rg");
}
