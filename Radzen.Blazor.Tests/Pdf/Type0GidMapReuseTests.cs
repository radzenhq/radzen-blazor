#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class Type0GidMapReuseTests
{
    private static byte[] BuildMixedSubsetDocument()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        BuildTestSupport.RegisterCjk(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice 12345 - Total 4200.00", BuildTestSupport.Latin, 16);
        BuildTestSupport.AddText(section, "你好世界 金额", BuildTestSupport.Cjk, 14);
        return builder.ToArray();
    }

    [Fact]
    public void MixedSubsetDocument_BuildsByteIdentically()
    {
        var baseline = BuildMixedSubsetDocument();
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(baseline, BuildMixedSubsetDocument());
        }
    }
}
