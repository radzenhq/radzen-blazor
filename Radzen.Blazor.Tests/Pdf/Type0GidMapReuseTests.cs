#nullable enable

using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The compact gid renumbering must be computed once in the generator and reused by the
// Type0 embedder. A representative document that embeds both a TrueType (glyf) and a
// CFF Type0 subset must serialize deterministically and identically build-to-build so a
// shared map cannot silently diverge from the content-stream codes.
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
