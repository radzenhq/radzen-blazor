#nullable enable
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentOutputConfigTests
{
    private static Document Authored()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Page one", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Page two", BuildTestSupport.Latin);
        return document;
    }

    private static ViewerPreferences Prefs() => new()
    {
        PageLayout = PdfPageLayout.TwoColumnLeft,
        PageMode = PdfPageMode.UseOutlines,
        HideToolbar = true,
        FitWindow = true,
        DisplayDocTitle = true,
        Direction = PdfReadingDirection.RightToLeft,
    };

    private static PortableDocument Load(byte[] bytes)
        => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    [Fact]
    public void ViewerPreferences_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaDocument = Authored();
        var viaBuilderRenderer = new DocumentRenderer();
        viaBuilderRenderer.ViewerPreferences = Prefs();

        var oldWay = new DocumentRenderer().Render(Authored());
        oldWay.ViewerPreferences = Prefs();

        var oldPreferences = Load(oldWay.ToArray()).ViewerPreferences!;
        var renderedPreferences = Load(viaBuilderRenderer.ToArray(viaDocument)).ViewerPreferences!;
        Assert.Equal(oldPreferences.PageLayout, renderedPreferences.PageLayout);
        Assert.Equal(oldPreferences.PageMode, renderedPreferences.PageMode);
        Assert.Equal(oldPreferences.HideToolbar, renderedPreferences.HideToolbar);
        Assert.Equal(oldPreferences.FitWindow, renderedPreferences.FitWindow);
        Assert.Equal(oldPreferences.DisplayDocTitle, renderedPreferences.DisplayDocTitle);
        Assert.Equal(oldPreferences.Direction, renderedPreferences.Direction);
    }

    [Fact]
    public void PageLabels_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaDocument = Authored();
        var viaBuilderRenderer = new DocumentRenderer();
        viaBuilderRenderer.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });

        var oldWay = new DocumentRenderer().Render(Authored());
        oldWay.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });

        var oldLabel = Assert.Single(Load(oldWay.ToArray()).PageLabels);
        var renderedLabel = Assert.Single(Load(viaBuilderRenderer.ToArray(viaDocument)).PageLabels);
        Assert.Equal(oldLabel.StartPage, renderedLabel.StartPage);
        Assert.Equal(oldLabel.Style, renderedLabel.Style);
    }

    [Fact]
    public void FormFields_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaDocument = Authored();
        var viaBuilderRenderer = new DocumentRenderer();
        viaBuilderRenderer.FormFields.Add(Field());

        var oldWay = new DocumentRenderer().Render(Authored());
        oldWay.FormFields.Add(Field());

        Assert.Equal(
            Load(oldWay.ToArray()).AcroForm!.FieldNames,
            Load(viaBuilderRenderer.ToArray(viaDocument)).AcroForm!.FieldNames);

        static TextFieldDefinition Field() => new("Name")
        {
            PageIndex = 0,
            X = 72,
            Y = 700,
            Width = 180,
            Height = 18,
            Value = "hello",
            Font = new Font { Family = "Helvetica", Size = 12 },
        };
    }

    [Fact]
    public void CombinedSurface_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaDocument = Authored();
        var viaBuilderRenderer = new DocumentRenderer();
        viaBuilderRenderer.ViewerPreferences = Prefs();
        viaBuilderRenderer.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });
        viaBuilderRenderer.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.Decimal, Prefix = "B-", Start = 3 });

        var oldWay = new DocumentRenderer().Render(Authored());
        oldWay.ViewerPreferences = Prefs();
        oldWay.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });
        oldWay.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.Decimal, Prefix = "B-", Start = 3 });

        var oldDocument = Load(oldWay.ToArray());
        var renderedDocument = Load(viaBuilderRenderer.ToArray(viaDocument));
        Assert.Equal(oldDocument.ViewerPreferences!.PageLayout, renderedDocument.ViewerPreferences!.PageLayout);
        Assert.Equal(
            oldDocument.PageLabels.Select(static label => (label.StartPage, label.Style, label.Prefix, label.Start)),
            renderedDocument.PageLabels.Select(static label => (label.StartPage, label.Style, label.Prefix, label.Start)));
    }

    [Fact]
    public void DefaultOutputConfig_LeavesBuildByteIdentical()
    {
        var bare = new DocumentRenderer().ToArray(Authored());
        var untouched = new DocumentRenderer().ToArray(Authored());
        Assert.Equal(bare, untouched);

        var builderRenderer = new DocumentRenderer();
        var document = Authored();
        Assert.Null(builderRenderer.ViewerPreferences);
        Assert.Empty(builderRenderer.PageLabels);
        Assert.Empty(builderRenderer.FormFields);
        Assert.Equal(bare, builderRenderer.ToArray(document));
    }
}
