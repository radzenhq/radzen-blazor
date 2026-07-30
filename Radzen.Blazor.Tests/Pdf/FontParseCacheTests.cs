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

public class FontParseCacheTests
{
    private const string Family = "Liberation Sans";

    private static byte[] FontBytes() => PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

    private static MemoryStream Exposed(byte[] bytes)
        => new(bytes, 0, bytes.Length, writable: false, publiclyVisible: true);

    private static SfntFont RegisterAndResolve(byte[] bytes, Stream stream)
    {
        var fonts = new FontCollection();
        fonts.Register(Family, stream);
        return fonts.ResolvePrimarySfnt(new Font { Family = Family });
    }

    [Fact]
    public void SameArrayIsParsedOnceAcrossCollections()
    {
        var bytes = FontBytes();
        var first = RegisterAndResolve(bytes, Exposed(bytes));
        var second = RegisterAndResolve(bytes, Exposed(bytes));

        Assert.Same(first, second);
    }

    [Fact]
    public void CloneOfSameContentSharesParse()
    {
        var bytes = FontBytes();
        var clone = (byte[])bytes.Clone();
        var first = RegisterAndResolve(bytes, Exposed(bytes));
        var second = RegisterAndResolve(clone, Exposed(clone));

        Assert.Same(first, second);
    }

    [Fact]
    public void NonExposedStreamsWithSameContentShareParse()
    {
        var bytes = FontBytes();
        var first = RegisterAndResolve(bytes, new MemoryStream(bytes));
        var second = RegisterAndResolve(bytes, new MemoryStream(bytes));

        Assert.Same(first, second);
    }

    [Fact]
    public void MutatedArrayIsNotServedStale()
    {
        var bytes = FontBytes();
        var first = RegisterAndResolve(bytes, Exposed(bytes));

        bytes[^1] ^= 0xFF;
        var fonts = new FontCollection();
        fonts.Register(Family, new MemoryStream(bytes, 0, bytes.Length, writable: true, publiclyVisible: true));
        var second = fonts.ResolvePrimarySfnt(new Font { Family = Family });

        Assert.NotSame(first, second);
    }

    [Fact]
    public void CacheDoesNotRootFontAfterSourceArrayIsUnreachable()
    {
        var reference = RegisterAndDrop();

        for (var i = 0; i < 3 && reference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False(reference.IsAlive);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference RegisterAndDrop()
    {
        var bytes = FontBytes();
        var unique = new byte[bytes.Length + 24];
        Array.Copy(bytes, unique, bytes.Length);
        "RZLEAK-PARSECACHE-UNIQUE"u8.CopyTo(unique.AsSpan(bytes.Length));
        return new WeakReference(RegisterAndResolve(unique, Exposed(unique)));
    }
}
