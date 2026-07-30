#nullable enable
using System;
using System.IO;
using System.Linq;
using Radzen.Documents.Fonts;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class FontCollectionFreezeTests
{
    private const string Sans = "Liberation Sans";

    private const string Serif = "Liberation Serif";

    private static MemoryStream SansBytes()
        => new(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static MemoryStream SerifBytes()
        => new(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf"));

    [Fact]
    public void RegisteringAfterAMeasurement_RefreshesTheFrozenConfiguration()
    {
        var fonts = new FontCollection();
        var font = new Font { Family = Sans, Size = 12 };

        Assert.Throws<InvalidOperationException>(() => fonts.MeasureText("Radzen", font));

        fonts.Register(Sans, SansBytes());

        Assert.True(fonts.MeasureText("Radzen", font) > 0);
        Assert.Equal(Sans, Assert.Single(fonts.Snapshot().Faces).Family);
    }

    [Fact]
    public void EnablingKerningAfterAMeasurement_RefreshesTheFrozenConfiguration()
    {
        var fonts = new FontCollection();
        fonts.Register(Sans, SansBytes());
        var font = new Font { Family = Sans, Size = 12 };

        var unkerned = fonts.MeasureText("AV Wa To", font);
        fonts.EnableKerning = true;

        Assert.True(fonts.Snapshot().EnableKerning);
        Assert.NotEqual(unkerned, fonts.MeasureText("AV Wa To", font));
    }

    [Fact]
    public void SettingTheFallbackChainAfterAMeasurement_RefreshesTheFrozenConfiguration()
    {
        var fonts = new FontCollection();
        fonts.Register(Sans, SansBytes());
        fonts.Register(Serif, SerifBytes());
        var font = new Font { Family = Sans, Size = 12 };

        fonts.MeasureText("Radzen", font);
        fonts.SetFallback(Serif);

        Assert.Equal([Serif], fonts.Snapshot().Fallback.ToArray());
    }

    [Fact]
    public void ASnapshotTakenBeforeARegistration_IsUnaffectedByIt()
    {
        var fonts = new FontCollection();
        fonts.Register(Sans, SansBytes());

        var taken = fonts.Snapshot();
        fonts.Register(Serif, SerifBytes());

        Assert.Equal([Sans], taken.Faces.Select(face => face.Family).ToArray());
        Assert.Equal([Sans, Serif], fonts.Snapshot().Faces.Select(face => face.Family).ToArray());
    }

    [Fact]
    public void RepeatedFreezes_WithoutConfigurationChanges_ShareOneSnapshot()
    {
        var fonts = new FontCollection();
        fonts.Register(Sans, SansBytes());

        Assert.Same(fonts.Shaper(), fonts.Shaper());

        fonts.EnableKerning = true;

        Assert.Same(fonts.Shaper(), fonts.Shaper());
    }
}
