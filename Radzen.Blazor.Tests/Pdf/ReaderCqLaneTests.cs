#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderCqLaneTests
{
    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }

    private static byte[] ClassicPdf(string extraTrailer)
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append("trailer\n<< /Size 3 /Root 1 0 R" + extraTrailer + " >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void EncryptEntryThatIsNotADictionary_Throws()
    {
        Assert.Throws<DocumentParseException>(() => DocumentReader.Parse(ClassicPdf(" /Encrypt [ 1 2 ]")));
    }

    [Fact]
    public void NoEncryptEntry_ParsesUnencrypted()
    {
        var reader = DocumentReader.Parse(ClassicPdf(""));

        Assert.False(reader.IsEncrypted);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void TruncatedXrefStream_RepairsInsteadOfDroppingObjects()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        var off3 = pdf.Position;
        pdf.Append("3 0 obj\n<< /Type /XRef /Size 6 /Index [0 6] /W [1 2 1] /Root 1 0 R /Length 12 >>\nstream\n")
            .Append(new byte[12])
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + off3 + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void ObjectStreamMemberNumberMismatch_Throws()
    {
        var b0 = "42";
        var b1 = "<< /Type /Catalog >>";
        var body = b0 + "\n" + b1;
        var off1 = (b0 + "\n").Length;

        var header = $"99 0 2 {off1} ";
        var stmData = header + body;
        var first = header.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offStm = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 2 /First {first} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offXref = pdf.Position;
        var payload = new byte[16];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offStm, 0));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offXref, 0));
        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 2 4 2] /W [1 2 1] /Root 2 0 R /Length 16 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offXref + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        Assert.Throws<DocumentParseException>(() => reader.GetObject(1));
    }

    [Fact]
    public void WrongLength_RecoversToEndstream()
    {
        const string payload = "HELLO-STREAM-DATA";
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        pdf.Mark(3).Append("3 0 obj\n<< /Length 999999 >>\nstream\n" + payload + "\nendstream\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 4 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());
        var stream = Assert.IsType<StreamObject>(reader.GetObject(3));
        Assert.Equal(payload, Encoding.Latin1.GetString(reader.DecodeStream(stream)));
    }

    [Fact]
    public void NonSeekableStream_ParsesIdenticallyToSeekable()
    {
        var bytes = ClassicPdf("");

        var seekable = DocumentReader.Parse(new MemoryStream(bytes));
        var forwardOnly = DocumentReader.Parse(new ForwardOnlyStream(bytes));

        Assert.Equal(seekable.ObjectCount, forwardOnly.ObjectCount);
        var catalog = Assert.IsType<DictionaryObject>(forwardOnly.Resolve(forwardOnly.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    private sealed class ForwardOnlyStream(byte[] data) : Stream
    {
        private int position;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var remaining = data.Length - position;
            if (remaining <= 0)
            {
                return 0;
            }

            var take = Math.Min(count, remaining);
            Array.Copy(data, position, buffer, offset, take);
            position += take;
            return take;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
