using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Builds each page's /Resources and /MediaBox on save: registers generated fonts
// and image XObjects, materializes base-14 font dictionaries, and merges freshly
// emitted resources into the entries a loaded page already carried.
internal static class PageResourceBuilder
{
    public static DictionaryObject? BuildGeneratedResources(
        DocumentWriter writer,
        GeneratedPage page,
        Dictionary<GeneratedFont, DocumentObject> fontRefs,
        Dictionary<GeneratedImage, ReferenceObject> imageRefs)
    {
        DictionaryObject? fonts = null;
        foreach (var font in page.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[font.Key] = ResolveFont(writer, font, fontRefs);
        }

        DictionaryObject? xobjects = null;
        foreach (var image in page.Images)
        {
            xobjects ??= new DictionaryObject();
            xobjects[image.Key] = ResolveImage(writer, image, imageRefs);
        }

        DictionaryObject? extGStates = null;
        foreach (var state in page.ExtGStates)
        {
            extGStates ??= new DictionaryObject();
            DocumentObject? softMask = null;
            if (state.SoftMask is { } mask)
            {
                softMask = SoftMask.BuildDictionary(writer, mask);
            }
            else if (state.ClearSoftMask)
            {
                softMask = new NameObject("None");
            }

            extGStates[state.Key] = ExtGStateDictionary(
                state.FillAlpha,
                state.StrokeAlpha,
                state.Blend,
                state.OverprintStroke,
                state.OverprintFill,
                state.OverprintMode,
                state.Intent,
                softMask);
        }

        DictionaryObject? patterns = null;
        foreach (var pattern in page.Patterns)
        {
            patterns ??= new DictionaryObject();
            patterns[pattern.Key] = writer.Add(ShadingBuilder.BuildPattern(pattern.Gradient));
        }

        if (fonts is null && xobjects is null && extGStates is null && patterns is null)
        {
            return null;
        }

        var resources = new DictionaryObject();
        if (fonts is not null)
        {
            resources["Font"] = fonts;
        }

        if (xobjects is not null)
        {
            resources["XObject"] = xobjects;
        }

        if (extGStates is not null)
        {
            resources["ExtGState"] = extGStates;
        }

        if (patterns is not null)
        {
            resources["Pattern"] = patterns;
        }

        return resources;
    }

