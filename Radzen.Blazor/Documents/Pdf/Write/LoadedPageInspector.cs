using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal sealed class LoadedPageInspector(DocumentReader reader, string label)
{
    private readonly HashSet<DictionaryObject> visited = new(ReferenceEqualityComparer.Instance);

    public void Inspect(DictionaryObject resources) => InspectResources(resources, 0);

    private void InspectResources(DictionaryObject resources, int depth)
    {
        if (depth > reader.Limits.MaxPageTreeDepth || !visited.Add(resources))
        {
            return;
        }

        if (reader.GetDictionary(resources, "Font") is { } fonts)
        {
            foreach (var key in fonts.Keys)
            {
                if (reader.AsDictionary(fonts[key]) is { } font)
                {
                    InspectFont(font);
                }
            }
        }

        if (reader.GetDictionary(resources, "XObject") is { } xObjects)
        {
            foreach (var key in xObjects.Keys)
            {
                if (reader.AsStream(xObjects[key]) is not { } xObject)
                {
                    continue;
                }

                if (reader.GetName(xObject.Dictionary, "Subtype") == "Form")
                {
                    if (reader.GetDictionary(xObject.Dictionary, "Resources") is { } nested)
                    {
                        InspectResources(nested, depth + 1);
                    }

                    continue;
                }

                InspectImage(xObject.Dictionary);
            }
        }
    }

    // ISO 19005-2 6.2.11.4.1: every font used to render text must be embedded. Type 3 fonts carry
    // their glyphs as content streams in /CharProcs and have no font file.
    private void InspectFont(DictionaryObject font)
    {
        var subtype = reader.GetName(font, "Subtype");
        if (subtype is "Type3")
        {
            return;
        }

        if (subtype is "Type0")
        {
            if (reader.GetArray(font, "DescendantFonts") is { Count: > 0 } descendants
                && reader.AsDictionary(descendants[0]) is { } descendant)
            {
                RequireFontFile(descendant, reader.GetName(font, "BaseFont"));
            }

            return;
        }

        RequireFontFile(font, reader.GetName(font, "BaseFont"));
    }

    private void RequireFontFile(DictionaryObject font, string? baseFont)
    {
        if (reader.GetDictionary(font, "FontDescriptor") is { } descriptor
            && (descriptor.ContainsKey("FontFile")
                || descriptor.ContainsKey("FontFile2")
                || descriptor.ContainsKey("FontFile3")))
        {
            return;
        }

        throw new InvalidOperationException(
            $"{label} requires every font to be embedded, and the loaded page references "
            + $"'{baseFont ?? "an unnamed font"}' without an embedded font file. Rebuild the page with "
            + "DocumentRenderer and registered fonts, or save without conformance "
            + "(PdfAConformance.None and PdfUaConformance.None).");
    }

    // ISO 19005-2 6.2.4.3: DeviceCMYK requires a CMYK output intent, and this library writes an sRGB one.
    private void InspectImage(DictionaryObject image)
    {
        if (reader.GetName(image, "ColorSpace") == "DeviceCMYK")
        {
            throw new InvalidOperationException(
                $"{label} pairs a DeviceCMYK image on a loaded page with an sRGB output intent, which ISO 19005 "
                + "forbids; convert the image to RGB or grayscale, or save without conformance "
                + "(PdfAConformance.None and PdfUaConformance.None).");
        }
    }
}
