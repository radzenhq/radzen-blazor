using System.Text;
using Xunit;

namespace Radzen.Blazor.Tests;

public class RadzenQREncoderTests
{
    [Theory]
    [InlineData("x", RadzenQREcc.Low)]
    [InlineData("https://blazor.radzen.com", RadzenQREcc.Medium)]
    [InlineData("The quick brown fox jumps over the lazy dog 0123456789", RadzenQREcc.High)]
    public void EncodeBytesProducesTheSameMatrixAsEncodeUtf8(string value, RadzenQREcc ecc)
    {
        var fromString = RadzenQREncoder.EncodeUtf8(value, ecc);
        var fromBytes = RadzenQREncoder.EncodeBytes(Encoding.UTF8.GetBytes(value), ecc);

        Assert.Equal(fromString, fromBytes);
    }
}
