#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.4: /Filter and /DecodeParms may be indirect references (top level and inside a filter array) and must be resolved before applying the chain.
public class FilterDispatchTests
{
    private static byte[] SinglePageFile(Action<FixturePdf> content, (int Number, string Body)[] extra, int size)
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        content(pdf);
        foreach (var (number, body) in extra)
        {
            pdf.Object(number, body);
        }

        var known = new bool[size];
        known[1] = known[2] = known[3] = known[4] = true;
        foreach (var (number, _) in extra)
        {
            known[number] = true;
        }

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 " + size + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < size; i++)
        {
            pdf.Append(known[i] ? FixturePdf.Entry20(pdf.OffsetOf(i)) : FixturePdf.Entry20(0, 65535, 'f'));
        }

        pdf.Append("trailer\n<< /Size " + size + " /Root 1 0 R >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static StreamObject ContentStream(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        return Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
    }

    [Fact]
    public void FilterArrayWithIndirectName_IsResolvedAndApplied()
    {
        const string marker = "BT /F1 12 Tf 72 720 Td (filter-array-ref) Tj ET";
        var encoded = Convert.ToHexString(Encoding.Latin1.GetBytes(marker)) + ">";
        var bytes = SinglePageFile(
            pdf => pdf.Append("4 0 obj\n<< /Length " + encoded.Length
                + " /Filter [7 0 R] >>\nstream\n" + encoded + "\nendstream\nendobj\n"),
            [(7, "7 0 obj\n/ASCIIHexDecode\nendobj\n")],
            8);

        var reader = DocumentReader.Parse(bytes);
        var decoded = Encoding.Latin1.GetString(reader.DecodeStream(ContentStream(reader)));
        Assert.Contains("(filter-array-ref) Tj", decoded);
    }

    [Fact]
    public void IndirectDecodeParms_PredictorIsApplied()
    {
        var text = "BT /F1 12 Tf 72 720 Td (png-predictor-marker) Tj ET";
        var plain = Encoding.Latin1.GetBytes(text.PadRight((text.Length + 3) / 4 * 4));
        var predicted = PngPredictor.Encode(plain, 12, 1, 8, 4);
        var deflated = FlateFilter.Encode(predicted);
        var bytes = SinglePageFile(
            pdf => pdf.Append("4 0 obj\n<< /Length " + deflated.Length
                    + " /Filter /FlateDecode /DecodeParms 8 0 R >>\nstream\n")
                .Append(deflated)
                .Append("\nendstream\nendobj\n"),
            [(8, "8 0 obj\n<< /Predictor 12 /Colors 1 /BitsPerComponent 8 /Columns 4 >>\nendobj\n")],
            9);

        var reader = DocumentReader.Parse(bytes);
        var decoded = Encoding.Latin1.GetString(reader.DecodeStream(ContentStream(reader)));
        Assert.Contains("(png-predictor-marker) Tj", decoded);
    }

    [Fact]
    public void ObjectStreamWithIndirectFilter_MembersResolve()
    {
        var member = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";
        var objStmPlain = "4 0 " + member;
        var encoded = Convert.ToHexString(Encoding.Latin1.GetBytes(objStmPlain)) + ">";

        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /ObjStm /N 1 /First 4 /Filter 7 0 R /Length " + encoded.Length
            + " >>\nstream\n" + encoded + "\nendstream\nendobj\n");
        pdf.Object(7, "7 0 obj\n/ASCIIHexDecode\nendobj\n");

        var offset6 = pdf.Position;
        var payload = new byte[28];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(1), 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(2), 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(3), 0));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(2, 5, 0));
        Copy(payload, 16, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(5), 0));
        Copy(payload, 20, FixturePdf.XrefStreamEntry(1, (int)offset6, 0));
        Copy(payload, 24, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(7), 0));
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /XRef /Size 8 /Index [1 7] /W [1 2 1] /Root 1 0 R /Length 28 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset6 + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());
        var font = Assert.IsType<DictionaryObject>(reader.GetObject(4));
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    private static void Copy(byte[] target, int at, byte[] source)
        => Array.Copy(source, 0, target, at, source.Length);
}
