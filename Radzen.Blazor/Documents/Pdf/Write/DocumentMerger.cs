namespace Radzen.Documents.Pdf.Write;

internal static class DocumentMerger
{
    internal static void Append(PortableDocument target, PortableDocument other)
    {
        var pageOffset = target.Pages.Count;
        foreach (var source in other.Pages)
        {
            AppendPage(target, other, source, 0, other.Pages.Count, pageOffset);
        }
    }

    internal static Page AppendPage(
        PortableDocument target,
        PortableDocument other,
        Page source,
        int sourcePageOffset,
        int sourcePageCount,
        int targetPageOffset)
    {
        var origin = other.Loaded;
        var page = new Page(source.Width, source.Height);
        if (source.MediaBoxSet)
        {
            page.MediaBox = source.MediaBox;
        }
        else
        {
            page.SetPreservedMediaBox(source.MediaBox);
        }

        if (source.CropBoxSet)
        {
            page.CropBox = source.CropBox;
        }
        else if (source.CropBox is { } preservedCropBox)
        {
            page.SetPreservedCropBox(preservedCropBox);
        }

        page.SetLoadedRotate(source.Rotate);
        page.BleedBox = source.BleedBox;
        page.TrimBox = source.TrimBox;
        page.ArtBox = source.ArtBox;
        if (source.RawContent is { } content)
        {
            page.SetLoadedContent([.. content]);
        }

        if (source.Annotations.WasLoaded)
        {
            page.Annotations.RewriteImported();
        }

        foreach (var entry in source.Annotations.Entries)
        {
            if (entry.Annotation is null)
            {
                page.Annotations.Load(null, entry.Reader!, entry.Original!, entry.Dictionary);
                continue;
            }

            if (AnnotationCloner.Clone(
                entry.Annotation, sourcePageOffset, sourcePageCount, targetPageOffset) is not { } clone)
            {
                continue;
            }

            if (!entry.Annotation.IsModified && entry.Original is not null && PreserveLoadedVerbatim(entry.Annotation))
            {
                page.Annotations.Load(clone, entry.Reader!, entry.Original!, entry.Dictionary);
            }
            else
            {
                page.Annotations.Add(clone);
            }
        }

        if (source.OutputIdentity is { } generated)
        {
            page.OutputIdentity = generated;
            if (source.TextFonts is { } generatedFonts)
            {
                page.SetTextFonts(generatedFonts);
            }
        }
        else if (origin?.Source is { } reader && origin.SourceResources.TryGetValue(source, out var loadedResources))
        {
            target.EnsureLoaded().RecordAppendedResources(page, reader, loadedResources);
            page.SetTextFonts(DocumentLoader.BuildTextFonts(reader, loadedResources));
            page.SetReservedResourceNames(PageResourceBuilder.ResourceNames(reader, loadedResources));
        }
        else if (origin is not null && origin.AppendedResources.TryGetValue(source, out var appended))
        {
            target.EnsureLoaded().RecordAppendedResources(page, appended.Reader, appended.Resources);
            page.SetTextFonts(DocumentLoader.BuildTextFonts(appended.Reader, appended.Resources));
            page.SetReservedResourceNames(PageResourceBuilder.ResourceNames(appended.Reader, appended.Resources));
        }

        if (origin is not null)
        {
            target.EnsureLoaded().CarryAppended(page, source, origin);
        }

        target.Pages.Insert(target.Pages.Count, page);
        return page;
    }

    private static bool PreserveLoadedVerbatim(Annotation annotation)
        => annotation is not LinkAnnotation
            || annotation is LinkAnnotation { Destination: null, TargetPageIndex: null };
}
