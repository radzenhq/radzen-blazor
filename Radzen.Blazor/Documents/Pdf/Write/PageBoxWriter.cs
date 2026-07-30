using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class PageBoxWriter
{
    public static void WriteIfPresent(DictionaryObject node, string key, PdfRect? box)
    {
        if (box is { } rect)
        {
            node[key] = NumberBox(rect);
        }
    }

    public static void WriteExplicitBoxes(DictionaryObject node, Page page)
    {
        if (page.MediaBoxSet)
        {
            node["MediaBox"] = NumberBox(page.MediaBox);
        }

        if (page.CropBoxSet && page.CropBox is { } cropBox)
        {
            node["CropBox"] = NumberBox(cropBox);
        }
    }

    public static void EmitPageGeometry(PortableDocument document, Page page, DictionaryObject node)
    {
        node["MediaBox"] = MediaBox(document, page);

        var loaded = document.Loaded;
        if (page.CropBoxSet && page.CropBox is { } explicitCropBox)
        {
            node["CropBox"] = NumberBox(explicitCropBox);
        }
        else if (!page.CropBoxSet && loaded is not null && loaded.SourceCropBoxes.TryGetValue(page, out var cropBox))
        {
            node["CropBox"] = NumberBox(cropBox);
        }

        EmitAuxiliaryBox(document, node, page, "BleedBox", page.BleedBox);
        EmitAuxiliaryBox(document, node, page, "TrimBox", page.TrimBox);
        EmitAuxiliaryBox(document, node, page, "ArtBox", page.ArtBox);

        if (page.Rotate != 0)
        {
            node["Rotate"] = new NumberObject(page.Rotate);
        }
        else if (loaded is not null && loaded.SourceRotations.TryGetValue(page, out var rotation))
        {
            node["Rotate"] = new NumberObject(rotation);
        }
    }

    private static void EmitAuxiliaryBox(PortableDocument document, DictionaryObject node, Page page, string key, PdfRect? value)
    {
        if (value is not null)
        {
            WriteIfPresent(node, key, value);
            return;
        }

        if (document.Loaded?.Source is { } source && document.Loaded.SourcePages.TryGetValue(page, out var sourceNode)
            && source.GetArray(sourceNode, key) is { } box && box.Count >= 4)
        {
            node[key] = NumberBox(box);
        }
    }

    public static ArrayObject MediaBox(PortableDocument document, Page page)
    {
        if (page.MediaBoxSet)
        {
            return NumberBox(page.MediaBox);
        }

        if (document.Loaded is { } loaded && loaded.SourceBoxes.TryGetValue(page, out var box))
        {
            return NumberBox(box);
        }

        return
        [
            new NumberObject(0.0),
            new NumberObject(0.0),
            new NumberObject(page.Width.Point),
            new NumberObject(page.Height.Point),
        ];
    }

    public static ArrayObject NumberBox(ArrayObject box) =>
    [
        new NumberObject(DocumentLoader.Number(box[0])),
        new NumberObject(DocumentLoader.Number(box[1])),
        new NumberObject(DocumentLoader.Number(box[2])),
        new NumberObject(DocumentLoader.Number(box[3])),
    ];

    public static ArrayObject NumberBox(PdfRect box) =>
    [
        new NumberObject(box.Left),
        new NumberObject(box.Bottom),
        new NumberObject(box.Right),
        new NumberObject(box.Top),
    ];
}
