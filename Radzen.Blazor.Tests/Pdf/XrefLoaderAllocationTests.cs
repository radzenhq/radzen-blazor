#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Every classic-xref open parses two fixed-format decimal fields per entry before a single
// object is read. Those fields must be accumulated digit by digit out of the raw bytes:
// materializing a string per field puts two short-lived allocations on every entry of the
// table, which for a large document is millions of allocations of pure GC churn.
public class XrefLoaderAllocationTests
{
    private static byte[] BuildClassicXrefDocument(int count)
    {
        var text = new StringBuilder("%PDF-1.7\n");
        var offsets = new int[count + 1];
        for (var number = 1; number <= count; number++)
        {
            offsets[number] = text.Length;
            text.Append($"{number} 0 obj\n<< /Marker {number} >>\nendobj\n");
        }

        var xref = text.Length;
        text.Append($"xref\n0 {count + 1}\n0000000000 65535 f \n");
        for (var number = 1; number <= count; number++)
        {
            text.Append($"{offsets[number]:D10} 00000 n \n");
        }

        text.Append($"trailer\n<< /Size {count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(text.ToString());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long LoadXref(byte[] data, int count)
    {
        var limits = ReaderLimits.Default;
        var decoder = new StreamDecoder(limits, value => value);
        var loader = new XrefLoader(data, limits, decoder);
        var store = new IndirectObjectStore(data, limits, loader.Entries, decoder, new DocumentRepairer(data, limits));

        var before = GC.GetAllocatedBytesForCurrentThread();
        loader.Load(store);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(count + 1, loader.Entries.Count);
        return bytes;
    }

    [Fact]
    public void Load_DoesNotAllocatePerClassicXrefField()
    {
        const int Count = 20000;
        var data = BuildClassicXrefDocument(Count);

        LoadXref(data, Count);

        var bytes = LoadXref(data, Count);

        // The two entry dictionaries (the per-section table plus the merged one, each grown by
        // doubling) dominate at ~308 bytes per entry and are not what this pins. A string per
        // decimal field adds a measured 80 bytes per entry on top of that, taking the total to
        // ~388; the budget sits between the two so only the per-field allocation trips it.
        var budget = Count * 350L;
        Assert.True(bytes < budget, $"XrefLoader.Load allocated {bytes} bytes for {Count} entries, budget {budget}.");
    }

    [Fact]
    public void Load_ParsesClassicXrefFieldsIdenticallyToStringParsing()
    {
        var data = BuildClassicXrefDocument(64);
        var text = Encoding.ASCII.GetString(data);

        var limits = ReaderLimits.Default;
        var decoder = new StreamDecoder(limits, value => value);
        var loader = new XrefLoader(data, limits, decoder);
        loader.Load(new IndirectObjectStore(data, limits, loader.Entries, decoder, new DocumentRepairer(data, limits)));

        for (var number = 1; number <= 64; number++)
        {
            var entry = loader.Entries[number];
            Assert.Equal(1, entry.Type);
            Assert.Equal(0, entry.Field3);
            Assert.Equal($"{number} 0 obj", text.Substring((int)entry.Field2, $"{number} 0 obj".Length));
        }

        Assert.False(loader.Entries[0].InUse);
    }
}
