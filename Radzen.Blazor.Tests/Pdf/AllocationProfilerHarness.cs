#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Xunit.Abstractions;

namespace Radzen.Blazor.Pdf.Tests;

// Manual-run allocation/time profiler for the realistic invoice scenario. Not part of the
// regular suite: it no-ops unless RADZEN_PDF_PROFILE=1 and RADZEN_PDF_PROFILE_FONT points
// at a large Unicode TrueType font (e.g. Arial Unicode MS). Run with:
//
//   RADZEN_PDF_PROFILE=1 \
//   RADZEN_PDF_PROFILE_FONT=/path/to/ArialUnicode.ttf \
//   dotnet test Radzen.Blazor.Tests --filter FullyQualifiedName~AllocationProfilerHarness \
//     --logger "console;verbosity=detailed"
public class AllocationProfilerHarness(ITestOutputHelper output)
{
    private const string Family = "Arial Unicode MS";

    private static readonly string[] Items =
    [
        "Radzen Blazor Studio - Enterprise subscription",
        "Radzen Blazor Studio - Professional subscription",
        "Поддръжка и консултации (годишен план)",
        "Custom component development",
        "Приоритетна поддръжка - 12 месеца",
        "Radzen Blazor MCP - team seats",
        "Onboarding and training sessions",
        "Лиценз за екип от 5 разработчици",
        "Extended support renewal",
        "Данъчна основа - услуги",
        "Source code license upgrade",
        "Консултация по архитектура",
        "CI/CD integration assistance",
        "Миграция от WebForms към Blazor",
        "Annual maintenance and updates",
    ];

    private sealed record Sample(long Bytes, double Ms);

