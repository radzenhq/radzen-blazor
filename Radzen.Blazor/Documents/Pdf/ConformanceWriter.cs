using Radzen.Documents.Pdf.Objects;
using System;

namespace Radzen.Documents.Pdf;

// Enforces and emits PDF/A conformance on save: validates the document against the
// requested level, then writes the XMP metadata, sRGB output intent and trailer /ID
// the conformance level requires.
internal sealed class ConformanceWriter(Document document)
{
    public void ValidateConformance()
    {
        if (document.source is not null && document.source.IsEncrypted)
        {
            throw new InvalidOperationException("PDF/A forbids encryption; the source document is encrypted.");
        }

        if (document.Conformance == PdfAConformance.PdfA3A && document.Structure is null)
        {
            throw new InvalidOperationException(
                "PDF/A-3 Level A requires Tagged PDF logical structure; the document has no structure tree. Build the document with DocumentBuilder or use PdfAConformance.PdfA3B.");
        }

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
                        $"PDF/A forbids the standard-14 font '{font.Base14 ?? "Helvetica"}' referenced by name; register an embeddable font file with DocumentBuilder.Fonts instead.");
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
                    $"PDF/A forbids the standard-14 font '{name}' referenced by name; register an embeddable font file for '{text.Font.Name}' with DocumentBuilder.Fonts instead.");
            }
        }
    }

    public void WriteConformance(DocumentWriter writer, DictionaryObject catalog)
    {
        var xmp = new XmpMetadata
        {
            Info = document.Info,
            Producer = "Radzen.Documents.Pdf",
            PdfAPart = 3,
            PdfAConformance = document.Conformance == PdfAConformance.PdfA3A ? "A" : "B",
        };

        foreach (var attachment in document.Attachments)
        {
            if (attachment.Name == "factur-x.xml")
            {
                xmp.FacturX = new FacturXMetadata();
                break;
            }
        }

        catalog["Metadata"] = writer.Add(xmp.BuildStream());

        var intent = OutputIntentBuilder.BuildSrgb("sRGB IEC61966-2.1");
        if (intent["DestOutputProfile"] is StreamObject profile)
        {
            intent["DestOutputProfile"] = writer.Add(profile);
        }

        writer.Trailer["ID"] = BuildDocumentId();
        catalog["OutputIntents"] = new ArrayObject { writer.Add(intent) };
    }

    private ArrayObject BuildDocumentId()
    {
        var seed = $"{document.Info.Title}\n{document.Info.Author}\n{document.Pages.Count}\n{DateTime.UtcNow.Ticks}\n{Guid.NewGuid():N}";
        var hash = Radzen.Documents.Crypto.Sha2.Sha256(System.Text.Encoding.UTF8.GetBytes(seed));
        var id = Convert.ToHexString(hash, 0, 16);
        return [new StringObject(id), new StringObject(id)];
    }
}
