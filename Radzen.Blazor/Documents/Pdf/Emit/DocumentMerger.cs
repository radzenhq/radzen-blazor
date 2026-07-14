namespace Radzen.Documents.Pdf.Emit;

// Appends the pages of one document onto another, carrying each source page's
// resource closure, annotations, boxes and rotation. Extracted from Document to
// keep the page/metadata model separate from the import/merge orchestration; the
// state it populates still lives on the target Document, read on save by the
// feature writers. Behavior and populated state are identical to the previous
// inline implementation.
internal static class DocumentMerger
{
    internal static void Append(Document target, Document other)
    {
        foreach (var source in other.Pages)
        {
            AppendPage(target, other, source);
        }
    }

    internal static Page AppendPage(Document target, Document other, Page source)
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
        var content = source.GetContent();
        if (content is not null)
        {
            page.SetContent([.. content]);
        }

        // Carry the source page's resource closure so appended fonts/images still
        // resolve: a built page keeps its GeneratedPage; a loaded page keeps a
        // handle to its reader and effective /Resources, imported lazily on save.
        if (source.Generated is { } generated)
        {
            page.Generated = generated;
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

        // Carry each appended page's source node (for its /Annots and any widget
        // form fields), donor AcroForm, and its media/crop boxes and rotation, so a
        // merged loaded page re-serializes identically. Delegates to the target's
        // own LoadedState rather than reaching into raw dictionaries.
        if (origin is not null)
        {
            target.EnsureLoaded().CarryAppended(page, source, origin);
        }

        target.Pages.Insert(target.Pages.Count, page);
        return page;
    }
}
