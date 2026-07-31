using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class PageAnnotationImporter
{
    public static ArrayObject Import(GraphImporter importer, ArrayObject annots)
    {
        var imported = new ArrayObject();
        foreach (var annot in annots)
        {
            imported.Add(importer.ImportValue(annot));
        }

        return imported;
    }

    public static ArrayObject ImportWidgets(
        GraphImporter importer,
        DocumentReader reader,
        ArrayObject annots)
    {
        var imported = new ArrayObject();
        foreach (var annot in annots)
        {
            if (reader.AsDictionary(annot) is { } dictionary
                && string.Equals(reader.GetName(dictionary, "Subtype"), "Widget", System.StringComparison.Ordinal))
            {
                imported.Add(importer.ImportValue(annot));
            }
        }

        return imported;
    }
}
