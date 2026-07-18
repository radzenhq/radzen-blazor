namespace Radzen.Documents.Pdf.Emit;

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

        page.SetLoadedRotate(source.Rotate);
        page.BleedBox = source.BleedBox;
        page.TrimBox = source.TrimBox;
        page.ArtBox = source.ArtBox;
        if (source.RawContent is { } content)
        {
            page.SetLoadedContent([.. content]);
        }

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

        if (origin is not null)
        {
            target.EnsureLoaded().CarryAppended(page, source, origin);
        }

        target.Pages.Insert(target.Pages.Count, page);
        return page;
    }
}
