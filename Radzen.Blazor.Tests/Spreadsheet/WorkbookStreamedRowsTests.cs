using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class WorkbookStreamedRowsTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

    private static readonly DateTime Seed = new(2024, 3, 1);

    private static async IAsyncEnumerable<CellData?[]> Rows(params CellData?[][] rows)
    {
        foreach (var row in rows)
        {
            await Task.Yield();

            yield return row;
        }
    }

    private static CellData?[] Order(int i) =>
        [CellData.FromString($"order {i}"), CellData.FromNumber(i * 1.5), CellData.FromBoolean(i % 2 == 0)];

    private static Workbook Framed(int built = 0, int rows = 0)
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", Math.Max(rows, built + 1), 3);

        sheet.Cells[0, 0].SetValue("Name");
        sheet.Cells[0, 1].SetValue("Total");
        sheet.Cells[0, 2].SetValue("Shipped");

        for (var i = 0; i < built; i++)
        {
            sheet.Cells[i + 1, 0].SetValue($"built {i}");
            sheet.Cells[i + 1, 1].Value = (double)i;
            sheet.Cells[i + 1, 2].Value = true;
        }

        return workbook;
    }

    private static async Task<MemoryStream> Save(Workbook workbook, IAsyncEnumerable<CellData?[]> rows, CancellationToken cancellationToken = default, bool useInlineStrings = false)
    {
        var stream = new MemoryStream();

        await workbook.SaveToStreamAsync(stream, rows, useInlineStrings, cancellationToken);

        stream.Position = 0;

        return stream;
    }

    private static Worksheet Read(MemoryStream stream)
    {
        stream.Position = 0;

        return Workbook.LoadFromStream(stream).Sheets[0];
    }

    private static XDocument Part(MemoryStream stream, string name)
    {
        stream.Position = 0;

        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
        using var content = zip.GetEntry(name)!.Open();

        return XDocument.Load(content);
    }

    private static XElement Row(MemoryStream stream, int number) =>
        Part(stream, "xl/worksheets/sheet1.xml").Descendants(Main + "row")
            .Single(r => (string?)r.Attribute("r") == number.ToString(System.Globalization.CultureInfo.InvariantCulture));

    [Fact]
    public async Task Appends_below_a_built_header()
    {
        var sheet = Read(await Save(Framed(), Rows(Order(0), Order(1), Order(2))));

        Assert.Equal("Name", sheet.Cells[0, 0].Value);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal($"order {i}", sheet.Cells[i + 1, 0].Value);
            Assert.Equal(i * 1.5, sheet.Cells[i + 1, 1].Value);
            Assert.Equal(i % 2 == 0, sheet.Cells[i + 1, 2].Value);
        }
    }

    [Fact]
    public async Task Appends_directly_below_the_last_built_row()
    {
        var sheet = Read(await Save(Framed(built: 2), Rows(Order(7))));

        Assert.Equal("built 1", sheet.Cells[2, 0].Value);
        Assert.Equal("order 7", sheet.Cells[3, 0].Value);
    }

    [Fact]
    public async Task Appends_below_the_content_not_the_rows_the_sheet_was_sized_for()
    {
        var sheet = Read(await Save(Framed(rows: 50), Rows(Order(0))));

        Assert.Equal("order 0", sheet.Cells[1, 0].Value);
    }

    [Fact]
    public async Task Keeps_text_that_looks_like_a_number()
    {
        var cell = Read(await Save(Framed(), Rows([CellData.FromString("00123"), null, null]))).Cells[1, 0];

        Assert.Equal("00123", cell.Value);
        Assert.Equal(CellDataType.String, cell.ValueType);
    }

    [Fact]
    public async Task Styles_no_ordinary_text()
    {
        var cell = Row(await Save(Framed(), Rows([CellData.FromString("Alice"), null, null])), 2).Elements(Main + "c").Single();

        Assert.Equal("s", (string?)cell.Attribute("t"));
        Assert.Null(cell.Attribute("s"));
    }

    [Fact]
    public async Task Writes_every_date_in_a_column_with_one_date_style()
    {
        var stream = await Save(Framed(), Rows(
            [null, CellData.FromDate(Seed), null],
            [null, CellData.FromDate(Seed.AddDays(1)), null]));

        var styles = Enumerable.Range(2, 2)
            .Select(r => (string?)Row(stream, r).Elements(Main + "c").Single().Attribute("s"))
            .ToArray();

        Assert.NotNull(styles[0]);
        Assert.Equal(styles[0], styles[1]);

        var sheet = Read(stream);

        Assert.Equal(Seed.ToOADate(), sheet.Cells[1, 1].Value);
        Assert.Equal(Seed.AddDays(1).ToOADate(), sheet.Cells[2, 1].Value);
        Assert.False(string.IsNullOrEmpty(sheet.Cells[1, 1].Format.NumberFormat));
    }

    [Fact]
    public async Task Writes_no_cell_for_a_null()
    {
        var row = Row(await Save(Framed(), Rows([CellData.FromString("a"), null, CellData.FromBoolean(true)])), 2);

        Assert.Equal(new[] { "A2", "C2" }, row.Elements(Main + "c").Select(c => (string?)c.Attribute("r")));
        Assert.Equal("1:3", (string?)row.Attribute("spans"));
    }

    [Fact]
    public async Task Widens_a_table_that_ends_at_the_bottom_of_the_frame()
    {
        var workbook = Framed(built: 1);
        var range = RangeRef.Parse("A1:C2");

        workbook.Sheets[0].AddTable("Orders", range);

        var sheet = Read(await Save(workbook, Rows(Order(0), Order(1), Order(2))));

        Assert.Equal(RangeRef.Parse("A1:C5"), sheet.Tables.Single().Range);
        Assert.Equal(range, workbook.Sheets[0].Tables.Single().Range);
    }

    [Fact]
    public async Task Leaves_a_table_that_ends_above_the_frame()
    {
        var workbook = Framed(built: 2);

        workbook.Sheets[0].AddTable("Orders", RangeRef.Parse("A1:C2"));

        var sheet = Read(await Save(workbook, Rows(Order(0))));

        Assert.Equal(RangeRef.Parse("A1:C2"), sheet.Tables.Single().Range);
    }

    [Fact]
    public async Task Leaves_a_table_with_a_totals_row()
    {
        var workbook = Framed(built: 2);

        workbook.Sheets[0].AddTable("Orders", RangeRef.Parse("A1:C3"), totalsRowShown: true);

        var sheet = Read(await Save(workbook, Rows(Order(0))));

        Assert.Equal(RangeRef.Parse("A1:C3"), sheet.Tables.Single().Range);
    }

    [Fact]
    public async Task Leaves_out_the_dimension_only_when_rows_are_streamed()
    {
        var workbook = Framed(built: 1);

        var streamed = await Save(workbook, Rows(Order(0)));

        var built = new MemoryStream();
        workbook.SaveToStream(built);

        Assert.Empty(Part(streamed, "xl/worksheets/sheet1.xml").Descendants(Main + "dimension"));
        Assert.Single(Part(built, "xl/worksheets/sheet1.xml").Descendants(Main + "dimension"));
    }

    [Fact]
    public async Task Refuses_a_workbook_of_more_than_one_sheet()
    {
        var workbook = Framed();
        workbook.AddSheet("Sheet2", 1, 1);

        var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() => workbook.SaveToStreamAsync(stream, Rows(Order(0))));

        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task A_source_that_faults_leaves_nothing_that_opens()
    {
        var stream = new MemoryStream();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Framed().SaveToStreamAsync(stream, Faulting()));

        Assert.Equal("the source failed", error.Message);
        Assert.Equal(0, stream.Length);

        static async IAsyncEnumerable<CellData?[]> Faulting()
        {
            await Task.Yield();

            yield return Order(0);

            throw new InvalidOperationException("the source failed");
        }
    }

    [Fact]
    public async Task A_cancelled_save_leaves_nothing_that_opens()
    {
        using var cancellation = new CancellationTokenSource();
        var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Framed().SaveToStreamAsync(stream, Cancelling(cancellation), cancellation.Token));

        Assert.Equal(0, stream.Length);

        static async IAsyncEnumerable<CellData?[]> Cancelling(CancellationTokenSource cancellation)
        {
            await Task.Yield();

            yield return Order(0);

            cancellation.Cancel();

            yield return Order(1);
        }
    }

    [Fact]
    public async Task Refuses_a_string_longer_than_a_cell_holds()
    {
        var stream = new MemoryStream();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Framed().SaveToStreamAsync(stream, Rows(Order(0), [null, null, CellData.FromString(new string('x', 32_768))])));

        Assert.Contains("C3", error.Message, StringComparison.Ordinal);
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task Accepts_a_string_as_long_as_a_cell_holds()
    {
        var text = new string('x', 32_767);

        var sheet = Read(await Save(Framed(), Rows([CellData.FromString(text), null, null])));

        Assert.Equal(text, sheet.Cells[1, 0].Value);
    }

    [Fact]
    public async Task Refuses_a_row_past_the_last_a_sheet_holds()
    {
        var workbook = Framed(rows: 1_048_576);
        workbook.Sheets[0].Cells[1_048_575, 0].SetValue("last");

        var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() => workbook.SaveToStreamAsync(stream, Rows(Order(0))));

        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task Refuses_a_row_wider_than_a_sheet()
    {
        var wide = new CellData?[16_385];
        wide[16_384] = CellData.FromString("past the last column");

        var stream = new MemoryStream();

        await Assert.ThrowsAsync<InvalidOperationException>(() => Framed().SaveToStreamAsync(stream, Rows(wide)));

        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public async Task Stops_reading_a_source_that_ignores_the_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var read = 0;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Framed().SaveToStreamAsync(new MemoryStream(), Deaf(cancellation, () => read++), cancellation.Token));

        Assert.Equal(2, read);

        static async IAsyncEnumerable<CellData?[]> Deaf(CancellationTokenSource cancellation, Action counted)
        {
            for (var i = 0; i < 100; i++)
            {
                await Task.Yield();

                counted();

                if (i == 1)
                {
                    cancellation.Cancel();
                }

                yield return Order(i);
            }
        }
    }

    [Fact]
    public async Task A_source_that_ends_when_cancelled_is_not_saved_as_complete()
    {
        using var cancellation = new CancellationTokenSource();
        var stream = new MemoryStream();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Framed().SaveToStreamAsync(stream, Obedient(cancellation), cancellation.Token));

        Assert.Equal(0, stream.Length);

        static async IAsyncEnumerable<CellData?[]> Obedient(CancellationTokenSource cancellation,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();

            yield return Order(0);

            cancellation.Cancel();

            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            yield return Order(1);
        }
    }

    [Fact]
    public async Task Starts_at_the_first_row_of_an_empty_sheet()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 1, 3);

        var sheet = Read(await Save(workbook, Rows(Order(4))));

        Assert.Equal("order 4", sheet.Cells[0, 0].Value);
    }

    [Fact]
    public async Task A_source_of_no_rows_writes_the_frame_alone()
    {
        var workbook = Framed(built: 1);
        workbook.Sheets[0].AddTable("Orders", RangeRef.Parse("A1:C2"));

        var stream = await Save(workbook, Rows());
        var sheet = Read(stream);

        Assert.Equal("built 0", sheet.Cells[1, 0].Value);
        Assert.Equal(RangeRef.Parse("A1:C2"), sheet.Tables.Single().Range);
        Assert.Empty(Part(stream, "xl/worksheets/sheet1.xml").Descendants(Main + "dimension"));
    }

    [Fact]
    public async Task Writes_streamed_text_in_its_cell_and_leaves_the_shared_table_to_the_frame()
    {
        var rows = Enumerable.Range(0, 2_000)
            .Select(i => new CellData?[] { CellData.FromString($"unique {i}"), null, CellData.FromString($"note {i}") })
            .ToArray();

        var stream = await Save(Framed(), Rows(rows), useInlineStrings: true);

        var shared = Part(stream, "xl/sharedStrings.xml").Descendants(Main + "si").Select(si => si.Value).ToArray();

        Assert.Equal(new[] { "Name", "Total", "Shipped" }, shared);

        var types = Part(stream, "xl/worksheets/sheet1.xml").Descendants(Main + "row").Skip(1)
            .SelectMany(r => r.Elements(Main + "c")).Select(c => (string?)c.Attribute("t")).Distinct().ToArray();

        Assert.Equal(new[] { "inlineStr" }, types);

        var sheet = Read(stream);

        Assert.Equal("unique 1999", sheet.Cells[2_000, 0].Value);
        Assert.Equal("note 0", sheet.Cells[1, 2].Value);
    }

    [Fact]
    public async Task Inline_text_that_looks_like_a_number_stays_text()
    {
        var stream = await Save(Framed(), Rows([CellData.FromString("00123"), null, null]), useInlineStrings: true);

        var cell = Row(stream, 2).Elements(Main + "c").Single();

        Assert.Equal("inlineStr", (string?)cell.Attribute("t"));
        Assert.Equal("00123", Read(stream).Cells[1, 0].Value);
    }

    [Fact]
    public async Task Keeps_streamed_text_of_only_spaces()
    {
        var stream = await Save(Framed(), Rows([CellData.FromString("   "), null, null]));

        Assert.Equal("   ", Read(stream).Cells[1, 0].Value);
    }

    [Fact]
    public async Task Writes_streamed_empty_text_as_an_empty_string()
    {
        var stream = await Save(Framed(), Rows([CellData.FromString(""), CellData.FromString("after"), null]));

        var cell = Row(stream, 2).Elements(Main + "c").First();

        Assert.Equal("s", (string?)cell.Attribute("t"));
        Assert.Equal("", Read(stream).Cells[1, 0].Value);
        Assert.Equal("after", Read(stream).Cells[1, 1].Value);
    }

    [Fact]
    public async Task Asks_a_reader_to_keep_the_spaces_only_around_text_that_has_them()
    {
        var stream = await Save(Framed(), Rows([CellData.FromString(" leading"), CellData.FromString("trailing\n"), CellData.FromString("plain"), CellData.FromString("\u00A0no-break")]), useInlineStrings: true);

        var spaces = Row(stream, 2).Descendants(Main + "t").Select(t => (string?)t.Attribute(XNamespace.Xml + "space")).ToArray();

        Assert.Equal(new[] { "preserve", "preserve", null, null }, spaces);
    }

    [Fact]
    public async Task Default_save_shares_text_between_built_and_streamed_cells()
    {
        using var stream = new MemoryStream();
        await Framed().SaveToStreamAsync(stream, Rows(
            [CellData.FromString("Name"), CellData.FromString("repeated")],
            [CellData.FromString("repeated"), CellData.FromString("Name")]));

        var shared = Part(stream, "xl/sharedStrings.xml").Descendants(Main + "si").Select(si => si.Value).ToArray();
        Assert.Equal(new[] { "Name", "Total", "Shipped", "repeated" }, shared);
        var cells = Part(stream, "xl/worksheets/sheet1.xml").Descendants(Main + "c").ToArray();
        Assert.All(cells, cell => Assert.Equal("s", (string?)cell.Attribute("t")));
        Assert.Equal(cells[0].Element(Main + "v")!.Value, cells[3].Element(Main + "v")!.Value);
        Assert.Equal("repeated", Read(stream).Cells[2, 0].Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Text_storage_modes_preserve_empty_whitespace_and_quoted_text(bool useInlineStrings)
    {
        using var stream = await Save(Framed(), Rows(
            [CellData.FromString(""), CellData.FromString("  "), CellData.FromString("00123")],
            [CellData.FromString(" leading"), CellData.FromString("trailing\n"), CellData.FromString("=SUM(A1:A2)")]),
            useInlineStrings: useInlineStrings);
        var sheet = Read(stream);
        Assert.Equal("", sheet.Cells[1, 0].Value);
        Assert.Equal("  ", sheet.Cells[1, 1].Value);
        Assert.Equal("00123", sheet.Cells[1, 2].Value);
        Assert.Equal(" leading", sheet.Cells[2, 0].Value);
        Assert.Equal("trailing\n", sheet.Cells[2, 1].Value);
        Assert.Equal("=SUM(A1:A2)", sheet.Cells[2, 2].Value);
        Assert.Null(sheet.Cells[2, 2].Formula);
    }

    [Fact]
    public void Synchronous_save_finishes_multiple_sheets_and_their_tables()
    {
        var workbook = Framed(built: 1);
        workbook.Sheets[0].AddTable("First", RangeRef.Parse("A1:C2"));
        var second = workbook.AddSheet("Second", 2, 1);
        second.Cells[0, 0].SetValue("Name");
        second.Cells[1, 0].SetValue("other");
        second.AddTable("SecondTable", RangeRef.Parse("A1:A2"));
        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        var read = Workbook.LoadFromStream(stream);
        Assert.Equal(2, read.Sheets.Count);
        Assert.Equal("built 0", read.Sheets[0].Cells[1, 0].Value);
        Assert.Equal("other", read.Sheets[1].Cells[1, 0].Value);
        Assert.Equal(RangeRef.Parse("A1:A2"), read.Sheets[1].Tables.Single().Range);
        Assert.Single(Part(stream, "xl/worksheets/sheet2.xml").Descendants(Main + "dimension"));
    }

    [Fact]
    public async Task Awaiting_the_source_keeps_the_session_open_and_disposes_the_enumerator()
    {
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = false;
        using var stream = new MemoryStream();
        var save = Framed().SaveToStreamAsync(stream, Source());
        await waiting.Task;
        Assert.False(save.IsCompleted);
        resume.SetResult();
        await save;
        Assert.True(disposed);
        Assert.True(stream.CanWrite);
        Assert.Equal("order 1", Read(stream).Cells[2, 0].Value);

        async IAsyncEnumerable<CellData?[]> Source()
        {
            try
            {
                yield return Order(0);
                waiting.SetResult();
                await resume.Task;
                yield return Order(1);
            }
            finally
            {
                disposed = true;
            }
        }
    }

    [Fact]
    public async Task Failure_restores_the_destination_prefix_and_disposes_the_source()
    {
        using var stream = new MemoryStream();
        stream.Write(new byte[] { 1, 2, 3 });
        var disposed = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Framed().SaveToStreamAsync(stream, Source()));
        Assert.Equal(new byte[] { 1, 2, 3 }, stream.ToArray());
        Assert.Equal(3, stream.Position);
        Assert.True(disposed);
        Assert.True(stream.CanWrite);

        async IAsyncEnumerable<CellData?[]> Source()
        {
            try
            {
                await Task.Yield();
                yield return Order(0);
                throw new InvalidOperationException("source");
            }
            finally
            {
                disposed = true;
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Expected_truncation_failures_preserve_the_source_exception(bool unsupported)
    {
        using var stream = new TruncationFailureStream(unsupported);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Framed().SaveToStreamAsync(stream, Source()));
        Assert.Equal("source", error.Message);
        Assert.True(stream.TruncationAttempted);

        static async IAsyncEnumerable<CellData?[]> Source()
        {
            await Task.Yield();
            yield return Order(0);
            throw new InvalidOperationException("source");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Writes_to_a_non_seekable_destination(bool useInlineStrings)
    {
        using var destination = new NonSeekableStream();
        await Framed().SaveToStreamAsync(destination, Rows(Order(0)), useInlineStrings);
        Assert.True(destination.CanWrite);
        using var copy = new MemoryStream(destination.ToArray());
        Assert.Equal("order 0", Read(copy).Cells[1, 0].Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Output_failure_during_begin_or_end_discards_partial_output(bool failAtBegin)
    {
        using var stream = new FailingWriteStream { FailWrites = failAtBegin };
        var enumerated = false;
        var workbook = Framed(built: failAtBegin ? 2_000 : 0);
        var error = await Assert.ThrowsAsync<IOException>(() => workbook.SaveToStreamAsync(stream, Source()));
        Assert.Same(stream.Error, error);
        Assert.Equal(!failAtBegin, enumerated);
        Assert.Equal(0, stream.Length);
        Assert.True(stream.CanWrite);

        async IAsyncEnumerable<CellData?[]> Source()
        {
            await Task.Yield();
            enumerated = true;
            yield return Order(0);
            stream.FailWrites = true;
        }
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
    }

    private sealed class FailingWriteStream : MemoryStream
    {
        public bool FailWrites { get; set; }
        public IOException Error { get; } = new("output failed");

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (FailWrites)
            {
                throw Error;
            }

            base.Write(buffer, offset, count);
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (FailWrites)
            {
                throw Error;
            }

            base.Write(buffer);
        }
    }

    private sealed class TruncationFailureStream(bool unsupported) : MemoryStream
    {
        public bool TruncationAttempted { get; private set; }

        public override void SetLength(long value)
        {
            TruncationAttempted = true;
            if (unsupported)
            {
                throw new NotSupportedException();
            }

            throw new IOException();
        }
    }

}
