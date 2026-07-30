using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal static class PdfModelAdapter
{
    public static void Apply(in LaidOutDocumentInfo source, DocumentInfo target)
    {
        target.Title = source.Title;
        target.Author = source.Author;
        target.Subject = source.Subject;
        target.Keywords = source.Keywords;
        target.Creator = source.Creator;
        target.CreationDate = source.CreationDate;
        target.ModificationDate = source.ModificationDate;
    }
}
