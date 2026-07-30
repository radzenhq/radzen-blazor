#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Content;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class TjAdjustmentAgreementTests
{
    private static string Adjust(double pointsDelta, double denominator)
    {
        using var writer = new ContentWriter();
        writer.WriteTjAdjustment(pointsDelta, denominator);
        return Encoding.Latin1.GetString(writer.ToArray());
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.0000005)]
    [InlineData(-0.0000005)]
    public void NegligiblePointsDelta_WritesNothing(double pointsDelta)
    {
        Assert.Equal(string.Empty, Adjust(pointsDelta, 10.0));
    }

    [Fact]
    public void AboveEpsilon_WritesScaledThousandths()
    {
        Assert.Equal(" 50 ", Adjust(0.5, 10.0));
    }

    [Fact]
    public void SignIsCarriedFromTheCallSite()
    {
        Assert.Equal(" -50 ", Adjust(-0.5, 10.0));
    }

    [Fact]
    public void PointsDeltaJustAboveEpsilon_IsEmitted()
    {
        Assert.NotEqual(string.Empty, Adjust(0.0001, 10.0));
    }
}
