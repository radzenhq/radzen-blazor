#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ViewerPreferencesTests
{
    private static PortableDocument Document()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (v) Tj ET"));
        return document;
    }

    private static DictionaryObject Catalog(byte[] pdf) => ContentTestHelpers.Catalog(DocumentReader.Parse(pdf));

    private static string NameOf(DocumentReader reader, DictionaryObject dict, string key)
        => Assert.IsType<NameObject>(reader.Resolve(dict[key])).Value;

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

        var reader = DocumentReader.Parse(document.ToArray());
        var catalog = ContentTestHelpers.Catalog(reader);

        Assert.Equal("TwoColumnLeft", NameOf(reader, catalog, "PageLayout"));
        Assert.Equal("UseOutlines", NameOf(reader, catalog, "PageMode"));

        var prefs = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["ViewerPreferences"]));
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["HideToolbar"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["HideMenubar"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["FitWindow"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["CenterWindow"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(prefs["DisplayDocTitle"])).Value);
        Assert.Equal("R2L", NameOf(reader, prefs, "Direction"));
    }

    [Fact]
    public void OnlyExplicitOptions_AreEmitted()
    {
        var document = Document();
        document.ViewerPreferences = new ViewerPreferences { FitWindow = true };

        var catalog = Catalog(document.ToArray());
        Assert.False(catalog.ContainsKey("PageLayout"));
        Assert.False(catalog.ContainsKey("PageMode"));
        var prefs = Assert.IsType<DictionaryObject>(catalog["ViewerPreferences"]);
        Assert.True(prefs.ContainsKey("FitWindow"));
        Assert.False(prefs.ContainsKey("HideToolbar"));
        Assert.False(prefs.ContainsKey("Direction"));
    }

    [Fact]
    public void NoViewerPreferences_EmitsNothing_AndByteIdentical()
    {
        var bytes = Document().ToArray();
        Assert.Equal(bytes, Document().ToArray());

        var text = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("/ViewerPreferences", text);
        Assert.DoesNotContain("/PageLayout", text);
        Assert.DoesNotContain("/PageMode", text);

        Assert.False(Catalog(bytes).ContainsKey("ViewerPreferences"));
    }
}
