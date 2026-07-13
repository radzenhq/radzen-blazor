#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class StencilImageTests
{
    [Fact]
    public void Stencil_EmitsFillColorBeforeDo()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        using var stream = new MemoryStream(ImageTestHelpers.OneBitGrayPng(8, 8));
        var image = section.Blocks.AddImage(stream);
        image.Width = Unit.FromPoint(48);
        image.Height = Unit.FromPoint(48);
        image.Stencil = true;
        image.StencilColor = Color.FromRgb(255, 0, 0);

        var content = FeatureEmissionTestHelpers.Content(builder);
        Assert.Contains("1 0 0 rg", content, StringComparison.Ordinal);
        Assert.Contains(" Do", content, StringComparison.Ordinal);
    }
}
