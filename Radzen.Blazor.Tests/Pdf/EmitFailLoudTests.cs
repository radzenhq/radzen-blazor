#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class EmitFailLoudTests
{
    [Fact]
    public void RotatedGradientBackground_Throws()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Rotation = 15,
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.FromRgb(255, 0, 0)),
                new GradientStop(1, Color.FromRgb(0, 0, 255))),
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Rotated"));

        var error = Assert.Throws<NotSupportedException>(() =>
        {
            using var stream = new MemoryStream();
            var builderRenderer = new DocumentRenderer();
            builderRenderer.SaveToStream(document, stream);
        });
        Assert.Contains("gradient", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnrotatedGradientBackground_StillEmitsThePattern()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            BackgroundGradient = new LinearGradient(
                0, 0, 100, 0,
                new GradientStop(0, Color.FromRgb(255, 0, 0)),
                new GradientStop(1, Color.FromRgb(0, 0, 255))),
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Upright"));

        Assert.Contains("/Pattern cs", FeatureEmissionTestHelpers.Content(document), StringComparison.Ordinal);
    }

    [Fact]
    public void RotatedSolidBackground_StillSaves()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Rotation = 15,
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(200, 200, 200),
        });
        container.Blocks.Add(FeatureEmissionTestHelpers.Text("Rotated"));

        using var stream = new MemoryStream();
        builderRenderer.SaveToStream(document, stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void PdfA_WithCmykImage_Throws()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello conformance", BuildTestSupport.Latin);
        section.Blocks.AddImage(PdfTestResources.Open("Images/cmyk.jpg"));
        var builderRenderer = new DocumentRenderer();
        builderRenderer.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            using var stream = new MemoryStream();
            builderRenderer.SaveToStream(document, stream);
        });
        Assert.Contains("DeviceCMYK", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfA_WithRgbImage_Saves()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Hello conformance", BuildTestSupport.Latin);
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        builderRenderer.Conformance = PdfAConformance.PdfA2B;

        using var stream = new MemoryStream();
        builderRenderer.SaveToStream(document, stream);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public void CmykImage_WithoutConformance_Saves()
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer();
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();
        section.Blocks.AddImage(PdfTestResources.Open("Images/cmyk.jpg"));

        using var stream = new MemoryStream();
        builderRenderer.SaveToStream(document, stream);
        Assert.True(stream.Length > 0);
    }
}
