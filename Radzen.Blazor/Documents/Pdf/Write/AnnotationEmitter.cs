using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Write;

internal sealed class AnnotationEmitContext
{
    public required IObjectWriter Writer { get; init; }

    public required Func<DocumentReader?, DocumentObject, DocumentObject> ImportValue { get; init; }

    public required IReadOnlyList<(Page Page, DictionaryObject Node, ReferenceObject Reference)> Pages { get; init; }

    public required int PageIndex { get; init; }

    public DocumentReader? Source { get; init; }
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
        DocumentReader? source,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
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
                ImportValue = (reader, value) => ImportValue(
                    writer, importer, source, appendImporters, reader, value),
                Pages = pages,
                PageIndex = pageIndex,
                Source = source,
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
        int pageIndex,
        DocumentReader source,
        Dictionary<DocumentReader, GraphImporter> appendImporters)
    {
        var context = new AnnotationEmitContext
        {
            Writer = writer,
            ImportValue = (reader, value) => ImportIncrementalValue(
                writer, source, appendImporters, reader, value),
            Pages = pages,
            PageIndex = pageIndex,
            Source = source,
        };
        return BuildEntries(annotations, context, (dictionary, entry) =>
            entry.Original is ReferenceObject original && ReferenceEquals(entry.Reader, source)
                ? writer.Override(original.ObjectNumber, dictionary)
                : writer.Add(dictionary));
    }

    private static ArrayObject Build(AnnotationCollection annotations, AnnotationEmitContext context)
        => BuildEntries(annotations, context, (dictionary, _) => context.Writer.Add(dictionary));

    private static ArrayObject BuildEntries(
        AnnotationCollection annotations,
        AnnotationEmitContext context,
        Func<DictionaryObject, AnnotationCollection.Entry, DocumentObject> writeModeled)
    {
        var result = new ArrayObject();
        foreach (var entry in annotations.Entries)
        {
            if (IsForeignPageTargetedLink(entry, context))
            {
                continue;
            }

            if (entry.Annotation is null || (entry.Original is not null && !entry.Annotation.IsModified))
            {
                result.Add(Import(entry, context));
                continue;
            }

            result.Add(writeModeled(BuildDictionary(entry.Annotation, entry, context), entry));
        }

        return result;
    }

    private static bool IsForeignPageTargetedLink(AnnotationCollection.Entry entry, AnnotationEmitContext context)
        => entry is { Original: not null, Annotation: LinkAnnotation { Uri: null, IsModified: false } link }
            && (link.Destination is not null || link.TargetPageIndex is not null)
            && !ReferenceEquals(entry.Reader, context.Source);

    private static DocumentObject Import(AnnotationCollection.Entry entry, AnnotationEmitContext context)
    {
        if (entry.Original is null)
        {
            throw new InvalidOperationException("A loaded annotation cannot be preserved without its source importer.");
        }

        return context.ImportValue(entry.Reader, entry.Original);
    }

    private static DictionaryObject BuildDictionary(
        Annotation annotation,
        AnnotationCollection.Entry entry,
        AnnotationEmitContext context)
    {
        AnnotationValidator.Validate(annotation);
        var dictionary = new DictionaryObject();
        if (entry.Dictionary is { } original)
        {
            foreach (var key in original.Keys)
            {
                if (!ManagedKeys.Contains(key))
                {
                    dictionary[key] = context.ImportValue(entry.Reader, original[key]);
                }
            }
        }

        dictionary["Type"] = new NameObject("Annot");
        dictionary["Subtype"] = new NameObject(annotation.Subtype);
        dictionary["Rect"] = PageResourceBuilder.NumberBox(annotation.Bounds);
        dictionary["C"] = PdfColorArray.Rgb(annotation.Color);
        dictionary["CA"] = new NumberObject(annotation.Opacity);
        dictionary["F"] = new NumberObject((int)annotation.Flags);
        dictionary["P"] = context.Pages[context.PageIndex].Reference;
        if (annotation.Contents is not null)
        {
            dictionary["Contents"] = StringObject.FromText(annotation.Contents);
        }

        if (annotation.Title is not null)
        {
            dictionary["T"] = StringObject.FromText(annotation.Title);
        }

        Populate(annotation, dictionary, context);
        var appearance = BuildAppearance(annotation, context.Writer, context.Pages[context.PageIndex].Page.FontScope);
        if (appearance is not null)
        {
            dictionary["AP"] = new DictionaryObject { ["N"] = context.Writer.Add(appearance) };
        }

        return dictionary;
    }

    private static DocumentObject ImportValue(
        DocumentWriter writer,
        GraphImporter? importer,
        DocumentReader? source,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
        DocumentReader? reader,
        DocumentObject value)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("A loaded annotation cannot be preserved without its source reader.");
        }

        if (ReferenceEquals(reader, source))
        {
            return importer!.ImportValue(value);
        }

        return GraphImporter.GetOrCreate(appendImporters, reader, writer).ImportValue(value);
    }

    private static DocumentObject ImportIncrementalValue(
        IncrementalUpdateWriter writer,
        DocumentReader source,
        Dictionary<DocumentReader, GraphImporter> appendImporters,
        DocumentReader? reader,
        DocumentObject value)
    {
        if (reader is null)
        {
            throw new InvalidOperationException("A loaded annotation cannot be preserved without its source reader.");
        }

        return ReferenceEquals(reader, source)
            ? value
            : GraphImporter.GetOrCreate(appendImporters, reader, writer).ImportValue(value);
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
                var quadPoints = new ArrayObject();
                foreach (var area in markup.Areas)
                {
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
                dictionary["Name"] = new NameObject(stamp.Name);
                break;
            case InkAnnotation ink:
                var inkList = new ArrayObject();
                foreach (var stroke in ink.Strokes)
                {
                    var points = new ArrayObject();
                    foreach (var point in stroke)
                    {
                        points.Add(new NumberObject(point.X));
                        points.Add(new NumberObject(point.Y));
                    }

                    inkList.Add(points);
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
                    dictionary["IC"] = PdfColorArray.Rgb(interior);
                }

                break;
        }
    }

    private static void PopulateLink(LinkAnnotation link, DictionaryObject dictionary, AnnotationEmitContext context)
    {
        if (link.Uri is { } uri)
        {
            dictionary["A"] = LinkAction.Uri(uri.OriginalString);
        }
        else if (link.Destination is { } destination)
        {
            dictionary["A"] = LinkAction.GoTo(link.DestinationIsName ? new NameObject(destination) : new StringObject(destination));
        }
        else if (link.TargetPageIndex is { } pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= context.Pages.Count)
            {
                throw new InvalidOperationException($"Link target page index {pageIndex} is out of range; the document has {context.Pages.Count} pages.");
            }

            var target = link.ResolvedTarget is { PageIndex: { } resolvedPage } resolved && resolvedPage == pageIndex
                ? resolved
                : OutlineTarget.ToPageFit(pageIndex);
            var page = context.Pages[pageIndex].Reference;
            dictionary["Dest"] = DestinationWriter.Write(target, page, [page, new NameObject("Fit")]);
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
        FormXObjectShell.ApplyHeader(
            stream.Dictionary,
            PageResourceBuilder.NumberBox(AppearanceBounds(annotation)),
            formType: true);
        var resources = PageResourceBuilder.BuildResources(writer, emitted.Resources) ?? new DictionaryObject();
        if (annotation.Opacity < 1)
        {
            var extGStates = resources.TryGetValue("ExtGState", out var existing) && existing is DictionaryObject dict
                ? dict
                : new DictionaryObject();
            extGStates["AGS"] = PageResourceBuilder.ExtGStateDictionary(annotation.Opacity, annotation.Opacity);
            resources["ExtGState"] = extGStates;
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

        var bounds = new PdfRectBounds();
        bounds.Include(0, 0);
        bounds.Include(annotation.Bounds.Width, annotation.Bounds.Height);
        foreach (var stroke in ink.Strokes)
        {
            foreach (var point in stroke)
            {
                if (!double.IsFinite(point.X) || !double.IsFinite(point.Y))
                {
                    throw new InvalidOperationException("Ink stroke points must have finite coordinates.");
                }

                bounds.Include(point.X - annotation.Bounds.Left, point.Y - annotation.Bounds.Bottom);
            }
        }

        return bounds.ToRect();
    }

    private static string DefaultAppearance(FreeTextAnnotation annotation)
        => DefaultAppearanceGrammar.Write(
            annotation.Font.EffectiveFamily,
            annotation.Font.EffectiveSize.Point,
            string.Create(CultureInfo.InvariantCulture, $"{annotation.TextColor.R / 255.0:0.###} {annotation.TextColor.G / 255.0:0.###} {annotation.TextColor.B / 255.0:0.###} rg"));
}

