using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal static class PdfModelAdapter
{
    public static Font Materialize(in FontPaint paint)
        => new()
        {
            Family = paint.Family,
            Size = paint.Size,
            Bold = paint.Bold,
            Italic = paint.Italic,
            Underline = paint.Underline,
            Strikethrough = paint.Strikethrough,
            Color = paint.Color,
        };

    public static void Apply(in CapturedDocumentInfo source, DocumentInfo target)
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
