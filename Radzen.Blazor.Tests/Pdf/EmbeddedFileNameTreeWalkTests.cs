#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The catalog /Names EmbeddedFiles name tree is walked by DocumentLoader.CollectEmbeddedFiles
// on every Document.LoadFromStream of untrusted input. A name tree is a tree per ISO 32000-1
// 7.9.6, so a node reachable twice is malformed - and if the walk follows it anyway, the depth
// cap does not bound the work: a chain of D nodes that each list the SAME child twice is
// acyclic along every path (so depth never exceeds D) yet spans 2^D paths. The sibling walks
// (CollectPages, ReadNameTree, ReadPageLabelTree) all reject a revisited node; this one must too.
public class EmbeddedFileNameTreeWalkTests
{
    [Fact]
    public void EmbeddedFileTree_SharedKid_Throws()
    {
        var bytes = NameTreeFile(
            (6, "<< /Kids [7 0 R 7 0 R] >>"),
            (7, "<< /Names [(a.txt) 4 0 R] >>"));

        Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(new MemoryStream(bytes)));
    }

    // Every level halves nothing: pre-fix the walk visits 2^Levels leaves under a depth cap of
    // 1024 that never fires. The guard must reject the first re-entry, so this returns in the
    // time it takes to visit two nodes regardless of Levels.
    [Fact]
    public void EmbeddedFileTree_DuplicatingChain_IsBoundedByTheVisitedSet()
    {
        const int Levels = 20;

        var nodes = new List<(int Number, string Body)>();
        for (var i = 0; i < Levels; i++)
        {
            nodes.Add((6 + i, $"<< /Kids [{7 + i} 0 R {7 + i} 0 R] >>"));
        }

        nodes.Add((6 + Levels, "<< /Names [(a.txt) 4 0 R] >>"));

        var bytes = NameTreeFile(nodes.ToArray());

        var watch = Stopwatch.StartNew();
        Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(new MemoryStream(bytes)));
        watch.Stop();

        // 2^20 visits cannot happen in this budget; the assert above is the real check and this
        // only pins that the rejection is immediate rather than after the whole DAG is walked.
        Assert.True(watch.ElapsedMilliseconds < 2000, $"Walk took {watch.ElapsedMilliseconds} ms.");
    }

    [Fact]
    public void EmbeddedFileTree_DistinctKids_LoadsEveryAttachment_PositiveControl()
    {
        var bytes = NameTreeFile(
            (6, "<< /Kids [7 0 R 8 0 R] >>"),
            (7, "<< /Names [(a.txt) 4 0 R] >>"),
            (8, "<< /Names [(b.txt) 9 0 R] >>"),
            (9, "<< /Type /Filespec /F (b.txt) /UF (b.txt) /EF << /F 5 0 R >> >>"));

        var document = Document.LoadFromStream(new MemoryStream(bytes));

        Assert.Equal(new[] { "a.txt", "b.txt" }, document.Attachments.Select(a => a.Name).Order().ToArray());
    }

    [Fact]
    public void EmbeddedFileTree_SharedFilespecAcrossDistinctLeaves_LoadsItOnce_PositiveControl()
    {
        // Two distinct leaves may legitimately name the same filespec; only NODE re-entry is
        // malformed. AddAttachment's own `seen` set keeps this to a single attachment.
        var bytes = NameTreeFile(
            (6, "<< /Kids [7 0 R 8 0 R] >>"),
            (7, "<< /Names [(a.txt) 4 0 R] >>"),
            (8, "<< /Names [(b.txt) 4 0 R] >>"));

        var document = Document.LoadFromStream(new MemoryStream(bytes));

        Assert.Equal("a.txt", Assert.Single(document.Attachments).Name);
    }

    // Objects 1-3 are the minimal catalog/pages/page, 4 the filespec the leaves point at and 5
    // its embedded payload stream; `nodes` supplies the EmbeddedFiles tree from object 6 up.
    private static byte[] NameTreeFile(params (int Number, string Body)[] nodes)
    {
        const string Payload = "hello";

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 6 0 R >> >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Filespec /F (a.txt) /UF (a.txt) /EF << /F 5 0 R >> >>\nendobj\n");
        pdf.Object(
            5,
            $"5 0 obj\n<< /Type /EmbeddedFile /Length {Payload.Length} >>\nstream\n{Payload}\nendstream\nendobj\n");

        foreach (var (number, body) in nodes)
        {
            pdf.Object(number, $"{number} 0 obj\n{body}\nendobj\n");
        }

        var count = 5 + nodes.Length;
        var xref = pdf.Position;
        pdf.Append($"xref\n0 {count + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append($"trailer\n<< /Size {count + 1} /Root 1 0 R >>\n")
            .Append("startxref\n" + xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");

        return pdf.ToArray();
    }
}
