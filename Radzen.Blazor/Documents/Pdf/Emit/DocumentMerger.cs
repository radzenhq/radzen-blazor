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
            var page = new Page(source.Width, source.Height);
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
            else if (other.source is not null && other.sourceResources.TryGetValue(source, out var loadedResources))
            {
                target.appendedResources[page] = (other.source, loadedResources);
                page.SetTextFonts(DocumentLoader.BuildTextFonts(other.source, loadedResources));
            }

            // A loaded appended page keeps a handle to its source node so its
            // /Annots (and any widget-annotation form fields) survive the copy.
            if (other.source is not null && other.sourcePages.TryGetValue(source, out var sourceNode))
            {
                target.appendedPages[page] = (other.source, sourceNode);
                if (other.sourceAcroForm is not null)
                {
                    target.appendedAcroForms[other.source] = other.sourceAcroForm;
                }
            }

            if (other.sourceBoxes.TryGetValue(source, out var box))
            {
                target.sourceBoxes[page] = box;
            }

            // Carry the appended page's crop box and rotation too; serialization reads
            // these dictionaries, so without the copy a merged loaded page loses its
            // /CropBox and /Rotate. Also carries through a chained append, since a
            // document produced by Append records the same entries under its own keys.
            if (other.sourceCropBoxes.TryGetValue(source, out var cropBox))
            {
                target.sourceCropBoxes[page] = cropBox;
            }

            if (other.sourceRotations.TryGetValue(source, out var rotation))
            {
                target.sourceRotations[page] = rotation;
            }

            target.Pages.Insert(target.Pages.Count, page);
        }
    }
}
