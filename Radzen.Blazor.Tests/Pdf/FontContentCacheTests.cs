#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class FontContentCacheTests
{
    private const string Family = "Liberation Sans";
    private static byte[] SansBytes() => PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

    private static byte[] SerifBytes() => PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf");

    private static SfntFont RegisterIdiomatic(string family, byte[] bytes)
    {
        var fonts = new FontCollection();
        fonts.Register(family, new MemoryStream(bytes));
        return fonts.ResolvePrimarySfnt(new Font { Family = family });
    }

    [Fact]
    public void SameContentViaFreshNonExposedStreamsParsesOnce()
    {
        var first = RegisterIdiomatic(Family, SansBytes());
        var second = RegisterIdiomatic(Family, SansBytes());

        Assert.Same(first, second);
    }

    [Fact]
    public void SameContentAcrossDocumentsParsesOnce()
    {
        var firstBuilder = new Document();
        firstBuilder.Fonts.Register(Family, new MemoryStream(SansBytes()));

        var secondBuilder = new Document();
        secondBuilder.Fonts.Register(Family, new MemoryStream(SansBytes()));

        var font = new Font { Family = Family };
        Assert.Same(
            firstBuilder.Fonts.ResolvePrimarySfnt(font),
            secondBuilder.Fonts.ResolvePrimarySfnt(font));
    }

    [Fact]
    public void DistinctFontContentsDoNotCollide()
    {
        var sans = RegisterIdiomatic("Sans", SansBytes());
        var serif = RegisterIdiomatic("Serif", SerifBytes());

        Assert.NotSame(sans, serif);
        Assert.NotEqual(sans.FamilyName, serif.FamilyName);
    }

    [Fact]
    public void SameLengthDifferentContentDoesNotCollide()
    {
        var bytes = SansBytes();
        var tweaked = (byte[])bytes.Clone();
        tweaked[^1] ^= 0xFF;

        var first = RegisterIdiomatic(Family, bytes);
        var second = RegisterIdiomatic(Family, tweaked);

        Assert.NotSame(first, second);
    }

    [Fact]
    public void ContentCacheDoesNotRootFontAfterAllReferencesDropped()
    {
        var reference = RegisterIdiomaticAndDrop();

        for (var i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(reference.IsAlive);
    }

    [Fact]
    public void CollectedEntryIsReparsedOnNextRegistration()
    {
        var reference = RegisterIdiomaticAndDrop();

        for (var i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        var again = RegisterIdiomatic(Family, SansBytes());
        Assert.Equal(Family, again.FamilyName);
    }

    [Fact]
    public void CffBackedFontParsesOnceAcrossIdiomaticRegistrations()
    {
        var bytes = PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf");
        var first = RegisterIdiomatic("Noto Sans SC", bytes);
        var second = RegisterIdiomatic("Noto Sans SC", (byte[])bytes.Clone());

        Assert.Same(first, second);
    }

    [Fact]
    public void CollectionBackedFontParsesOnceAcrossIdiomaticRegistrations()
    {
        var bytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-RegBold.ttc");
        var first = RegisterIdiomatic(Family, bytes);
        var second = RegisterIdiomatic(Family, (byte[])bytes.Clone());

        Assert.Same(first, second);
    }

    private static byte[] UniqueSansBytes()
    {
        var bytes = SansBytes();
        var unique = new byte[bytes.Length + 26];
        Array.Copy(bytes, unique, bytes.Length);
        "RZLEAK-CONTENTCACHE-UNIQUE"u8.CopyTo(unique.AsSpan(bytes.Length));
        return unique;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterIdiomaticAndDrop()
        => new(RegisterIdiomatic(Family, UniqueSansBytes()));
}
