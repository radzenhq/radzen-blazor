#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RendererCaptureIsolationTests
{
    private static Document Authored()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin);
        return document;
    }

    private static string Save(PortableDocument document)
        => Encoding.Latin1.GetString(document.ToArray());

    [Fact]
    public void RoleMapMutatedAfterRender_LeavesTheReportedMapAndTheSavedBytesInAgreement()
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("Lead");
        BuildTestSupport.AddText(document.Sections.Add(), "See the note", BuildTestSupport.Latin).StyleName = "Lead";

        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        renderer.RoleMap.Add("Lead", "P");

        var pdf = renderer.Render(document);
        renderer.RoleMap.Add("Sidebar", "P");

        Assert.NotSame(renderer.RoleMap, pdf.RoleMap);
        Assert.Equal(1, pdf.RoleMap.Count);
        Assert.True(pdf.RoleMap.Contains("Lead"));
        Assert.False(pdf.RoleMap.Contains("Sidebar"));

        var saved = Save(pdf);
        Assert.Contains("/Lead", saved, System.StringComparison.Ordinal);
        Assert.DoesNotContain("/Sidebar", saved, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ProducerOnRenderer_IsWrittenToTheInfoDictionary()
    {
        var renderer = new DocumentRenderer { Producer = "Acme Publisher 3.0" };

        Assert.Contains("Acme Publisher 3.0", Encoding.Latin1.GetString(renderer.ToArray(Authored())), System.StringComparison.Ordinal);
    }
}
