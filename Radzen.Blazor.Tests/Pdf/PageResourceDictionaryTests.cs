#nullable enable
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class PageResourceDictionaryTests
{
    [Fact]
    public void FacadePageWithoutResources_StillCarriesAResourceDictionary()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4).SetContent(
            Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 720 Td (Body) Tj ET"));

        Carries("page", "/Resources <<", Line(Emit(document), "/Type /Page "));
    }
}
