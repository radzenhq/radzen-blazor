#nullable enable

using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class Type0GidMapReuseTests
{
    private static byte[] BuildMixedSubsetDocument()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.RegisterCjk(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice 12345 - Total 4200.00", BuildTestSupport.Latin, 16);
        BuildTestSupport.AddText(section, "发票中文 金额", BuildTestSupport.Cjk, 14);
        return new DocumentRenderer().ToArray(document);
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
