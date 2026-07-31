using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Write;

internal static class AppearanceStreamBuilder
{
    public static StreamObject Render(
        FontScope scope,
        ContentResourcePrefixes prefixes,
        ArrayObject boundingBox,
        bool formType,
        IEnumerable<ContentElement> elements,
        IObjectWriter? writer = null,
        string? prologue = null,
        string? epilogue = null,
        Action<DictionaryObject>? augmentResources = null,
        Action<ContentResourceManifest>? validateResources = null)
    {
        using var content = new ContentWriter(scope, prefixes);
        if (prologue is not null)
        {
            content.WriteRaw(prologue);
        }

        foreach (var element in elements)
        {
            element.Emit(content);
        }

        if (epilogue is not null)
        {
            content.WriteRaw(epilogue);
        }

        var emitted = content.DetachResult();
        validateResources?.Invoke(emitted.Resources);
        var stream = new StreamObject(emitted.Bytes!);
        FormXObjectBuilder.ApplyHeader(stream.Dictionary, boundingBox, formType);

        var resources = PageResourceBuilder.BuildResources(writer, emitted.Resources) ?? new DictionaryObject();
        augmentResources?.Invoke(resources);
        if (resources.Count > 0)
        {
            stream.Dictionary["Resources"] = resources;
        }

        return stream;
    }
}
