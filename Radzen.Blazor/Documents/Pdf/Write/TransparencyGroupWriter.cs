using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Radzen.Documents.Pdf.Emission;

namespace Radzen.Documents.Pdf.Write;

internal static class TransparencyGroupWriter
{
    public static StreamObject BuildForm(DocumentWriter writer, EmissionTransparencyGroup group)
    {
        var stream = FlateFilter.EncodeStream(group.Content.Span);
        var dict = stream.Dictionary;
        FormXObjectShell.ApplyHeader(
            dict,
            new ArrayObject
            {
                new NumberObject(group.BoundingBox[0]),
                new NumberObject(group.BoundingBox[1]),
                new NumberObject(group.BoundingBox[2]),
                new NumberObject(group.BoundingBox[3]),
            },
            formType: true);

        var groupDict = new DictionaryObject { ["S"] = new NameObject("Transparency") };
        if (group.ColorSpace is { } cs)
        {
            groupDict["CS"] = new NameObject(cs);
        }

        if (group.Isolated is { } isolated)
        {
            groupDict["I"] = new BooleanObject(isolated);
        }

        if (group.Knockout is { } knockout)
        {
            groupDict["K"] = new BooleanObject(knockout);
        }

        dict["Group"] = groupDict;
        if (group.XObjects.Length > 0)
        {
            var xobjects = new DictionaryObject();
            foreach (var (name, xobject) in group.XObjects)
            {
                xobjects[name] = writer.Add(xobject.CreateStream());
            }

            dict["Resources"] = new DictionaryObject { ["XObject"] = xobjects };
        }

        return stream;
    }
}
