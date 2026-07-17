#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Loaded-document PdfUA re-save must merge its own catalog entries into the preserved
// source catalog without discarding preserved data:
//  * an indirect source /ViewerPreferences is resolved and augmented with
//    DisplayDocTitle instead of being replaced (finding: PdfUA drops indirect prefs).
//  * the source /Metadata stream is not imported (it would be orphaned once
//    ConformanceWriter overwrites catalog["Metadata"]).
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

    // The source page itself is untagged, uninspectable content that PDF/UA cannot be claimed
    // over (ConformanceWriter refuses it), so it is replaced by a built page. The load-time
    // catalog - and with it the indirect /ViewerPreferences and orphan /Metadata this fixture
    // exists to exercise - is preserved from the source regardless of where the pages came from.
    private static Document LoadAsPdfUa()
    {
        var document = Document.LoadFromStream(new MemoryStream(SourceWithIndirectPrefsAndMetadata()));
        document.Pages.RemoveAt(0);
        document.Append(TaggedPage());

        document.Structure = new StructureElement { Type = "Document" };
        document.Language = "en-US";
        document.Info.Title = "Accessible Title";
        document.PdfUA = true;
        return document;
    }

    private static Document TaggedPage()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        builder.Info.Title = "Accessible Title";
        builder.Language = "en-US";
        builder.PdfUA = true;
        BuildTestSupport.AddText(builder.Sections.Add(), "Accessible content", BuildTestSupport.Latin);
        return builder.Build();
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
