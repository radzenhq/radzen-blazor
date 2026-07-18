#nullable enable

using System.Collections.Generic;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TextCompositionOrderingTests
{
    [Fact]
    public void Compare_OrdersDriftingBaselinesTransitively()
    {
        var placements = new List<TextComposition.Placement>();
        for (var i = 0; i < 24; i++)
        {
            placements.Add(new TextComposition.Placement(i * 0.4, i * 5.0, 0, 10));
        }

        for (var i = 0; i < placements.Count; i++)
        {
            for (var j = 0; j < placements.Count; j++)
            {
                for (var k = 0; k < placements.Count; k++)
                {
                    if (TextComposition.Compare(placements[i], placements[j]) <= 0
                        && TextComposition.Compare(placements[j], placements[k]) <= 0)
                    {
                        Assert.True(
                            TextComposition.Compare(placements[i], placements[k]) <= 0,
                            $"Non-transitive at ({i},{j},{k}).");
                    }
                }
            }
        }
    }
}
