#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

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

        Assert.True(twoHundred < ten * 2,
            $"Applying 200 edits allocated {twoHundred} bytes against {ten} for 10: allocation scales with hit count.");
        Assert.True(twoHundred < size * 3,
            $"Applying 200 edits to a {size} byte stream allocated {twoHundred} bytes.");
    }
}