    // Builds an /ExtGState parameter dictionary. Alpha (/ca, /CA) is always present;
    // /BM (blend mode), /OP + /op + /OPM (overprint) and /RI (rendering intent) are
    // appended only when requested, so an alpha-only state stays Type/ca/CA verbatim.
    public static DictionaryObject ExtGStateDictionary(
        double fillAlpha,
        double strokeAlpha,
        BlendMode? blend = null,
        bool? overprintStroke = null,
        bool? overprintFill = null,
        int? overprintMode = null,
        RenderingIntent? intent = null,
        DocumentObject? softMask = null)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("ExtGState"),
            ["ca"] = new NumberObject(fillAlpha),
            ["CA"] = new NumberObject(strokeAlpha),
        };

        if (blend is { } mode)
        {
            dictionary["BM"] = new NameObject(mode.PdfName());
        }

        if (overprintStroke is { } stroke)
        {
            dictionary["OP"] = new BooleanObject(stroke);
        }

        if (overprintFill is { } fill)
        {
            dictionary["op"] = new BooleanObject(fill);
        }

        if (overprintMode is { } opm)
        {
            dictionary["OPM"] = new NumberObject(opm);
        }

        if (intent is { } ri)
        {
            dictionary["RI"] = new NameObject(ri.PdfName());
        }

        if (softMask is not null)
        {
            dictionary["SMask"] = softMask;
        }

        return dictionary;
    }

    private static DocumentObject ResolveFont(DocumentWriter writer, GeneratedFont font, Dictionary<GeneratedFont, DocumentObject> cache)
    {
        if (cache.TryGetValue(font, out var existing))
        {
            return existing;
        }

        DocumentObject reference;
        if (font.Sfnt is { } sfnt)
        {
            reference = Fonts.Type0FontEmbedder.Embed(writer, sfnt, font.GidToUnicode, font.CompactGidMap);
        }
        else
        {
            reference = Base14FontDictionary(font.Base14 ?? "Helvetica");
        }

        cache[font] = reference;
        return reference;
    }

    private static ReferenceObject ResolveImage(DocumentWriter writer, GeneratedImage image, Dictionary<GeneratedImage, ReferenceObject> cache)
    {
        if (cache.TryGetValue(image, out var existing))
        {
            return existing;
        }

        var xobject = image.Image;
        if (xobject.SoftMask is { } mask)
        {
            xobject.Image.Dictionary["SMask"] = writer.Add(mask);
        }

        var reference = writer.Add(xobject.Image);
        cache[image] = reference;
        return reference;
    }

    // Adds the fonts and image XObjects referenced by an overlay stream to a built
    // page's resources. Overlay keys use a distinct prefix so generated entries are
    // never clobbered.
    public static DictionaryObject? OverlayResources(DocumentWriter writer, DictionaryObject? resources, ContentWriter emitter)
    {
        var emitted = BuildResources(writer, emitter);
        if (emitted is null)
        {
            return resources;
        }

        resources ??= new DictionaryObject();
        foreach (var key in emitted.Keys)
        {
            if (resources.TryGetValue(key, out var existing) && existing is DictionaryObject target
                && emitted[key] is DictionaryObject added)
            {
                foreach (var name in added.Keys)
                {
                    target[name] = added[name];
                }
            }
            else
            {
                resources[key] = emitted[key];
            }
        }

        return resources;
    }

    // ISO 32000-1 9.6.6.4: Symbol and ZapfDingbats are symbolic and carry a built-in
    // encoding; declaring /Encoding /WinAnsiEncoding would remap their glyphs, so it is
    // omitted for them and kept for the non-symbolic base-14 faces.
    public static DictionaryObject Base14FontDictionary(string baseFont)
    {
        var dictionary = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type1"),
            ["BaseFont"] = new NameObject(baseFont),
        };

        if (baseFont is not ("Symbol" or "ZapfDingbats"))
        {
            dictionary["Encoding"] = new NameObject("WinAnsiEncoding");
        }

        return dictionary;
    }

    public static DictionaryObject? BuildResources(DocumentWriter writer, ContentWriter emitter)
    {
        DictionaryObject? fonts = null;
        foreach (var (baseFont, key) in emitter.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[key] = Base14FontDictionary(baseFont);
        }

        DictionaryObject? xobjects = null;
        foreach (var (key, image) in emitter.Images)
        {
            xobjects ??= new DictionaryObject();
            if (image.SoftMask is { } mask)
            {
                image.Image.Dictionary["SMask"] = writer.Add(mask);
            }

            xobjects[key] = writer.Add(image.Image);
        }

        DictionaryObject? patterns = null;
        foreach (var (key, pattern) in emitter.Patterns)
        {
            patterns ??= new DictionaryObject();
            patterns[key] = writer.Add(pattern);
        }

        if (fonts is null && xobjects is null && patterns is null)
        {
            return null;
        }

        var resources = new DictionaryObject();
        if (fonts is not null)
        {
            resources["Font"] = fonts;
        }

        if (xobjects is not null)
        {
            resources["XObject"] = xobjects;
        }

        if (patterns is not null)
        {
            resources["Pattern"] = patterns;
        }

        return resources;
    }

    // Imports the loaded page's effective /Resources into the writer and overlays
    // any newly emitted entries (emitter keys win on collision) so a re-save keeps
    // the source fonts, XObjects and graphics states.
    public static DictionaryObject MergeResources(
        GraphImporter importer,
        DocumentReader reader,
        DictionaryObject loaded,
        DictionaryObject? emitted)
    {
        var result = new DictionaryObject();
        foreach (var key in loaded.Keys)
        {
            result[key] = importer.ImportValue(loaded[key]);
        }

        if (emitted is null)
        {
            return result;
        }

        foreach (var key in emitted.Keys)
        {
            if (result.ContainsKey(key) && emitted[key] is DictionaryObject added)
            {
                var combined = new DictionaryObject();
                if (reader.Resolve(loaded[key]) is DictionaryObject sub)
                {
                    foreach (var name in sub.Keys)
                    {
                        combined[name] = importer.ImportValue(sub[name]);
                    }
                }

                foreach (var name in added.Keys)
                {
                    combined[name] = added[name];
                }

                result[key] = combined;
            }
            else
            {
                result[key] = emitted[key];
            }
        }

        return result;
    }

    // The /Font and /XObject names a loaded page already binds; a full re-emit must not
    // reuse any of them for a freshly registered base-14 face or image XObject.
    public static HashSet<string> ResourceNames(DocumentReader reader, DictionaryObject resources)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        CollectResourceKeys(reader, resources, "Font", names);
        CollectResourceKeys(reader, resources, "XObject", names);
        return names;
    }

    private static void CollectResourceKeys(DocumentReader reader, DictionaryObject resources, string category, HashSet<string> names)
    {
        if (resources.TryGetValue(category, out var value) && value is not null && reader.Resolve(value) is DictionaryObject dict)
        {
            foreach (var key in dict.Keys)
            {
                names.Add(key);
            }
        }
    }

    public static ArrayObject MediaBox(Document document, Page page)
    {
        // Re-emit a loaded page's original box so a non-zero origin round-trips;
        // content coordinates are preserved verbatim and would otherwise shift.
        if (document.sourceBoxes.TryGetValue(page, out var box))
        {
            return NumberBox(box);
        }

        return
        [
            new NumberObject(0.0),
            new NumberObject(0.0),
            new NumberObject(page.Width.Point),
            new NumberObject(page.Height.Point),
        ];
    }

    public static ArrayObject NumberBox(ArrayObject box) =>
    [
        new NumberObject(DocumentLoader.Number(box[0])),
        new NumberObject(DocumentLoader.Number(box[1])),
        new NumberObject(DocumentLoader.Number(box[2])),
        new NumberObject(DocumentLoader.Number(box[3])),
    ];
}
