using Radzen.Documents.Pdf.Objects;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal sealed class LoadedState
{
    public Dictionary<Page, DictionaryObject> SourcePages { get; } = [];

    public List<Page> LoadedPages { get; } = [];

    public Dictionary<Page, DictionaryObject> SourceResources { get; } = [];

    public Dictionary<Page, ArrayObject> SourceBoxes { get; } = [];

    public Dictionary<Page, ArrayObject> SourceCropBoxes { get; } = [];

    public Dictionary<Page, int> SourceRotations { get; } = [];

    public Dictionary<Page, (PdfRect? Bleed, PdfRect? Trim, PdfRect? Art, int Rotate)> LoadedPageSettings { get; } = [];

    public Dictionary<Page, (DocumentReader Reader, DictionaryObject Resources)> AppendedResources { get; } = [];

    public Dictionary<Page, (DocumentReader Reader, DictionaryObject Node)> AppendedPages { get; } = [];

    public Dictionary<DocumentReader, DictionaryObject> AppendedAcroForms { get; } = [];

    public DocumentReader? Source { get; }

    public DictionaryObject? SourceCatalog { get; set; }

    public DictionaryObject? SourceAcroForm { get; set; }

    public byte[]? SourceBytes { get; }

    public DictionaryObject? SourceInfo { get; set; }

    public bool OutlineRequiresRewrite { get; set; }

    public LoadedState()
    {
    }

    public LoadedState(DocumentReader source, byte[] sourceBytes)
    {
        Source = source;
        SourceBytes = sourceBytes;
    }

    public LoadedState(DocumentReader source)
    {
        Source = source;
    }

    public void RecordAppendedResources(Page page, DocumentReader reader, DictionaryObject resources)
        => AppendedResources[page] = (reader, resources);

    public void CarryAppended(Page page, Page source, LoadedState origin)
    {
        if (origin.Source is { } reader && origin.SourcePages.TryGetValue(source, out var sourceNode))
        {
            AppendedPages[page] = (reader, sourceNode);
            if (origin.SourceAcroForm is { } form)
            {
                AppendedAcroForms[reader] = form;
            }
        }
        else if (origin.AppendedPages.TryGetValue(source, out var appendedNode))
        {
            AppendedPages[page] = appendedNode;
            if (origin.AppendedAcroForms.TryGetValue(appendedNode.Reader, out var appendedForm))
            {
                AppendedAcroForms[appendedNode.Reader] = appendedForm;
            }
        }

        if (origin.SourceBoxes.TryGetValue(source, out var box))
        {
            SourceBoxes[page] = box;
        }

        if (origin.SourceCropBoxes.TryGetValue(source, out var cropBox))
        {
            SourceCropBoxes[page] = cropBox;
        }

        if (origin.SourceRotations.TryGetValue(source, out var rotation))
        {
            SourceRotations[page] = rotation;
        }
    }

    public void CarryForeign(Page page, LoadedState origin)
    {
        if (origin.Source is { } reader && origin.SourceResources.TryGetValue(page, out var resources))
        {
            RecordAppendedResources(page, reader, resources);
        }
        else if (origin.AppendedResources.TryGetValue(page, out var appendedResources))
        {
            RecordAppendedResources(page, appendedResources.Reader, appendedResources.Resources);
        }

        CarryAppended(page, page, origin);

        if (page.Annotations.WasLoaded)
        {
            page.Annotations.RewriteImported();
        }
    }

    public void ClearAcroForm() => SourceAcroForm = null;
}
