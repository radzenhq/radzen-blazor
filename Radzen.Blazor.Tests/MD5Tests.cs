using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests;

public class MD5Tests
{
    [Theory]
    [InlineData(0, "d41d8cd98f00b204e9800998ecf8427e")]
    [InlineData(54, "33dd62a2df6538daf1cf821d9cde61f9")]
    [InlineData(55, "6912ee65fff2d9f9ce2508cddf8bcda0")]
    [InlineData(56, "51fdd1acda72405dfdfa03fcb85896d7")]
    [InlineData(57, "5320ef4c17ef34a0cf2db763338d25eb")]
    [InlineData(58, "9f4f41b5cde885f94cfc0e06e78f929d")]
    [InlineData(59, "e39965bc00ecacd90fd875f77eff499a")]
    [InlineData(60, "63ed72093ae09e2c8553ee069e63d702")]
    [InlineData(61, "0d08fc14ac5baa37792377355dbad0ae")]
    [InlineData(62, "f3cdffe2e160a061754a06dafcfd688b")]
    [InlineData(63, "48a6295221902e8e0938f773a7185e72")]
    [InlineData(64, "b2d3f56bc197fd985d5965079b5e7148")]
    [InlineData(65, "8bd7053801c768420faf816fadba971c")]
    public void InputsAroundThePaddingBoundaryHashLikeRfc1321(int length, string expected)
    {
        var input = Enumerable.Range(0, length).Select(index => (byte)(index % 256)).ToArray();

        Assert.Equal(expected, MD5.Calculate(input));
    }
}
