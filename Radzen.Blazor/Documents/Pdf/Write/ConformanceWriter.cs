using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Output;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Write;

internal sealed class ConformanceWriter(PortableDocument document, PageOutputMap pageMap)
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

    private IReadOnlyList<PageOutput> PlannedPages() => pageMap.Planned;

    public void ValidateConformance()
    {
        if (document.Conformance != PdfAConformance.None)
        {
            ValidatePdfA();
        }

        ValidateInspectable();

        if (document.IsPdfUa && document.Output?.Structure is null && !document.HasPreservableStructureGraph)
        {
            throw new InvalidOperationException(
                "PDF/UA requires Tagged PDF logical structure; the document has no structure tree. Render the document with DocumentRenderer.");
        }

        ValidateFonts();

        if (document.IsPdfUa && string.IsNullOrEmpty(document.Language))
        {
            throw new InvalidOperationException(
                "PDF/UA requires the document's natural language to be determinable; set Document.Language (e.g. \"en-US\").");
        }

        if (document.IsPdfUa && string.IsNullOrEmpty(document.Info.Title))
        {
            throw new InvalidOperationException(
                "PDF/UA requires a document title displayed by the viewer (DisplayDocTitle); set Document.Info.Title.");
        }

        ValidateTagging();
    }

    // Font embedding: ISO 19005-2 6.2.11.4.1. DeviceCMYK against sRGB intent: 6.2.4.3.
    private void ValidateInspectable()
    {
        foreach (var page in document.Pages)
        {
            if (page.IsGenerated)
            {
                continue;
            }

            if (LoadedResources(page) is { } loaded)
            {
                new LoadedPageValidator(loaded.Reader, Label).Inspect(loaded.Resources);
                continue;
            }

            throw new InvalidOperationException(
                $"{Label} cannot be claimed for a page that did not come from DocumentRenderer and carries no readable "
                + "resources: its fonts, images and color spaces cannot be inspected, and this library will not identify "
                + "a document as conformant on content it has not verified. Rebuild the page with DocumentRenderer, or "
                + "save without conformance (PdfAConformance.None and PdfUaConformance.None).");
        }
    }

    private (DocumentReader Reader, DictionaryObject Resources)? LoadedResources(Page page)
    {
        if (document.Loaded is not { } loaded)
        {
            return null;
        }

        if (loaded.Source is { } source && loaded.SourceResources.TryGetValue(page, out var resources))
        {
            return (source, resources);
        }

        if (loaded.AppendedResources.TryGetValue(page, out var appended))
        {
            return (appended.Reader, appended.Resources);
        }

        return null;
    }

    private void ValidateTagging()
    {
        if (!document.IsPdfUa && !IsLevelA(document.Conformance))
        {
            return;
        }

        if (document.Output?.Structure is { } structure)
        {
            ValidateFigureAltText(structure);
        }

        ValidateRoleMap();
        ValidateLinkStructure();
    }

    // ISO 14289-1:2014 7.1: non-standard structure types shall be mapped, in the structure tree root's role
    // map, to the nearest functionally equivalent standard type defined in ISO 32000-1:2008 14.8.4.
    private void ValidateRoleMap()
    {
        ValidateRoleMapChains();

        if (document.Output?.UnmappedRoles is not { Length: > 0 } roles)
        {
            return;
        }

        throw new InvalidOperationException(
            $"{Label} requires every non-standard structure type to be role mapped to a standard ISO 32000-1 "
            + $"structure type, but Style.Role declares '{string.Join("', '", roles)}' with no matching "
            + "DocumentRenderer.RoleMap entry, so a reader cannot interpret the tag. Call "
            + "DocumentRenderer.RoleMap.Add(role, standardType) for each role, or clear Style.Role.");
    }

    // ISO 14289-1:2014 7.1 and Matterhorn Protocol 1.1 checkpoint 02: a role mapping chain shall terminate
    // at a standard ISO 32000-1 structure type (02-002) and shall not be circular (02-003).
    private void ValidateRoleMapChains()
    {
        if (document.Output?.RoleMap is not { Length: > 0 } entries)
        {
            return;
        }

        var chain = new Dictionary<string, string>(entries.Length, StringComparer.Ordinal);

        foreach (var entry in entries)
        {
            chain[entry.Key] = entry.Value;
        }

        foreach (var role in chain.Keys)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var current = role;

            while (!RoleMap.IsStandardType(current))
            {
                if (!visited.Add(current))
                {
                    throw new InvalidOperationException(
                        $"{Label} requires every role mapping to terminate at a standard ISO 32000-1 structure type, "
                        + $"but '{role}' maps in a cycle through '{current}'. Break the cycle in "
                        + "DocumentRenderer.RoleMap.");
                }

                if (!chain.TryGetValue(current, out var next))
                {
                    throw new InvalidOperationException(
                        $"{Label} requires every role mapping to terminate at a standard ISO 32000-1 structure type, "
                        + $"but '{role}' maps to '{current}', which is neither a standard type nor itself role "
                        + "mapped. Add a DocumentRenderer.RoleMap entry for it, or map the role directly to a "
                        + "standard type.");
                }

                current = next;
            }
        }
    }

    // ISO 32000-1 14.8.4.4.2 and ISO 14289-1 7.18: a link annotation belongs to a Link structure element,
    // reached through an OBJR kid and the annotation's own /StructParent entry.
    private void ValidateLinkStructure()
    {
        foreach (var generated in PlannedPages())
        {
            foreach (var link in generated.Links)
            {
                if (link.StructureElementId is null)
                {
                    throw new InvalidOperationException(
                        $"{Label} requires every link annotation to be referenced from the structure tree, but a hyperlink "
                        + "sits outside the tagged content (page headers, footers and watermarks are artifacts and carry no "
                        + "structure); move the hyperlink into the document body or remove it.");
                }
            }
        }
    }

    private void ValidateFigureAltText(StructureElementSnapshot element)
    {
        if (element.Type == "Figure"
            && string.IsNullOrEmpty(element.Alt)
            && string.IsNullOrEmpty(element.ActualText))
        {
            throw new InvalidOperationException(
                $"{Label} requires every non-decorative Figure to carry alternate or replacement text; "
                + "set AlternateText or ActualText, or set AlternateText to the empty string when the content is "
                + "purely decorative.");
        }

        foreach (var kid in element.Kids)
        {
            if (kid.Child is { } child)
            {
                ValidateFigureAltText(child);
            }
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
                "PDF/A forbids encryption; clear DocumentRenderer.Encryption or use PdfAConformance.None.");
        }

        if (IsLevelA(document.Conformance)
            && document.Output?.Structure is null
            && !document.HasPreservableStructureGraph)
        {
            throw new InvalidOperationException(
                $"{document.Conformance} requires Tagged PDF logical structure; the document has no structure tree. Render the document with DocumentRenderer or use a Level B conformance.");
        }

        switch (document.Conformance)
        {
            case PdfAConformance.PdfA2B or PdfAConformance.PdfA2A
                or PdfAConformance.PdfA4 or PdfAConformance.PdfA4E when document.Attachments.Count > 0:
                throw new InvalidOperationException(
                    $"{document.Conformance} only permits embedded files that are themselves PDF/A conformant, which cannot be verified; remove the attachments or use PdfA3B, PdfA3A or PdfA4F.");
            case PdfAConformance.PdfA4F when document.Attachments.Count == 0:
                throw new InvalidOperationException(
                    "PDF/A-4F requires at least one embedded file; add one with DocumentRenderer.Attachments or use PdfA4.");
        }

        ValidateImageColorSpaces();
    }

    // ISO 19005-2 6.2.4.3: DeviceCMYK requires a CMYK output intent.
    private void ValidateImageColorSpaces()
    {
        foreach (var generated in PlannedPages())
        {
            foreach (var image in generated.Images)
            {
                if (image.Image.ColorSpace == ImageColorSpace.DeviceCmyk)
                {
                    throw new InvalidOperationException(
                        "PDF/A pairs a DeviceCMYK image with an sRGB output intent, which ISO 19005 forbids; convert the image to RGB or grayscale, or use PdfAConformance.None.");
                }
            }
        }
    }

    private void ValidateFonts()
    {
        foreach (var generated in PlannedPages())
        {
            foreach (var font in generated.Fonts)
            {
                if (!font.IsEmbedded)
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
                    Label, Fonts.FontResolution.ResolveBase14Name(text.Font, scope: default), text.Font.Family);
            }
        }
    }

    public void WriteConformance(DocumentWriter writer, DictionaryObject catalog)
    {
        var xmp = DocumentGraphBuilder.BaseXmp(document.Info);

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

        if (document.IsPdfUa)
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

        if (document.IsPdfUa)
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
