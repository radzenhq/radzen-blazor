using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf;

// A transparency-group form XObject to be written on save (ISO 32000-1 11.6.6): a form
// XObject whose /Group dictionary declares /S /Transparency. Content is a raw content
// stream in page space; XObjects lists the named image/form resources it draws.
internal sealed class GeneratedTransparencyGroup
{
    public required byte[] Content { get; init; }

    // [x0 y0 x1 y1] form bounding box, in the coordinate space the content is written in.
    public required double[] BBox { get; init; }

    // Group colour space (/CS), e.g. DeviceGray for a luminosity mask group. Null omits it.
    public string? ColorSpace { get; init; }

    // /I (isolated) and /K (knockout); null omits the entry (defaulting to false per spec).
    public bool? Isolated { get; init; }

    public bool? Knockout { get; init; }

    public IReadOnlyList<KeyValuePair<string, StreamObject>> XObjects { get; init; } = [];
}

internal static class TransparencyGroup
{
    // Materializes the form XObject and its child resources into the writer, returning the
    // stream (added by the caller so the reference can be reused, e.g. as an SMask /G).
    public static StreamObject BuildForm(DocumentWriter writer, GeneratedTransparencyGroup group)
    {
        var stream = FlateFilter.EncodeStream(group.Content);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Form");
        dict["FormType"] = new NumberObject(1);
        dict["BBox"] = new ArrayObject
        {
            new NumberObject(group.BBox[0]),
            new NumberObject(group.BBox[1]),
            new NumberObject(group.BBox[2]),
            new NumberObject(group.BBox[3]),
        };

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

    // Builds an 8-bit DeviceGray image XObject from a raw luminance buffer, ready to be
    // drawn inside a luminosity-mask group.
    public static StreamObject GrayImage(byte[] samples, int width, int height)
    {
        var stream = new StreamObject(FlateFilter.Encode(samples));
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(width);
        dict["Height"] = new NumberObject(height);
        dict["ColorSpace"] = new NameObject("DeviceGray");
        dict["BitsPerComponent"] = new NumberObject(8);
        dict["Filter"] = new NameObject("FlateDecode");
        return stream;
    }
}
