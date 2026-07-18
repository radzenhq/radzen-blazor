#nullable enable
using System;
using System.Runtime.CompilerServices;
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        action();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] Timestamp(byte[] original)
        => PdfTimestamper.Timestamp(original, new RecordingTimestampProvider());

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

    [Fact]
    public void Timestamp_DoesNotCopyTheDocumentToHashIt()
    {
        var original = BuildLargeDocument(4 * 1024 * 1024);

        Timestamp(original);

        var bytes = Measure(() => Timestamp(original));

        var budget = original.Length * 2L;
        Assert.True(
            bytes < budget,
            $"Timestamping a {original.Length} byte document allocated {bytes} bytes (budget {budget}).");
    }

    [Fact]
    public void Timestamp_OverheadDoesNotScaleWithDocumentSize()
    {
        var small = BuildLargeDocument(1 * 1024 * 1024);
        var large = BuildLargeDocument(8 * 1024 * 1024);

        Timestamp(small);
        Timestamp(large);

        var smallBytes = Measure(() => Timestamp(small)) - small.Length;
        var largeBytes = Measure(() => Timestamp(large)) - large.Length;

        Assert.True(
            largeBytes < smallBytes + (large.Length - small.Length),
            $"Excess grew from {smallBytes} to {largeBytes} between a {small.Length} and {large.Length} byte document.");
    }
}
