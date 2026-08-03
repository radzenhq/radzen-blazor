#nullable enable
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PageResourceDictionaryTests
{
    [Fact]
    public void FacadePageWithoutResources_StillCarriesAResourceDictionary()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4).SetContent(
            Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 720 Td (Body) Tj ET"));

        var reader = DocumentReader.Parse(document.ToArray());
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;

        Assert.True(page.TryGetValue("Resources", out var resources), "the page carries /Resources");
        Assert.IsType<DictionaryObject>(reader.Resolve(resources!));
    }
}
