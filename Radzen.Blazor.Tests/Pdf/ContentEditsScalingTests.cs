#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Applying edits one at a time reallocates and recopies the whole content stream per edit,
// which is quadratic in the hit count on a large stream. These pin the single-pass cost
// model: allocation must track the output size, not hits x streamSize.
public class ContentEditsScalingTests
{
    private static long Apply(int streamSize, int hits)
    {
        var source = new byte[streamSize];
        var edits = new List<ContentEdit>();
        var stride = streamSize / (hits + 1);
        for (var i = 0; i < hits; i++)
        {
            edits.Add(new ContentEdit(i * stride, (i * stride) + 4, new byte[4]));
        }

        ContentEdits.Apply(source, [.. edits]);

        var before = GC.GetAllocatedBytesForCurrentThread();
        ContentEdits.Apply(source, [.. edits]);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Apply_AllocationDoesNotScaleWithHitCount()
    {
        const int size = 360_000;

        var ten = Apply(size, 10);
        var twoHundred = Apply(size, 200);

        // Splicing one edit at a time cost ~456KB per additional hit (10 hits -> 54MB,
        // 200 -> 141MB). One forward pass allocates the single output buffer regardless.
        Assert.True(twoHundred < ten * 2,
            $"Applying 200 edits allocated {twoHundred} bytes against {ten} for 10: allocation scales with hit count.");
        Assert.True(twoHundred < size * 3,
            $"Applying 200 edits to a {size} byte stream allocated {twoHundred} bytes.");
    }
}
