using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emission;
namespace Radzen.Documents.Pdf.Write;

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
        PageEmissionPlan page,
        Dictionary<EmissionFont, DocumentObject> fontRefs,
        Dictionary<EmissionImage, ReferenceObject> imageRefs,
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
                softMask = SoftMaskWriter.BuildDictionary(writer, mask);
            }
            else if (state.ClearSoftMask)
            {
                softMask = new NameObject("None");
            }

            resources.Add("ExtGState", state.Key, ExtGStateDictionary(
                state.FillAlpha,
                state.StrokeAlpha,
                state.Blend,
                softMask));
        }

        foreach (var pattern in page.Patterns)
        {
            if (referencedKeys is not null && !referencedKeys.Contains(pattern.Key))
            {
                continue;
            }

            resources.Add("Pattern", pattern.Key, writer.Add(ShadingBuilder.BuildPattern(pattern.Gradient, pattern.Matrix)));
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

        if (softMask is not null)
        {
            dictionary["SMask"] = softMask;
        }

        return dictionary;
    }

    private static DocumentObject ResolveFont(
        DocumentWriter writer,
        EmissionFont font,
        Dictionary<EmissionFont, DocumentObject> cache)
    {
        if (cache.TryGetValue(font, out var existing))
        {
            return existing;
        }

        DocumentObject reference = font.Program is { } program
            ? Fonts.Type0FontEmbedder.Embed(writer, program)
            : Base14FontDictionary(font.Base14Name);

        cache[font] = reference;
        return reference;
    }

    private static ReferenceObject ResolveImage(
        DocumentWriter writer,
        EmissionImage image,
        Dictionary<EmissionImage, ReferenceObject> cache)
    {
        if (cache.TryGetValue(image, out var existing))
        {
            return existing;
        }

        var stream = image.Image.CreateStream();
        if (image.SoftMask is { } mask)
        {
            stream.Dictionary["SMask"] = writer.Add(mask.CreateStream());
        }

        var reference = writer.Add(stream);
        cache[image] = reference;
        return reference;
    }

    public static DictionaryObject? OverlayResources(DocumentWriter writer, DictionaryObject? resources, ContentResourceManifest manifest)
    {
        var emitted = BuildResources(writer, manifest);
        return emitted is null
            ? resources
            : MergeResourceCategories(resources ?? new DictionaryObject(), emitted, static value => value, static value => value as DictionaryObject);
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
        IObjectWriter? writer,
        ContentResourceManifest manifest,
        Dictionary<object, ReferenceObject>? sharedImages = null)
    {
        var resources = new ResourceDictionaryBuilder();

        foreach (var (baseFont, key) in manifest.Fonts)
        {
            resources.Add("Font", key, Base14FontDictionary(baseFont));
        }

        foreach (var image in manifest.ImagesForWriting)
        {
            resources.Add("XObject", image.Key, ResolveManifestImage(writer!, image, sharedImages));
        }

        foreach (var (key, pattern) in manifest.Patterns)
        {
            resources.Add("Pattern", key, writer!.Add(pattern));
        }

        foreach (var (key, opacity) in manifest.ExtGStates)
        {
            resources.Add("ExtGState", key, ExtGStateDictionary(opacity, opacity));
        }

        return resources.Build();
    }

    private static ReferenceObject ResolveManifestImage(
        IObjectWriter writer,
        EmissionImage image,
        Dictionary<object, ReferenceObject>? sharedImages)
    {
        if (sharedImages is not null && sharedImages.TryGetValue(image.Identity, out var existing))
        {
            return existing;
        }

        var stream = image.Image.CreateStream();
        if (image.SoftMask is { } mask)
        {
            stream.Dictionary["SMask"] = writer.Add(mask.CreateStream());
        }

        var reference = writer.Add(stream);
        sharedImages?.TryAdd(image.Identity, reference);
        return reference;
    }

    public static DictionaryObject MergeResources(
        GraphImporter importer,
        DocumentReader reader,
        DictionaryObject loaded,
        DictionaryObject? emitted)
        => MergeResourceCategories(loaded, emitted, importer.ImportValue, reader.AsDictionary);

    private static DictionaryObject MergeResourceCategories(
        DictionaryObject baseResources,
        DictionaryObject? emitted,
        Func<DocumentObject, DocumentObject> copy,
        Func<DocumentObject, DictionaryObject?> asDictionary)
    {
        var result = new DictionaryObject();
        foreach (var key in baseResources.Keys)
        {
            result[key] = copy(baseResources[key]);
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
                if (asDictionary(baseResources[key]) is { } sub)
                {
                    foreach (var name in sub.Keys)
                    {
                        combined[name] = copy(sub[name]);
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

    public static void EmitPageGeometry(PortableDocument document, Page page, DictionaryObject node)
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

    private static void EmitAuxiliaryBox(PortableDocument document, DictionaryObject node, Page page, string key, PdfRect? value)
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

    public static ArrayObject MediaBox(PortableDocument document, Page page)
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