    private static Sample Measure(Action action)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        action();
        clock.Stop();
        return new Sample(GC.GetAllocatedBytesForCurrentThread() - before, clock.Elapsed.TotalMilliseconds);
    }

    private static Sample Median(int iterations, Action action)
    {
        var samples = new List<Sample>(iterations);
        for (var i = 0; i < iterations; i++)
        {
            samples.Add(Measure(action));
        }

        var byBytes = samples.OrderBy(s => s.Bytes).ToList();
        var byMs = samples.OrderBy(s => s.Ms).ToList();
        return new Sample(byBytes[iterations / 2].Bytes, byMs[iterations / 2].Ms);
    }

    private void Report(string phase, Sample sample)
        => output.WriteLine($"{phase,-34} {sample.Bytes / (1024.0 * 1024.0),10:F2} MB {sample.Ms,10:F2} ms");

    private static DocumentBuilder BuildInvoice(byte[] fontBytes)
    {
        var builder = new DocumentBuilder();
        // Exposed buffer lets the weak-keyed parse cache key on the array's identity.
        builder.Fonts.Register(Family, new MemoryStream(fontBytes, 0, fontBytes.Length, writable: false, publiclyVisible: true));

        var section = builder.Sections.Add();
        section.PageSize = PageSizes.A4;
        section.Margin = Unit.FromPoint(48);

        AddParagraph(section.Blocks, "INVOICE / ФАКТУРА No 2026-0042", 22, bold: true);
        AddParagraph(section.Blocks, "Radzen Ltd, 1000 Sofia, Bulgaria, ул. Шипченски проход 63", 10);
        AddParagraph(section.Blocks, "Bill to: Contoso GmbH, Hauptstrasse 12, Berlin / Получател: Контосо ГмбХ", 10);
        AddParagraph(section.Blocks, "Date: 2026-07-10   Due: 2026-08-09   VAT: BG204123456", 10);

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(250));
        table.Columns.Add(Unit.FromPoint(60));
        table.Columns.Add(Unit.FromPoint(90));
        table.Columns.Add(Unit.FromPoint(90));

        var header = table.Rows.Add();
        header.IsHeader = true;
        FillCell(header.Cells[0], "Description / Описание", bold: true);
        FillCell(header.Cells[1], "Qty", bold: true);
        FillCell(header.Cells[2], "Unit price", bold: true);
        FillCell(header.Cells[3], "Amount / Сума", bold: true);

        for (var i = 0; i < Items.Length; i++)
        {
            var row = table.Rows.Add();
            FillCell(row.Cells[0], Items[i]);
            FillCell(row.Cells[1], (1 + i % 3).ToString());
            FillCell(row.Cells[2], $"{99.90 + i * 10:F2} EUR");
            FillCell(row.Cells[3], $"{(1 + i % 3) * (99.90 + i * 10):F2} EUR");
        }

        AddParagraph(section.Blocks, "Subtotal: 4,318.50 EUR   VAT 20%: 863.70 EUR", 11);
        AddParagraph(section.Blocks, "Total due / Обща сума: 5,182.20 EUR", 13, bold: true);
        AddParagraph(section.Blocks, "Payment terms: net 30. Bank: UniCredit Bulbank, IBAN BG18UNCR70001512345678.", 9);

        var footer = new Paragraph();
        var footerRun = footer.Inlines.Add("Radzen Ltd - www.radzen.com - Благодарим Ви за доверието!");
        footerRun.Font.Name = Family;
        footerRun.Font.Size = 8;
        section.Footer.Blocks.Add(footer);

        return builder;
    }

    private static void AddParagraph(BlockCollection blocks, string text, double size, bool bold = false)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = Family;
        run.Font.Size = size;
        run.Font.Bold = bold;
        paragraph.SpacingAfter = Unit.FromPoint(6);
        blocks.Add(paragraph);
    }

    private static void FillCell(Cell cell, string text, bool bold = false)
    {
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add(text);
        run.Font.Name = Family;
        run.Font.Size = 10;
        run.Font.Bold = bold;
        cell.Blocks.Add(paragraph);
        cell.Padding = Unit.FromPoint(3);
    }

    private static IEnumerable<string> AllInvoiceText()
    {
        yield return "INVOICE / ФАКТУРА No 2026-0042";
        yield return "Radzen Ltd, 1000 Sofia, Bulgaria, ул. Шипченски проход 63";
        yield return "Bill to: Contoso GmbH, Hauptstrasse 12, Berlin / Получател: Контосо ГмбХ";
        yield return "Date: 2026-07-10   Due: 2026-08-09   VAT: BG204123456";
        yield return "Description / Описание Qty Unit price Amount / Сума";
        foreach (var item in Items)
        {
            yield return item;
        }

        yield return "Subtotal: 4,318.50 EUR   VAT 20%: 863.70 EUR 5,182.20 0123456789.,EUR";
        yield return "Payment terms: net 30. Bank: UniCredit Bulbank, IBAN BG18UNCR70001512345678.";
        yield return "Radzen Ltd - www.radzen.com - Благодарим Ви за доверието!";
    }

    private static SortedSet<ushort> UsedGlyphs(SfntFont font)
    {
        var gids = new SortedSet<ushort> { 0 };
        foreach (var text in AllInvoiceText())
        {
            foreach (var ch in text)
            {
                gids.Add(font.GetGlyphId(ch));
            }
        }

        return gids;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Font, WeakReference Bytes) BuildAndRelease(string fontPath)
    {
        var bytes = File.ReadAllBytes(fontPath);
        var builder = BuildInvoice(bytes);
        using var stream = new MemoryStream();
        builder.SaveToStream(stream);
        var font = builder.Fonts.ResolvePrimarySfnt(new Font { Name = Family });
        return (new WeakReference(font), new WeakReference(bytes));
    }

    [Fact]
    [Trait("Category", "Profiler")]
    public void ProfileInvoiceAllocations()
    {
        if (Environment.GetEnvironmentVariable("RADZEN_PDF_PROFILE") != "1")
        {
            return;
        }

        var fontPath = Environment.GetEnvironmentVariable("RADZEN_PDF_PROFILE_FONT")
            ?? throw new InvalidOperationException("Set RADZEN_PDF_PROFILE_FONT to a TrueType font path.");
        var fontBytes = File.ReadAllBytes(fontPath);
        var iterations = int.TryParse(
            Environment.GetEnvironmentVariable("RADZEN_PDF_PROFILE_ITERATIONS"), out var n) ? n : 15;

        output.WriteLine($"Font: {fontPath} ({fontBytes.Length / (1024.0 * 1024.0):F1} MB), iterations={iterations}");
        output.WriteLine($"{"phase",-34} {"alloc",13} {"time",13}");

        // COLD: first full document on this thread, includes first font parse and all JIT.
        var cold = Measure(() =>
        {
            using var stream = new MemoryStream();
            BuildInvoice(fontBytes).SaveToStream(stream);
        });
        Report("full pipeline COLD", cold);

        // WARM full pipeline: register (parse) + build + save per document, as a server would
        // when it does not cache the FontCollection between requests.
        long pdfSize = 0;
        var warmFull = Median(iterations, () =>
        {
            using var stream = new MemoryStream();
            BuildInvoice(fontBytes).SaveToStream(stream);
            pdfSize = stream.Length;
        });
        Report("full pipeline WARM (median)", warmFull);
        output.WriteLine($"PDF size: {pdfSize / 1024.0:F1} KB");

        // Phase: font parse alone.
        Report("SfntFont.Parse", Median(iterations, () => SfntFont.Parse(fontBytes)));

        var font = SfntFont.Parse(fontBytes);
        var gids = UsedGlyphs(font);
        output.WriteLine($"glyphs used: {gids.Count} of {font.GlyphCount}");

        // Phase: cmap lookups for the whole invoice text, x100 to be measurable.
        Report("cmap GetGlyphId x100 docs", Median(iterations, () =>
        {
            for (var r = 0; r < 100; r++)
            {
                foreach (var text in AllInvoiceText())
                {
                    foreach (var ch in text)
                    {
                        font.GetGlyphId(ch);
                    }
                }
            }
        }));

        // Phase: glyf subset alone.
        Report("GlyfSubsetter.Subset", Median(iterations, () => GlyfSubsetter.Subset(font, gids)));

        // Phase: full Type0 embed (subset + CIDSet + ToUnicode + flate) into a throwaway writer.
        var gidToUnicode = new Dictionary<ushort, int>();
        foreach (var text in AllInvoiceText())
        {
            foreach (var ch in text)
            {
                gidToUnicode[font.GetGlyphId(ch)] = ch;
            }
        }

        Report("Type0FontEmbedder.Embed", Median(iterations, () =>
        {
            using var sink = new MemoryStream();
            var writer = new DocumentWriter(sink);
            Type0FontEmbedder.Embed(writer, font, gidToUnicode);
        }));

        // Phase: registration amortized - one builder, repeated Build() (layout + embed only).
        var prebuilt = BuildInvoice(fontBytes);
        Report("Build() (font pre-registered)", Median(iterations, () => prebuilt.Build()));

        // Phase: serialization of an already built Document.
        var document = prebuilt.Build();
        Report("SaveToStream (prebuilt doc)", Median(iterations, () =>
        {
            using var stream = new MemoryStream();
            document.SaveToStream(stream);
        }));

        // Comparison: fresh array identity per build defeats the parse cache and shows
        // the uncached (pre-cache) cost.
        Report("full WARM (fresh array, no cache)", Median(iterations, () =>
        {
            using var stream = new MemoryStream();
            BuildInvoice((byte[])fontBytes.Clone()).SaveToStream(stream);
        }));

        // Leak check: once the source array is unreachable the cache entry must be
        // collectible - the parsed font must not be rooted by the static cache.
        var (fontRef, bytesRef) = BuildAndRelease(fontPath);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        output.WriteLine($"leak check: font alive={fontRef.IsAlive}, bytes alive={bytesRef.IsAlive}");
        Assert.False(fontRef.IsAlive);
        Assert.False(bytesRef.IsAlive);

        // Baseline: same invoice with a small (~140 KB) Latin font shows how much of the
        // pipeline cost scales with font size. Cyrillic falls back to notdef there; layout
        // shape stays comparable.
        var smallFont = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        Report("full WARM w/ small font", Median(iterations, () =>
        {
            using var stream = new MemoryStream();
            var builder = BuildInvoice(smallFont);
            builder.SaveToStream(stream);
        }));
    }
}
