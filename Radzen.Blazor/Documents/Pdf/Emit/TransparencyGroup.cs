using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Emit;

// ISO 32000-1 11.6.6: transparency-group form XObject whose /Group declares /S /Transparency.
internal sealed class GeneratedTransparencyGroup
{
    public required byte[] Content { get; init; }

    public required double[] BBox { get; init; }

    public string? ColorSpace { get; init; }

    public bool? Isolated { get; init; }

    public bool? Knockout { get; init; }

    public IReadOnlyList<KeyValuePair<string, StreamObject>> XObjects { get; init; } = [];
}

internal static class TransparencyGroup
{
    public static StreamObject BuildForm(DocumentWriter writer, GeneratedTransparencyGroup group)
    {
        var stream = FlateFilter.EncodeStream(group.Content);
        var dict = stream.Dictionary;
        FormXObjectShell.ApplyHeader(
            dict,
            new ArrayObject
            {
                new NumberObject(group.BBox[0]),
                new NumberObject(group.BBox[1]),
                new NumberObject(group.BBox[2]),
                new NumberObject(group.BBox[3]),
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

        if (group.XObjects.Count > 0)
        {
            var xobjects = new DictionaryObject();
            foreach (var (name, xobject) in group.XObjects)
            {
                xobjects[name] = writer.Add(xobject);
            }

            dict["Resources"] = new DictionaryObject { ["XObject"] = xobjects };
        }

        return stream;
    }

    public static StreamObject GrayImage(byte[] samples, int width, int height)
        => ImageXObjectShell.FlateImage(samples, width, height, 8, new NameObject("DeviceGray"));
}
