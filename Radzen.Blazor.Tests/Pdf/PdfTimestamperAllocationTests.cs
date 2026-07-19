#nullable enable
using System;
using System.Text;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfTimestamperAllocationTests
{
    private sealed class RecordingTimestampProvider : ITimestampProvider
    {
        public byte[]? LastHash { get; private set; }

        public byte[] GetTimestampToken(ReadOnlySpan<byte> hash)
        {
            LastHash = hash.ToArray();
            return new byte[100];
        }
    }

    private static byte[] BuildLargeDocument(int padding)
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (page zero) Tj ET"));

        var payload = new byte[padding];
        new Random(7).NextBytes(payload);
        document.Pages.Add(PageSizes.A4).SetContent(payload);

        return document.ToArray();
    }

    [Fact]
    public void Timestamp_HashesTheTwoByteRangeSegmentsIncrementally()
    {
        var original = BuildLargeDocument(64 * 1024);
        var provider = new RecordingTimestampProvider();

        var stamped = PdfTimestamper.Timestamp(original, provider);

        var reader = DocumentReader.Parse(stamped);
        var catalog = (DictionaryObject)reader.Resolve(reader.Trailer["Root"]);
        var acroForm = (DictionaryObject)reader.Resolve(catalog["AcroForm"]);
        var fields = (ArrayObject)reader.Resolve(acroForm["Fields"]);
        var field = (DictionaryObject)reader.Resolve(fields[0]);
        var signature = (DictionaryObject)reader.Resolve(field["V"]);
        var range = (ArrayObject)reader.Resolve(signature["ByteRange"]);
        var gapStart = ((NumberObject)range[1]).IntValue;
        var gapEnd = ((NumberObject)range[2]).IntValue;
        var tail = ((NumberObject)range[3]).IntValue;

        var hasher = new Sha256Hasher();
        hasher.Append(stamped.AsSpan(0, gapStart));
        hasher.Append(stamped.AsSpan(gapEnd, tail));
        var expected = hasher.Finish();

        Assert.NotNull(provider.LastHash);
        Assert.True(expected.AsSpan().SequenceEqual(provider.LastHash));
    }

}
