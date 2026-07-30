using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Render;

namespace Radzen.Documents.Pdf.Write;

internal static class SoftMaskWriter
{
    public static DictionaryObject BuildDictionary(DocumentWriter writer, GeneratedSoftMask mask)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("Mask"),
            ["S"] = new NameObject(mask.Type == SoftMaskType.Luminosity ? "Luminosity" : "Alpha"),
            ["G"] = writer.Add(TransparencyGroupWriter.BuildForm(writer, mask.Group)),
        };

        if (mask.Backdrop is { } backdrop)
        {
            var bc = new ArrayObject();
            foreach (var component in backdrop)
            {
                bc.Add(new NumberObject(component));
            }

            dictionary["BC"] = bc;
        }

        return dictionary;
    }
}