internal static class AnnotationValidator
{
    public static bool IsHidden(Annotation annotation) => (annotation.Flags & AnnotationFlags.Hidden) != 0;

    public static void Validate(Annotation annotation)
    {
        if (!annotation.Bounds.IsFiniteAndPositive)
        {
            throw new InvalidOperationException("Annotation bounds must be finite and have positive width and height.");
        }

        if (!double.IsFinite(annotation.Opacity) || annotation.Opacity < 0 || annotation.Opacity > 1)
        {
            throw new InvalidOperationException("Annotation opacity must be between 0 and 1.");
        }

        switch (annotation)
        {
            case MarkupAnnotation markup:
                if (markup.Areas.Count == 0)
                {
                    throw new InvalidOperationException($"A /{annotation.Subtype} annotation requires at least one markup area.");
                }

                foreach (var area in markup.Areas)
                {
                    ValidateMarkupArea(markup, area);
                }

                break;
            case StampAnnotation stamp:
                if (string.IsNullOrWhiteSpace(stamp.Name))
                {
                    throw new InvalidOperationException("A stamp annotation requires a non-empty name.");
                }

                break;
            case InkAnnotation ink:
                var strokeCount = 0;
                foreach (var stroke in ink.Strokes)
                {
                    if (stroke.Count < 2)
                    {
                        throw new InvalidOperationException("Every ink stroke requires at least two points.");
                    }

                    strokeCount++;
                }

                if (strokeCount == 0)
                {
                    throw new InvalidOperationException("An ink annotation requires at least one stroke.");
                }

                break;
            case LinkAnnotation link:
                var targets = (link.Uri is null ? 0 : 1)
                    + (link.Destination is null ? 0 : 1)
                    + (link.Destination is null && link.TargetPageIndex is not null ? 1 : 0);
                if (targets != 1)
                {
                    throw new InvalidOperationException("A link annotation requires exactly one URI, named destination or target page.");
                }

                break;
        }
    }

    private static void ValidateMarkupArea(MarkupAnnotation annotation, PdfRect area)
    {
        if (!area.IsFiniteAndPositive)
        {
            throw new InvalidOperationException("Markup areas must be finite and have positive width and height.");
        }

        var bounds = annotation.Bounds;
        if (area.Left < bounds.Left || area.Bottom < bounds.Bottom || area.Right > bounds.Right || area.Top > bounds.Top)
        {
            throw new InvalidOperationException("Markup areas must be contained within the annotation bounds.");
        }
    }
}
