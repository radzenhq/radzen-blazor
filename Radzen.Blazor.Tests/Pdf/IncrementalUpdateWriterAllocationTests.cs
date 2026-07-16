#nullable enable
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A signature or annotation update appends a few KB to a document that may be tens of MB.
// ToArray must not double-buffer the original through a capacity-less MemoryStream: the
// doubling chain plus the final copy costs several multiples of the file size, all on the
// LOH, which is exactly what the sibling PooledBufferStream exists to avoid.
public class IncrementalUpdateWriterAllocationTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] BuildLargeDocument(int padding)
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (page zero) Tj ET"));

        // A stream of incompressible bytes gives the original a predictable size.
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
    private static byte[] Update(byte[] original)
    {
        var writer = new IncrementalUpdateWriter(original);
        writer.Add(new DictionaryObject { ["Type"] = new NameObject("Marker") });
        return writer.ToArray();
    }

    [Fact]
    public void ToArray_DoesNotDoubleBufferTheOriginal()
    {
        var original = BuildLargeDocument(4 * 1024 * 1024);

        Update(original);

        var bytes = Measure(() => Update(original));

        // The returned array is unavoidable; anything much beyond it is buffering overhead.
        var budget = original.Length * 2L;
        Assert.True(
            bytes < budget,
            $"ToArray over a {original.Length} byte original allocated {bytes} bytes (budget {budget}).");
    }

    [Fact]
    public void ToArray_OverheadDoesNotScaleWithOriginalSize()
    {
        var small = BuildLargeDocument(1 * 1024 * 1024);
        var large = BuildLargeDocument(8 * 1024 * 1024);

        Update(small);
        Update(large);

        var smallBytes = Measure(() => Update(small)) - small.Length;
        var largeBytes = Measure(() => Update(large)) - large.Length;

        // Both should be ~one working buffer, so the excess grows linearly at worst,
        // never with the ~3x multiplier a doubling MemoryStream applies.
        Assert.True(
            largeBytes < smallBytes + (large.Length - small.Length) * 2L,
            $"Excess grew from {smallBytes} to {largeBytes} between a {small.Length} and {large.Length} byte original.");
    }
}
