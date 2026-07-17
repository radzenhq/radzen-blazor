#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadedPageWatermarkAppendTests
{
    private static byte[] Source(int pages, int linesPerPage)
    {
        var text = new StringBuilder();
        for (var i = 0; i < linesPerPage; i++)
        {
            text.Append("BT /F1 12 Tf 72 ").Append(700 - (i % 50)).Append(" Td (line ").Append(i).Append(") Tj ET\n");
        }

        var content = Encoding.ASCII.GetBytes(text.ToString());
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        var kids = new StringBuilder();
        for (var i = 0; i < pages; i++)
        {
            kids.Append(3 + (i * 2)).Append(" 0 R ");
        }

        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count " + pages + " /Kids [" + kids.ToString().TrimEnd() + "] >>\nendobj\n");
        for (var i = 0; i < pages; i++)
        {
            var page = 3 + (i * 2);
            pdf.Object(page, page + " 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents "
                + (page + 1) + " 0 R /Resources << /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >> >>\nendobj\n");
            pdf.Mark(page + 1);
            pdf.Append(page + 1 + " 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        }

        var count = 3 + (pages * 2);
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < count; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static Document Load(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        return Document.LoadFromStream(input);
    }

    private static long WatermarkAllocations(int pages, int linesPerPage)
    {
        var bytes = Source(pages, linesPerPage);
        var document = Load(bytes);
        var before = GC.GetAllocatedBytesForCurrentThread();
        document.AddWatermark("DRAFT");
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void AddWatermark_LoadedPages_DoesNotScaleWithContentSize()
    {
        WatermarkAllocations(4, 20);

        var small = WatermarkAllocations(4, 20);
        var large = WatermarkAllocations(4, 200);

        Assert.True(large < small * 2,
            $"AddWatermark allocations scaled with content size: {small} bytes for 20 lines/page, {large} bytes for 200 lines/page.");
    }

    [Fact]
    public void AddWatermark_LoadedPage_EmitsSameBytesAsMaterializedAppend()
    {
        var bytes = Source(2, 30);

        var lazy = Load(bytes);
        lazy.AddWatermark("DRAFT");
        using var lazyOutput = new MemoryStream();
        lazy.SaveToStream(lazyOutput);

        var eager = Load(bytes);
        foreach (var page in eager.Pages)
        {
            _ = page.Content.Count;
        }

        eager.AddWatermark("DRAFT");
        using var eagerOutput = new MemoryStream();
        eager.SaveToStream(eagerOutput);

        Assert.Equal(eagerOutput.ToArray(), lazyOutput.ToArray());
    }
}
