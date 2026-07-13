#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Repair must recover objects compressed inside /Type /ObjStm containers. When a
// modern PDF stores its catalog/pages inside an object stream and its xref is
// unusable, the header scan only sees the container's "N G obj"; the members need
// synthesized type-2 entries so /Root and the compressed objects still resolve.
public class RepairObjectStreamTests
{
    // Objects 1 (int), 2 (dict), 3 (Catalog) live inside ObjStm object 4; object 5
    // is the xref stream. Removing the xref stream object and startxref forces the
    // repair scan, which can only see "4 0 obj".
    private static byte[] ObjStmFile(bool withXref)
    {
        var b1 = "42";
        var b2 = "<< /A 1 /B (x) >>";
        var b3 = "<< /Type /Catalog >>";
        var body = b1 + "\n" + b2 + "\n" + b3;
        var o1 = 0;
        var o2 = (b1 + "\n").Length;
        var o3 = (b1 + "\n" + b2 + "\n").Length;
        var header = $"1 {o1} 2 {o2} 3 {o3} ";
        var stmData = header + body;
        var first = header.Length;
        var length = stmData.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset4 = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 3 /First {first} /Length {length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        if (!withXref)
        {
            return pdf.ToArray();
        }

        var offset5 = pdf.Position;
        var payload = new byte[24];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(0, 0, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(2, 4, 2));
        Copy(payload, 16, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));
        Copy(payload, 20, FixturePdf.XrefStreamEntry(1, (int)offset5, 0));

        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /W [1 2 1] /Root 3 0 R /Length 24 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset5 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    // Sanity: with the xref stream present the compressed catalog resolves normally.
    [Fact]
    public void IntactXref_ResolvesCompressedCatalog()
    {
        var reader = DocumentReader.Parse(ObjStmFile(withXref: true));
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void CorruptXref_RepairRecoversRootFromObjStm()
    {
        var reader = DocumentReader.Parse(ObjStmFile(withXref: false));
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void CorruptXref_RepairResolvesCompressedMembers()
    {
        var reader = DocumentReader.Parse(ObjStmFile(withXref: false));

        Assert.Equal(42, Assert.IsType<NumberObject>(reader.GetObject(1)).IntValue);

        var dict = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal(1, Assert.IsType<NumberObject>(dict["A"]).IntValue);
        Assert.Equal("x", Assert.IsType<StringObject>(dict["B"]).Value);
    }

    [Fact]
    public void CorruptXref_RepairObjectMembersExceedingXrefBudget_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(
                ObjStmFile(withXref: false), null, new ReaderLimits { MaxXrefEntries = 3 }));
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }
}
