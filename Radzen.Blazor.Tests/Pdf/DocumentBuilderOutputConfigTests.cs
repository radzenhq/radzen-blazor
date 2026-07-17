#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentBuilderOutputConfigTests
{
    private static DocumentBuilder Authored()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Page one", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Page two", BuildTestSupport.Latin);
        return builder;
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

    [Fact]
    public void ViewerPreferences_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaBuilder = Authored();
        viaBuilder.ViewerPreferences = Prefs();

        var oldWay = Authored().Build();
        oldWay.ViewerPreferences = Prefs();

        Assert.Equal(oldWay.ToArray(), viaBuilder.ToArray());
    }

    [Fact]
    public void PageLabels_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaBuilder = Authored();
        viaBuilder.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });

        var oldWay = Authored().Build();
        oldWay.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });

        Assert.Equal(oldWay.ToArray(), viaBuilder.ToArray());
    }

    [Fact]
    public void FormFields_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaBuilder = Authored();
        viaBuilder.FormFields.Add(Field());

        var oldWay = Authored().Build();
        oldWay.FormFields.Add(Field());

        Assert.Equal(oldWay.ToArray(), viaBuilder.ToArray());

        static TextFieldDefinition Field() => new("Name")
        {
            PageIndex = 0,
            X = 72,
            Y = 700,
            Width = 180,
            Height = 18,
            Value = "hello",
            Font = new Font { Name = "Helvetica", Size = 12 },
        };
    }

    [Fact]
    public void CombinedSurface_ThroughBuilder_EqualsOldMutatePath()
    {
        var viaBuilder = Authored();
        viaBuilder.ViewerPreferences = Prefs();
        viaBuilder.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });
        viaBuilder.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.Decimal, Prefix = "B-", Start = 3 });

        var oldWay = Authored().Build();
        oldWay.ViewerPreferences = Prefs();
        oldWay.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });
        oldWay.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.Decimal, Prefix = "B-", Start = 3 });

        Assert.Equal(oldWay.ToArray(), viaBuilder.ToArray());
    }

    [Fact]
    public void DefaultOutputConfig_LeavesBuildByteIdentical()
    {
        var bare = Authored().ToArray();
        var untouched = Authored().ToArray();
        Assert.Equal(bare, untouched);

        var builder = Authored();
        Assert.Null(builder.ViewerPreferences);
        Assert.Empty(builder.PageLabels);
        Assert.Empty(builder.FormFields);
        Assert.Equal(bare, builder.ToArray());
    }
}
