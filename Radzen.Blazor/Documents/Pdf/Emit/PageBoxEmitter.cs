using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

internal static class PageBoxEmitter
{
    public static void WriteIfPresent(DictionaryObject node, string key, PdfRect? box)
    {
        if (box is { } rect)
        {
            node[key] = PageResourceBuilder.NumberBox(rect);
        }
    }
}
