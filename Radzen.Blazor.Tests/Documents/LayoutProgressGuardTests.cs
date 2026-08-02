#nullable enable
using System;
using Radzen.Documents.Layout;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class LayoutProgressGuardTests
{
    [Fact]
    public void AdvancingPositions_AreAllowedIndefinitely()
    {
        var guard = new LayoutProgressGuard("Work");

        Assert.Null(Record.Exception(() =>
        {
            for (var position = 0; position < 1000; position++)
            {
                guard.Reached(position);
            }
        }));
    }

    [Fact]
    public void OneNonAdvancingStep_IsAllowedBecauseThatStepMovedToAFreshPage()
    {
        var guard = new LayoutProgressGuard("Work");

        Assert.Null(Record.Exception(() =>
        {
            for (var position = 0; position < 1000; position++)
            {
                guard.Reached(position);
                guard.Reached(position);
            }
        }));
    }

    [Fact]
    public void TwoStalledStepsInARow_FailLoudly()
    {
        var guard = new LayoutProgressGuard("Paragraph pagination over 3 lines");
        guard.Reached(7);
        guard.Reached(7);

        var error = Assert.Throws<InvalidOperationException>(() => guard.Reached(7));

        Assert.Equal(
            "Paragraph pagination over 3 lines stopped making progress at 7; "
            + "every step must either place content or start a new page.",
            error.Message);
    }

    [Fact]
    public void StallCount_ResetsWhenProgressResumes()
    {
        var guard = new LayoutProgressGuard("Work");
        guard.Reached(0);
        guard.Reached(0);

        Assert.Null(Record.Exception(() =>
        {
            guard.Reached(1);
            guard.Reached(1);
            guard.Reached(2);
            guard.Reached(2);
        }));
    }
}
