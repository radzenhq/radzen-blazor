using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
namespace Radzen.Documents.Pdf.Emit;

internal sealed class ResourceDictionaryBuilder
{
    private DictionaryObject? resources;

    public void Add(string category, string key, DocumentObject value)
    {
        resources ??= new DictionaryObject();
        if (!resources.TryGetValue(category, out var existing) || existing is not DictionaryObject entries)
        {
            entries = new DictionaryObject();
            resources[category] = entries;
        }

        entries[key] = value;
    }

    public DictionaryObject? Build() => resources;
}

internal static class PageResourceBuilder
{
    public static DictionaryObject? BuildGeneratedResources(
        DocumentWriter writer,
        GeneratedPage page,
        Dictionary<GeneratedFont, DocumentObject> fontRefs,
        Dictionary<GeneratedImage, ReferenceObject> imageRefs,
        IReadOnlySet<string>? referencedKeys = null)
    {
        var resources = new ResourceDictionaryBuilder();

        foreach (var font in page.Fonts)
        {
            if (referencedKeys is not null && !referencedKeys.Contains(font.Key))
            {
                continue;
            }

            resources.Add("Font", font.Key, ResolveFont(writer, font, fontRefs));
        }

        foreach (var image in page.Images)
        {
            if (referencedKeys is not null && !referencedKeys.Contains(image.Key))
            {
                continue;
            }

            resources.Add("XObject", image.Key, ResolveImage(writer, image, imageRefs));
        }

        foreach (var state in page.ExtGStates)
        {
            if (referencedKeys is not null && !referencedKeys.Contains(state.Key))
            {
                continue;
            }

            DocumentObject? softMask = null;
            if (state.SoftMask is { } mask)
            {
                softMask = SoftMask.BuildDictionary(writer, mask);
            }
            else if (state.ClearSoftMask)
            {
                softMask = new NameObject("None");
            }

            resources.Add("ExtGState", state.Key, ExtGStateDictionary(
                state.FillAlpha,
                state.StrokeAlpha,
                state.Blend,
                state.OverprintStroke,
                state.OverprintFill,
                state.OverprintMode,
                state.Intent,
                softMask));
        }

        foreach (var pattern in page.Patterns)
        {
            if (referencedKeys is not null && !referencedKeys.Contains(pattern.Key))
            {
                continue;
            }

            resources.Add("Pattern", pattern.Key, writer.Add(ShadingBuilder.BuildPattern(pattern.Gradient)));
        }

        return resources.Build();
    }

