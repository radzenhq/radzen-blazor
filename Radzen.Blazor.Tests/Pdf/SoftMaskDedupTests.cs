#nullable enable

using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Core;
namespace Radzen.Blazor.Pdf.Tests;

public class SoftMaskDedupTests
{
    private static PagePlan Plan() => new() { Size = PageSizes.A4 };

    private static BoxShadowPaint Shadow() => new(
        Color.FromArgb(160, 0, 0, 0),
        BlurRadius: 8,
        OffsetX: 2,
        OffsetY: 3,
        Spread: 1);

    private static int SoftMaskStates(PagePlan plan) => plan.Resources.ExtGStates.Count(s => s.SoftMask is not null);

    [Fact]
    public void IdenticalShadows_ShareOneSoftMaskState()
    {
        var plan = Plan();
        var bounds = PdfRect.FromSize(50, 500, 200, 100);

        SoftMask.EmitBoxShadow(plan, bounds, 6, Shadow(), SemanticArtifactKind.LayoutDecoration);
        SoftMask.EmitBoxShadow(plan, bounds, 6, Shadow(), SemanticArtifactKind.LayoutDecoration);

        Assert.Equal(1, SoftMaskStates(plan));
        Assert.Equal(2, plan.Fills.Count);
        Assert.Equal(plan.Fills[0].ExtGState, plan.Fills[1].ExtGState);
    }

    [Fact]
    public void DifferentPositions_KeepSeparateSoftMaskStates()
    {
        var plan = Plan();

        SoftMask.EmitBoxShadow(plan, PdfRect.FromSize(50, 500, 200, 100), 6, Shadow(), SemanticArtifactKind.LayoutDecoration);
        SoftMask.EmitBoxShadow(plan, PdfRect.FromSize(50, 200, 200, 100), 6, Shadow(), SemanticArtifactKind.LayoutDecoration);

        Assert.Equal(2, SoftMaskStates(plan));
        Assert.NotEqual(plan.Fills[0].ExtGState, plan.Fills[1].ExtGState);
    }
}
