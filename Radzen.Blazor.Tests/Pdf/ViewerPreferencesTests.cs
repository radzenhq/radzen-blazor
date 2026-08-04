#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class ViewerPreferencesTests
{
    private static PortableDocument Document()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (v) Tj ET"));
        return document;
    }

    private static string Catalog(PortableDocument document) => Line(Emit(document), "/Type /Catalog");

    private static string Preferences(string catalog)
        => Shaped("catalog /ViewerPreferences", @"/ViewerPreferences << ([^>]*)>>", catalog).Groups[1].Value;

    [Fact]
    public void ViewerPreferences_EmitsCatalogAndDictionaryEntries()
    {
        var document = Document();
        document.ViewerPreferences = new ViewerPreferences
        {
            PageLayout = PdfPageLayout.TwoColumnLeft,
            PageMode = PdfPageMode.UseOutlines,
            HideToolbar = true,
            HideMenubar = true,
            FitWindow = true,
            CenterWindow = true,
            DisplayDocTitle = true,
            Direction = PdfReadingDirection.RightToLeft,
        };

        var catalog = Catalog(document);

        Carries("catalog", "/PageLayout /TwoColumnLeft", catalog);
        Carries("catalog", "/PageMode /UseOutlines", catalog);

        var preferences = Preferences(catalog);

        Carries("viewer preferences", "/HideToolbar true", preferences);
        Carries("viewer preferences", "/HideMenubar true", preferences);
        Carries("viewer preferences", "/FitWindow true", preferences);
        Carries("viewer preferences", "/CenterWindow true", preferences);
        Carries("viewer preferences", "/DisplayDocTitle true", preferences);
        Carries("viewer preferences", "/Direction /R2L", preferences);
    }

    [Fact]
    public void OnlyExplicitOptions_AreEmitted()
    {
        var document = Document();
        document.ViewerPreferences = new ViewerPreferences { FitWindow = true };

        var catalog = Catalog(document);

        Lacks("catalog", "/PageLayout", catalog);
        Lacks("catalog", "/PageMode", catalog);

        var preferences = Preferences(catalog);

        Carries("viewer preferences", "/FitWindow", preferences);
        Lacks("viewer preferences", "/HideToolbar", preferences);
        Lacks("viewer preferences", "/Direction", preferences);
    }

    [Fact]
    public void NoViewerPreferences_EmitsNothing_AndByteIdentical()
    {
        Assert.Equal(Document().ToArray(), Document().ToArray());

        var catalog = Catalog(Document());

        Lacks("catalog", "/ViewerPreferences", catalog);
        Lacks("catalog", "/PageLayout", catalog);
        Lacks("catalog", "/PageMode", catalog);
    }
}