    public static IReadOnlySet<string> ReferencedResourceKeys(byte[] content)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var token in Content.ContentTokenizer.Tokenize(content, new Content.ContentTokenizer.Cache()))
        {
            if (token.Kind == Content.ContentTokenizer.TokenKind.Name && token.Text is { } name)
            {
                keys.Add(name);
            }
        }

        return keys;
    }

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
            reference = Base14FontDictionary(font.Base14Name);
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

        var reference = WriteImage(writer, image.Image);
        cache[image] = reference;
        return reference;
    }

    private static ReferenceObject WriteImage(IObjectWriter writer, ImageXObject image)
    {
        if (image.SoftMask is { } mask)
        {
            image.Image.Dictionary["SMask"] = writer.Add(mask);
        }

        return writer.Add(image.Image);
    }

    public static DictionaryObject? OverlayResources(DocumentWriter writer, DictionaryObject? resources, ContentResourceManifest manifest)
    {
        var emitted = BuildResources(writer, manifest);
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

    // ISO 32000-1 9.6.6.4: Symbol and ZapfDingbats are symbolic with a built-in encoding; /WinAnsiEncoding would remap their glyphs.
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

    public static DictionaryObject? BuildResources(
        IObjectWriter writer,
        ContentResourceManifest manifest,
        Dictionary<ImageXObject, ReferenceObject>? sharedImages = null)
    {
        var resources = new ResourceDictionaryBuilder();

        foreach (var (baseFont, key) in manifest.Fonts)
        {
            resources.Add("Font", key, Base14FontDictionary(baseFont));
        }

        foreach (var (key, image) in manifest.Images)
        {
            resources.Add("XObject", key, ResolveManifestImage(writer, image, sharedImages));
        }

        foreach (var (key, pattern) in manifest.Patterns)
        {
            resources.Add("Pattern", key, writer.Add(pattern));
        }

        foreach (var (key, opacity) in manifest.ExtGStates)
        {
            resources.Add("ExtGState", key, ExtGStateDictionary(opacity, opacity));
        }

        return resources.Build();
    }

    private static ReferenceObject ResolveManifestImage(
        IObjectWriter writer,
        ImageXObject image,
        Dictionary<ImageXObject, ReferenceObject>? sharedImages)
    {
        if (sharedImages is not null && sharedImages.TryGetValue(image, out var existing))
        {
            return existing;
        }

        var reference = WriteImage(writer, image);
        sharedImages?.TryAdd(image, reference);
        return reference;
    }

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
                if (reader.AsDictionary(loaded[key]) is { } sub)
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

    public static HashSet<string> ResourceNames(DocumentReader reader, DictionaryObject resources)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        CollectResourceKeys(reader, resources, "Font", names);
        CollectResourceKeys(reader, resources, "XObject", names);
        CollectResourceKeys(reader, resources, "ExtGState", names);
        CollectResourceKeys(reader, resources, "Pattern", names);
        return names;
    }

    private static void CollectResourceKeys(DocumentReader reader, DictionaryObject resources, string category, HashSet<string> names)
    {
        if (reader.GetDictionary(resources, category) is { } dict)
        {
            foreach (var key in dict.Keys)
            {
                names.Add(key);
            }
        }
    }

    public static void EmitPageGeometry(Document document, Page page, DictionaryObject node)
    {
        node["MediaBox"] = MediaBox(document, page);

        var loaded = document.Loaded;
        if (page.CropBoxSet && page.CropBox is { } explicitCropBox)
        {
            node["CropBox"] = NumberBox(explicitCropBox);
        }
        else if (!page.CropBoxSet && loaded is not null && loaded.SourceCropBoxes.TryGetValue(page, out var cropBox))
        {
            node["CropBox"] = NumberBox(cropBox);
        }

        EmitAuxiliaryBox(document, node, page, "BleedBox", page.BleedBox);
        EmitAuxiliaryBox(document, node, page, "TrimBox", page.TrimBox);
        EmitAuxiliaryBox(document, node, page, "ArtBox", page.ArtBox);

        if (page.Rotate != 0)
        {
            node["Rotate"] = new NumberObject(page.Rotate);
        }
        else if (loaded is not null && loaded.SourceRotations.TryGetValue(page, out var rotation))
        {
            node["Rotate"] = new NumberObject(rotation);
        }
    }

    private static void EmitAuxiliaryBox(Document document, DictionaryObject node, Page page, string key, PdfRect? value)
    {
        if (value is not null)
        {
            PageBoxEmitter.WriteIfPresent(node, key, value);
            return;
        }

        if (document.Loaded?.Source is { } source && document.Loaded.SourcePages.TryGetValue(page, out var sourceNode)
            && source.GetArray(sourceNode, key) is { } box && box.Count >= 4)
        {
            node[key] = NumberBox(box);
        }
    }

    public static ArrayObject MediaBox(Document document, Page page)
    {
        if (page.MediaBoxSet)
        {
            return NumberBox(page.MediaBox);
        }

        if (document.Loaded is { } loaded && loaded.SourceBoxes.TryGetValue(page, out var box))
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

    public static ArrayObject NumberBox(PdfRect box) =>
    [
        new NumberObject(box.Left),
        new NumberObject(box.Bottom),
        new NumberObject(box.Right),
        new NumberObject(box.Top),
    ];
}
