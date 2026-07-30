#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class StyledFaceRegistrationTests
{
    private const string Family = "Liberation Sans";

    private static void RegisterFace(FontCollection fonts, string resource, bool bold, bool italic)
    {
        using var stream = new MemoryStream(PdfTestResources.ReadAllBytes(resource));
        var styled = typeof(FontCollection).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "Register"
                && m.GetParameters() is { Length: 4 } p
                && p[0].ParameterType == typeof(string)
                && p[1].ParameterType == typeof(Stream)
                && p[2].ParameterType == typeof(bool)
                && p[3].ParameterType == typeof(bool));

        if (styled != null)
        {
            styled.Invoke(fonts, [Family, stream, bold, italic]);
        }
        else
        {
            fonts.Register(Family, stream);
        }
    }

    private static void RegisterBoth(FontCollection fonts)
    {
        RegisterFace(fonts, "Fonts/LiberationSans-Regular.ttf", bold: false, italic: false);
        RegisterFace(fonts, "Fonts/LiberationSans-Bold.ttf", bold: true, italic: false);
    }

    [Fact]
    public void BoldRun_UsesRegisteredBoldFace()
    {
        var document = new Document();
        RegisterBoth(document.Fonts);

        var section = document.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var regular = paragraph.Inlines.Add("Regular ");
        regular.Font.Family = Family;
        var bold = paragraph.Inlines.Add("Bold");
        bold.Font.Family = Family;
        bold.Font.Bold = true;

        var reader = BuildTestSupport.Read(document);
        var type0 = BuildTestSupport.Type0Fonts(reader);

        Assert.Equal(2, type0.Count);
        var names = type0.Select(f => BuildTestSupport.Name(reader, f, "BaseFont")).ToList();
        Assert.Contains(names, n => n.Contains("Bold", StringComparison.Ordinal));
        Assert.Contains(names, n => !n.Contains("Bold", StringComparison.Ordinal));
    }

    [Fact]
    public void Measurement_IsStyleAware_WhenBoldFaceRegistered()
    {
        var fonts = new FontCollection();
        RegisterBoth(fonts);

        var regularWidth = fonts.MeasureText("Hello Width", new Font { Family = Family, Size = 12 });
        var boldWidth = fonts.MeasureText("Hello Width", new Font { Family = Family, Size = 12, Bold = true });

        Assert.NotEqual(regularWidth, boldWidth);
    }

    [Fact]
    public void BoldRequested_RegularOnlyRegistered_FallsBackToRegularFace()
    {
        var document = new Document();
        RegisterFace(document.Fonts, "Fonts/LiberationSans-Regular.ttf", bold: false, italic: false);

        var section = document.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("Bold wanted");
        run.Font.Family = Family;
        run.Font.Bold = true;

        var reader = BuildTestSupport.Read(document);
        Assert.Single(BuildTestSupport.Type0Fonts(reader));

        var reloaded = BuildTestSupport.Reload(document);
        Assert.Contains("Bold wanted", reloaded.ExtractText(), StringComparison.Ordinal);
    }
}
