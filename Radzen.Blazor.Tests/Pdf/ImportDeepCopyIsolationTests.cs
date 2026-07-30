#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ImportDeepCopyIsolationTests
{
    private static byte[] SourceBytes()
    {
        var body = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (ORIGINAL-TEXT) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + "/Resources << /Font << /F1 5 0 R >> >> >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + body.Length + " >>\nstream\n").Append(body).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }
        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static PortableDocument Load(byte[] b) => PortableDocument.LoadFromStream(new MemoryStream(b));

    [Fact]
    public void ImportedPage_DoesNotShareResourceGraphWithSource()
    {
        var source = Load(SourceBytes());
        var target = new PortableDocument();
        var imported = target.ImportPage(source, 0);

        var sourceResources = source.Loaded!.SourceResources[source.Pages[0]];
        var sourceFont = (DictionaryObject)source.Loaded.Source!.Resolve(sourceResources["Font"]!);

        var before = target.ToArray();
        sourceFont["Injected"] = new NumberObject(1);
        var after = target.ToArray();

        Assert.Equal(before, after);
        Assert.DoesNotContain("Injected", Encoding.Latin1.GetString(after));
        Assert.False(target.Loaded!.AppendedResources.TryGetValue(imported, out var appended)
            && ReferenceEquals(appended.Resources, sourceResources));
    }

    [Fact]
    public void UnmodifiedImport_IsByteIdenticalToWholeSourceSnapshotImport()
    {
        var directTarget = new PortableDocument();
        directTarget.ImportPage(Load(SourceBytes()), 0);

        var snapshotSource = PageOperations.Snapshot(Load(SourceBytes()));
        var snapshotTarget = new PortableDocument();
        PageOperations.Import(snapshotTarget, snapshotSource, 0, 1);

        Assert.Equal(snapshotTarget.ToArray(), directTarget.ToArray());
    }
}
