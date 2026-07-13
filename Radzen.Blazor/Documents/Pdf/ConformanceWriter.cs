using Radzen.Documents.Pdf.Objects;
using System;
using System.Text;

namespace Radzen.Documents.Pdf;

// Enforces and emits PDF/A and PDF/UA conformance on save: validates the document
// against the requested level, then writes the XMP metadata, sRGB output intent and
// trailer /ID the conformance level requires.
internal sealed class ConformanceWriter(Document document)
{
    // (pdfaid:part, pdfaid:conformance). PDF/A-4 carries no conformance letter.
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

    // Fully tagged conformance (PDF/UA, PDF/A Level-A) forbids untagged real content and
    // requires alternate descriptions and structure-linked annotations. These are checked
    // here so the writer fails loud instead of advertising a conformance it does not meet.
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
        if (document.source is not null && document.source.IsEncrypted)
        {
            throw new InvalidOperationException("PDF/A forbids encryption; the source document is encrypted.");
        }

        if (IsLevelA(document.Conformance) && document.Structure is null)
        {
            throw new InvalidOperationException(
                $"{document.Conformance} requires Tagged PDF logical structure; the document has no structure tree. Build the document with DocumentBuilder or use a Level B conformance.");
        }

        // PDF/A-2 and base PDF/A-4 only permit embedded files that are themselves
        // PDF/A conformant, which this library cannot verify; PDF/A-4E is treated
        // the same conservatively. PDF/A-3 and PDF/A-4F allow arbitrary files, and
        // PDF/A-4F additionally requires at least one.
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
                    throw new InvalidOperationException(
                        $"{Label} forbids the standard-14 font '{font.Base14 ?? "Helvetica"}' referenced by name; register an embeddable font file with DocumentBuilder.Fonts instead.");
                }
            }
        }

        // Overlay text added through Page.Content emits a non-embedded base-14 Type1
        // font, which the generated.Fonts scan above cannot see; reject it with the
        // same error the generator raises. Loaded original text keeps its own font
        // reference (FontResourceName) and is not re-emitted as a base-14 face.
        foreach (var page in document.Pages)
        {
            foreach (var element in page.Content)
            {
                if (element is not TextContent { FontResourceName: null } text)
                {
                    continue;
                }

                var name = Fonts.Base14Metrics.Resolve(text.Font)?.PostScriptName ?? "Helvetica";
                throw new InvalidOperationException(
                    $"{Label} forbids the standard-14 font '{name}' referenced by name; register an embeddable font file for '{text.Font.Name}' with DocumentBuilder.Fonts instead.");
            }
        }
    }

    public void WriteConformance(DocumentWriter writer, DictionaryObject catalog)
    {
        var xmp = new XmpMetadata
        {
            Info = document.Info,
            Producer = document.Info.Producer ?? "Radzen.Documents.Pdf",
            CreationDate = document.Info.CreationDate,
            ModificationDate = document.Info.ModificationDate,
        };

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

        catalog["Metadata"] = writer.Add(BuildMetadataStream(xmp, part));

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
                // ISO 19005-4 documents are PDF 2.0; the header stays 1.7, so
                // declare the version through the catalog.
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

    // Amends the packet XmpMetadata builds with the identification entries it does
    // not model: pdfaid:rev (required by PDF/A-4) and the pdfuaid schema.
    private StreamObject BuildMetadataStream(XmpMetadata xmp, int part)
    {
        var packet = Encoding.UTF8.GetString(xmp.BuildPacket());

        if (part == 4)
        {
            packet = InsertAfter(packet, "</pdfaid:part>\n", "   <pdfaid:rev>2020</pdfaid:rev>\n");
        }

        if (document.PdfUA)
        {
            packet = InsertAfter(
                packet,
                "xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"\n",
                "   xmlns:pdfuaid=\"http://www.aiim.org/pdfua/ns/id/\"\n");
            packet = InsertBefore(packet, "  </rdf:Description>", "   <pdfuaid:part>1</pdfuaid:part>\n");

            // PDF/A 6.6.2.3.1: pdfuaid is not among the PDF/A predefined XMP
            // schemas, so combining with any PDF/A level needs an extension
            // schema declaring it.
            if (document.Conformance != PdfAConformance.None)
            {
                packet = InsertBefore(packet, " </rdf:RDF>", PdfUaExtensionSchema);
            }
        }

        return XmpMetadata.WrapPacket(Encoding.UTF8.GetBytes(packet));
    }

    // factur-x.xml with no declared profile keeps the historic BASIC 1.0 INVOICE
    // defaults; a caller that sets a profile must fill every field so the XMP never
    // declares a blank fx:ConformanceLevel.
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

    // PreserveCatalog imports an indirect source /ViewerPreferences as a reference
    // into the writer; resolve it so preserved entries (Direction, HideToolbar, ...)
    // survive alongside the DisplayDocTitle PDF/UA requires instead of being replaced.
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

    private const string PdfUaExtensionSchema =
        "  <rdf:Description rdf:about=\"\"\n"
        + "   xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\"\n"
        + "   xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\"\n"
        + "   xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">\n"
        + "   <pdfaExtension:schemas>\n"
        + "    <rdf:Bag>\n"
        + "     <rdf:li rdf:parseType=\"Resource\">\n"
        + "      <pdfaSchema:schema>PDF/UA identification schema</pdfaSchema:schema>\n"
        + "      <pdfaSchema:namespaceURI>http://www.aiim.org/pdfua/ns/id/</pdfaSchema:namespaceURI>\n"
        + "      <pdfaSchema:prefix>pdfuaid</pdfaSchema:prefix>\n"
        + "      <pdfaSchema:property>\n"
        + "       <rdf:Seq>\n"
        + "        <rdf:li rdf:parseType=\"Resource\">\n"
        + "         <pdfaProperty:name>part</pdfaProperty:name>\n"
        + "         <pdfaProperty:valueType>Integer</pdfaProperty:valueType>\n"
        + "         <pdfaProperty:category>internal</pdfaProperty:category>\n"
        + "         <pdfaProperty:description>PDF/UA version identifier</pdfaProperty:description>\n"
        + "        </rdf:li>\n"
        + "       </rdf:Seq>\n"
        + "      </pdfaSchema:property>\n"
        + "     </rdf:li>\n"
        + "    </rdf:Bag>\n"
        + "   </pdfaExtension:schemas>\n"
        + "  </rdf:Description>\n";

    private static string InsertAfter(string packet, string anchor, string insertion)
        => packet.Insert(RequireAnchor(packet, anchor) + anchor.Length, insertion);

    private static string InsertBefore(string packet, string anchor, string insertion)
        => packet.Insert(RequireAnchor(packet, anchor), insertion);

    // The identification entries PDF/A-4 and PDF/UA require are spliced into the raw
    // XMP packet by anchor. A missing anchor means XmpMetadata's format drifted and
    // the amendment would silently drop; fail loud rather than emit a non-conforming
    // packet that still reports success.
    private static int RequireAnchor(string packet, string anchor)
    {
        var index = packet.IndexOf(anchor, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException(
                $"XMP conformance amendment anchor not found: '{anchor}'.");
        }

        return index;
    }
}
