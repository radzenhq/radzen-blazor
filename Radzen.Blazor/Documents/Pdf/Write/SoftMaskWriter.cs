using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Output;

namespace Radzen.Documents.Pdf.Write;

internal static class SoftMaskWriter
{
    public static DictionaryObject BuildDictionary(DocumentWriter writer, OutputSoftMask mask)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("Mask"),
            ["S"] = new NameObject("Luminosity"),
            ["G"] = writer.Add(TransparencyGroupWriter.BuildForm(writer, mask.Group)),
        };

        return dictionary;
    }
}
