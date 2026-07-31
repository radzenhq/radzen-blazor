using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Output;
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

internal readonly record struct ResourceEntry(
    string Category,
    string Key,
    Func<DocumentObject> Value);

internal static class PageResourceBuilder
{
    public static DictionaryObject? BuildGeneratedResources(
        DocumentWriter writer,
        PageOutput page,
        Dictionary<OutputFont, DocumentObject> fontRefs,
        Dictionary<OutputImage, ReferenceObject> imageRefs,
        IReadOnlySet<string>? referencedKeys = null)
    {
        return BuildResourceDictionary(GeneratedEntries(writer, page, fontRefs, imageRefs), referencedKeys);
    }

    private static IEnumerable<ResourceEntry> GeneratedEntries(
        DocumentWriter writer,
        PageOutput page,
        Dictionary<OutputFont, DocumentObject> fontRefs,
        Dictionary<OutputImage, ReferenceObject> imageRefs)
    {
        foreach (var font in page.Fonts)
        {
            yield return new ResourceEntry("Font", font.Key, () => ResolveFont(writer, font, fontRefs));
        }

        foreach (var image in page.Images)
        {
            yield return new ResourceEntry("XObject", image.Key, () => ResolveImage(writer, image, image, imageRefs));
        }

        foreach (var state in page.ExtGStates)
        {
            yield return new ResourceEntry(
                "ExtGState",
                state.Key,
                () => ExtGStateDictionary(state.FillAlpha, state.StrokeAlpha, state.Blend, SoftMaskOf(writer, state)));
        }

        foreach (var pattern in page.Patterns)
        {
            yield return new ResourceEntry(
                "Pattern",
                pattern.Key,
                () => writer.Add(ShadingBuilder.BuildPattern(pattern.Gradient, pattern.Matrix)));
        }
    }

    private static DocumentObject? SoftMaskOf(DocumentWriter writer, OutputExtGState state)
    {
        if (state.SoftMask is { } mask)
        {
            return SoftMaskWriter.BuildDictionary(writer, mask);
        }

        return null;
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
        OutputFont font,
        Dictionary<OutputFont, DocumentObject> cache)
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

    private static ReferenceObject ResolveImage<TKey>(
        IObjectWriter writer,
        OutputImage image,
        TKey key,
        Dictionary<TKey, ReferenceObject>? cache)
        where TKey : notnull
    {
        if (cache is not null && cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var stream = ImageXObjectBuilder.Build(image.Image);
        if (image.SoftMask is { } mask)
        {
            stream.Dictionary["SMask"] = writer.Add(ImageXObjectBuilder.Build(mask));
        }

        var reference = writer.Add(stream);
        cache?.TryAdd(key, reference);
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
        return BuildResourceDictionary(ContentEntries(writer, manifest, sharedImages));
    }

    private static IEnumerable<ResourceEntry> ContentEntries(
        IObjectWriter? writer,
        ContentResourceManifest manifest,
        Dictionary<object, ReferenceObject>? sharedImages)
    {
        foreach (var font in manifest.Fonts)
        {
            yield return new ResourceEntry("Font", font.Value, () => Base14FontDictionary(font.Key));
        }

        foreach (var image in manifest.ImagesForWriting)
        {
            yield return new ResourceEntry(
                "XObject",
                image.Key,
                () => ResolveImage(writer!, image, image.Identity, sharedImages));
        }

        foreach (var pattern in manifest.Patterns)
        {
            yield return new ResourceEntry("Pattern", pattern.Key, () => writer!.Add(pattern.Value));
        }

        foreach (var state in manifest.ExtGStates)
        {
            yield return new ResourceEntry(
                "ExtGState",
                state.Key,
                () => ExtGStateDictionary(state.Value, state.Value));
        }
    }

    private static DictionaryObject? BuildResourceDictionary(
        IEnumerable<ResourceEntry> entries,
        IReadOnlySet<string>? referencedKeys = null)
    {
        var resources = new ResourceDictionaryBuilder();
        foreach (var entry in entries)
        {
            if (referencedKeys is null || referencedKeys.Contains(entry.Key))
            {
                resources.Add(entry.Category, entry.Key, entry.Value());
            }
        }

        return resources.Build();
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
}
