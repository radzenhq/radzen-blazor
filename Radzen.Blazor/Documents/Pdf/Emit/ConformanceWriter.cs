using Radzen.Documents.Pdf.Objects;
using System;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ConformanceWriter(Document document)
{
    private static (int Part, string Conformance) Identification(PdfAConformance level) => level switch
    {
        PdfAConformance.PdfA2B => (2, "B"),
        PdfAConformance.PdfA2A => (2, "A"),
        PdfAConformance.PdfA3B => (3, "B"),
        PdfAConformance.PdfA3A => (3, "A"),
        PdfAConformance.PdfA4 => (4, ""),
        PdfAConformance.PdfA4E => (4, "E"),
        PdfAConformance.PdfA4F => (4, "F"),
        _ => throw new InvalidOperationException($"Unsupported PDF/A conformance level '{level}'."),
    };

    private static bool IsLevelA(PdfAConformance level)
        => level is PdfAConformance.PdfA2A or PdfAConformance.PdfA3A;

    private string Label => document.Conformance != PdfAConformance.None ? "PDF/A" : "PDF/UA";

    public void ValidateConformance()
    {
        if (document.Conformance != PdfAConformance.None)
        {
            ValidatePdfA();
        }

        if (document.PdfUA && document.Structure is null)
        {
            throw new InvalidOperationException(
                "PDF/UA requires Tagged PDF logical structure; the document has no structure tree. Build the document with DocumentBuilder.");
        }

        ValidateInspectable();

        ValidateFonts();

        if (document.PdfUA && string.IsNullOrEmpty(document.Language))
        {
            throw new InvalidOperationException(
                "PDF/UA requires the document's natural language to be determinable; set DocumentBuilder.Language (e.g. \"en-US\").");
        }

        if (document.PdfUA && string.IsNullOrEmpty(document.Info.Title))
        {
            throw new InvalidOperationException(
                "PDF/UA requires a document title displayed by the viewer (DisplayDocTitle); set DocumentBuilder.Info.Title.");
        }

        ValidateTagging();
    }

    // Font embedding: ISO 19005-2 6.2.11.4.1. DeviceCMYK against sRGB intent: 6.2.4.3.
    private void ValidateInspectable()
    {
        foreach (var page in document.Pages)
        {
            if (page.Generated is null)
            {
                throw new InvalidOperationException(
                    $"{Label} cannot be claimed for a page that did not come from DocumentBuilder: its fonts, images and "
                    + "colour spaces cannot be inspected, and this library will not identify a document as conformant on "
                    + "content it has not verified. Rebuild the page with DocumentBuilder, or save without conformance "
                    + "(PdfAConformance.None and PdfUA false).");
            }
        }
    }

    private void ValidateTagging()
    {
        if (!document.PdfUA && !IsLevelA(document.Conformance))
        {
            return;
        }

        if (document.Structure is { } structure)
        {
            ValidateFigureAltText(structure);
        }

        if (!document.PdfUA && document.HasUntaggedListContent)
        {
            throw new InvalidOperationException(
                $"{Label} requires every list to be tagged, but the document has an untagged list; set DocumentBuilder.PdfUA to tag lists or remove them.");
        }

        foreach (var page in document.Pages)
        {
            if (page.Generated is { Links.Count: > 0 })
            {
                throw new InvalidOperationException(
                    $"{Label} requires every link annotation to be referenced from the structure tree, which this library does not yet emit; remove the hyperlinks (Run.Link / Run.LinkToAnchor) to produce a conforming document.");
            }
        }
    }

    private void ValidateFigureAltText(StructureElement element)
    {
        if (element.Type == "Figure" && element.Alt is null && element.ActualText is null)
        {
            throw new InvalidOperationException(
                $"{Label} requires every Figure to carry an alternate description; set Image.AlternateText or Image.ActualText.");
        }

        foreach (var child in element.Children)
        {
            ValidateFigureAltText(child);
        }
    }

    private void ValidatePdfA()
    {
        if (document.Loaded?.Source is { } source && source.IsEncrypted)
        {
            throw new InvalidOperationException("PDF/A forbids encryption; the source document is encrypted.");
        }

        if (document.Encryption is not null)
        {
            throw new InvalidOperationException(
                "PDF/A forbids encryption; clear DocumentBuilder.Encryption or use PdfAConformance.None.");
        }

        if (IsLevelA(document.Conformance) && document.Structure is null)
        {
            throw new InvalidOperationException(
                $"{document.Conformance} requires Tagged PDF logical structure; the document has no structure tree. Build the document with DocumentBuilder or use a Level B conformance.");
        }

        switch (document.Conformance)
        {
            case PdfAConformance.PdfA2B or PdfAConformance.PdfA2A
                or PdfAConformance.PdfA4 or PdfAConformance.PdfA4E when document.Attachments.Count > 0:
                throw new InvalidOperationException(
                    $"{document.Conformance} only permits embedded files that are themselves PDF/A conformant, which cannot be verified; remove the attachments or use PdfA3B, PdfA3A or PdfA4F.");
            case PdfAConformance.PdfA4F when document.Attachments.Count == 0:
                throw new InvalidOperationException(
                    "PDF/A-4F requires at least one embedded file; add one with DocumentBuilder.Attachments or use PdfA4.");
        }

        ValidateImageColorSpaces();
    }

    // ISO 19005-2 6.2.4.3: DeviceCMYK requires a CMYK output intent.
    private void ValidateImageColorSpaces()
    {
        foreach (var page in document.Pages)
        {
            if (page.Generated is not { } generated)
            {
                continue;
            }

            foreach (var image in generated.Images)
            {
                if (image.Image.Image.Dictionary.TryGetValue("ColorSpace", out var space)
                    && space is NameObject { Value: "DeviceCMYK" })
                {
                    throw new InvalidOperationException(
                        "PDF/A pairs a DeviceCMYK image with an sRGB output intent, which ISO 19005 forbids; convert the image to RGB or grayscale, or use PdfAConformance.None.");
                }
            }
        }
    }

    private void ValidateFonts()
    {
        foreach (var page in document.Pages)
        {
            if (page.Generated is not { } generated)
            {
                continue;
            }

            foreach (var font in generated.Fonts)
            {
                if (font.Sfnt is null)
                {
                    throw Fonts.FontResolution.Base14Forbidden(Label, font.Base14Name, family: null);
                }
            }
        }

        foreach (var page in document.Pages)
        {
            foreach (var element in page.Content)
            {
                if (element is not TextContent { FontResourceName: null } text)
                {
                    continue;
                }

                throw Fonts.FontResolution.Base14Forbidden(
                    Label, Fonts.FontResolution.ResolveBase14Name(text.Font, scope: default), text.Font.Name);
            }
        }
    }

    public void WriteConformance(DocumentWriter writer, DictionaryObject catalog)
    {
        var xmp = DocumentSaver.BaseXmp(document.Info);

        var part = 0;
        if (document.Conformance != PdfAConformance.None)
        {
            (part, var conformance) = Identification(document.Conformance);
            xmp.PdfAPart = part;
            xmp.PdfAConformance = conformance;

            foreach (var attachment in document.Attachments)
            {
                if (attachment.Name == "factur-x.xml")
                {
                    xmp.FacturX = BuildFacturX(attachment.FacturX);
                    break;
                }
            }
        }

        if (part == 4)
        {
            xmp.PdfARevision = 2020;
        }

        if (document.PdfUA)
        {
            xmp.PdfUaPart = 1;
            xmp.IncludePdfUaExtensionSchema = document.Conformance != PdfAConformance.None;
        }

        catalog["Metadata"] = writer.Add(xmp.BuildStream());

        if (document.Conformance != PdfAConformance.None)
        {
            var intent = OutputIntentBuilder.BuildSrgb("sRGB IEC61966-2.1");
            if (intent["DestOutputProfile"] is StreamObject profile)
            {
                intent["DestOutputProfile"] = writer.Add(profile);
            }

            catalog["OutputIntents"] = new ArrayObject { writer.Add(intent) };

            if (part == 4)
            {
                // ISO 19005-4 6.1.2: PDF 2.0 header. Catalog restates the version (ISO 32000-2 7.5.2).
                catalog["Version"] = new NameObject("2.0");
            }
        }

        if (document.PdfUA)
        {
            ResolveViewerPreferences(writer, catalog)["DisplayDocTitle"] = new BooleanObject(true);
        }

        if (!string.IsNullOrEmpty(document.Language))
        {
            catalog["Lang"] = new StringObject(document.Language);
        }
    }

    private static FacturXMetadata BuildFacturX(FacturXProfile? profile)
    {
        if (profile is null)
        {
            return new FacturXMetadata();
        }

        if (string.IsNullOrEmpty(profile.DocumentType)
            || string.IsNullOrEmpty(profile.Version)
            || string.IsNullOrEmpty(profile.ConformanceLevel))
        {
            throw new InvalidOperationException(
                "Attachment.FacturX requires DocumentType, Version and ConformanceLevel; leave the profile null to use the BASIC 1.0 INVOICE defaults.");
        }

        return new FacturXMetadata
        {
            DocumentType = profile.DocumentType,
            Version = profile.Version,
            ConformanceLevel = profile.ConformanceLevel,
        };
    }

    private static DictionaryObject ResolveViewerPreferences(DocumentWriter writer, DictionaryObject catalog)
    {
        if (catalog.TryGetValue("ViewerPreferences", out var existing))
        {
            if (existing is DictionaryObject dictionary)
            {
                return dictionary;
            }

            if (existing is ReferenceObject reference && writer.Resolve(reference) is DictionaryObject referenced)
            {
                return referenced;
            }
        }

        var preferences = new DictionaryObject();
        catalog["ViewerPreferences"] = preferences;
        return preferences;
    }

}
