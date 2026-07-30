#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class PdfUaCatalogPreservationTests
{
    private const string OrphanMarker = "SOURCE-ORPHAN-MARKER";

    private static byte[] SourceWithIndirectPrefsAndMetadata()
    {
        var content = Encoding.ASCII.GetBytes("q 0 0 10 10 re f Q");
        var metadata = Encoding.ASCII.GetBytes(
            "<?xpacket begin=\"\"?><x:xmpmeta xmlns:x=\"adobe:ns:meta/\">" + OrphanMarker + "</x:xmpmeta><?xpacket end=\"w\"?>");

        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /ViewerPreferences 5 0 R /Metadata 6 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Direction /R2L /HideToolbar true >>\nendobj\n");
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /Metadata /Subtype /XML /Length " + metadata.Length + " >>\nstream\n")
            .Append(metadata).Append("\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 7; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static PortableDocument LoadAsPdfUa()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(SourceWithIndirectPrefsAndMetadata()));
        document.Pages.RemoveAt(0);
        document.Append(TaggedPage());

        document.Structure = new StructureElement { Type = "Document" };
        document.Language = "en-US";
        document.Info.Title = "Accessible Title";
        document.Accessibility = PdfUaConformance.PdfUa1;
        return document;
    }

    private static PortableDocument TaggedPage()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Accessible Title";
        document.Language = "en-US";
        var renderer = new DocumentRenderer();
        renderer.Accessibility = PdfUaConformance.PdfUa1;
        BuildTestSupport.AddText(document.Sections.Add(), "Accessible content", BuildTestSupport.Latin);
        return renderer.Render(document);
    }

    [Fact]
    public void PdfUaSave_PreservesIndirectViewerPreferences_AndAddsDisplayDocTitle()
    {
        var reader = DocumentReader.Parse(LoadAsPdfUa().ToArray());
        var catalog = ContentTestHelpers.Catalog(reader);

        var prefs = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["ViewerPreferences"]));
        Assert.Equal("R2L", Assert.IsType<NameObject>(reader.Resolve(prefs["Direction"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["HideToolbar"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["DisplayDocTitle"])).Value);
    }

    [Fact]
    public void PdfUaSave_DoesNotImportOrphanedSourceMetadata()
    {
        var bytes = LoadAsPdfUa().ToArray();

        Assert.DoesNotContain(OrphanMarker, Encoding.Latin1.GetString(bytes));

        var catalog = ContentTestHelpers.Catalog(DocumentReader.Parse(bytes));
        var metadata = Assert.IsType<StreamObject>(DocumentReader.Parse(bytes).Resolve(catalog["Metadata"]));
        Assert.Equal("Metadata", Assert.IsType<NameObject>(metadata.Dictionary["Type"]).Value);
    }
}
