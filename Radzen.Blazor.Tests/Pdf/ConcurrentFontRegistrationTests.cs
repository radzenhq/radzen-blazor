#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class ConcurrentFontRegistrationTests
{
    private static byte[] SansBytes() => PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

    private static byte[] SerifBytes() => PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

    [Fact]
    public void ConcurrentRegistrationsOfDifferentFontsBothSucceed()
    {
        var sansFonts = new FontCollection();
        var serifFonts = new FontCollection();
        using var start = new Barrier(2);

        Task RegisterAsync(FontCollection fonts, string family, byte[] bytes) => Task.Run(() =>
        {
            start.SignalAndWait();
            fonts.Register(family, new MemoryStream(bytes));
        });

        var sansTask = RegisterAsync(sansFonts, "Sans", SansBytes());
        var serifTask = RegisterAsync(serifFonts, "Serif", SerifBytes());

        Task.WaitAll(sansTask, serifTask);

        var sans = sansFonts.ResolvePrimarySfnt(new Font { Family = "Sans" });
        var serif = serifFonts.ResolvePrimarySfnt(new Font { Family = "Serif" });

        Assert.NotSame(sans, serif);
        Assert.NotEqual(sans.FamilyName, serif.FamilyName);
    }

    [Fact]
    public void ConcurrentRegistrationsOfSameFontContentShareOneParse()
    {
        var bytes = SansBytes();
        var firstFonts = new FontCollection();
        var secondFonts = new FontCollection();
        using var start = new Barrier(2);

        Task<Radzen.Documents.Fonts.Sfnt.SfntFont> RegisterAsync(FontCollection fonts) => Task.Run(() =>
        {
            start.SignalAndWait();
            fonts.Register("Sans", new MemoryStream(bytes));
            return fonts.ResolvePrimarySfnt(new Font { Family = "Sans" });
        });

        var firstTask = RegisterAsync(firstFonts);
        var secondTask = RegisterAsync(secondFonts);

        Task.WaitAll(firstTask, secondTask);

        Assert.Same(firstTask.Result, secondTask.Result);
    }
}
