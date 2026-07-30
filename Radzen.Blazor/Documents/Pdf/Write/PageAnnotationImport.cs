using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class PageAnnotationImport
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
}
