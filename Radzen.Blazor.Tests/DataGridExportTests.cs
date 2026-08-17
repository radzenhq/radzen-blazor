using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Documents;
using Radzen.Documents.Core;
using Radzen.Documents.Pdf;
using Radzen.Documents.Spreadsheet;
using Xunit;
using DocumentTable = Radzen.Documents.Table;

namespace Radzen.Blazor.Tests;

public class DataGridExportTests
{
    [Fact]
    public async Task ToWorkbookAsync_ExportsVisibleBoundColumnsWithTypedValuesAndCulture()
    {
        using var context = CreateContext();
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var rows = new[]
        {
            new ExportRow(1, "Alpha", 1234.5m, new DateTime(2024, 8, 17), "X1"),
            new ExportRow(2, "Beta", 9.25m, new DateTime(2024, 8, 18), "X2")
        };
        var component = RenderGrid(context, rows, culture: culture);

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions { Title = "Orders" }));
        var sheet = workbook.Sheets.Single();

        Assert.Same(culture, workbook.Culture);
        Assert.Equal("Orders", sheet.Name);
        Assert.Equal(5, sheet.ColumnCount);
        Assert.Equal("Identifier", sheet.Cells[0, 0].Value);
        Assert.True(sheet.Cells[0, 0].Format.Bold);
        Assert.Equal(CellDataType.Number, sheet.Cells[1, 0].Data.Type);
        Assert.Equal(1234.5d, sheet.Cells[1, 2].Value);
        Assert.Equal("1.234,50", sheet.Cells[1, 2].GetDisplayText());
        Assert.Equal(CellDataType.Date, sheet.Cells[1, 3].Data.Type);
        Assert.Equal("Code: X1", sheet.Cells[1, 4].Value);
        Assert.DoesNotContain("Hidden", Enumerable.Range(0, sheet.ColumnCount).Select(index => sheet.Cells[0, index].Value));
    }

    [Fact]
    public async Task ToWorkbookAsync_RespectsCurrentPageAndAllScopes()
    {
        using var context = CreateContext();
        var rows = Enumerable.Range(1, 5).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), index.ToString(CultureInfo.InvariantCulture))).ToArray();
        var component = RenderGrid(context, rows, allowPaging: true, pageSize: 2);
        await component.InvokeAsync(() => component.Instance.GoToPage(1));

        var current = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync());
        var all = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions { Scope = DataGridExportScope.All }));

        Assert.Equal(3, current.Sheets[0].RowCount);
        Assert.Equal(6, all.Sheets[0].RowCount);
        Assert.Equal(3d, current.Sheets[0].Cells[1, 0].Value);
    }

    [Fact]
    public async Task ToPdfDocumentAsync_CreatesConfiguredTableAndFormattedCells()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(1, "Alpha", 12.5m, new DateTime(2024, 8, 17), "X1") };
        var component = RenderGrid(context, rows, culture: CultureInfo.InvariantCulture);

        var document = await component.InvokeAsync(() => component.Instance.ToPdfDocumentAsync(new DataGridPdfExportOptions
        {
            Title = "Orders",
            PageSize = PageSizes.Letter,
            Orientation = PageOrientation.Portrait,
            RepeatHeader = false
        }));
        var section = document.Sections.Single();
        var table = section.Blocks.OfType<DocumentTable>().Single();

        Assert.Equal("Orders", document.Info.Title);
        Assert.Equal(PageSizes.Letter, section.PageSize);
        Assert.Equal(PageOrientation.Portrait, section.Orientation);
        Assert.IsType<Paragraph>(section.Blocks[0]);
        Assert.Equal(5, table.Columns.Count);
        Assert.True(table.Rows[0].IsHeaderRow);
        Assert.False(table.Rows[0].RepeatOnEveryPage);
        Assert.Equal("12.50", table.Rows[1].Cells[2].Text);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(document.ToPdf(), 0, 5));
    }

    [Fact]
    public async Task ToPdfDocumentAsync_OmitsTitleWhenNotConfigured()
    {
        using var context = CreateContext();
        var component = RenderGrid(context, new[] { new ExportRow(1, "Alpha", 1m, DateTime.Today, "X") });

        var document = await component.InvokeAsync(() => component.Instance.ToPdfDocumentAsync());

        Assert.Null(document.Info.Title);
        Assert.IsType<DocumentTable>(document.Sections[0].Blocks[0]);
    }

    [Fact]
    public async Task ToWorkbookAsync_LoadDataAllUsesChunksReportsProgressAndRestoresPage()
    {
        using var context = CreateContext();
        var source = Enumerable.Range(1, 6).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), $"X{index}")).ToArray();
        var calls = new List<(int Skip, int Top)>();
        IRenderedComponent<RadzenDataGrid<ExportRow>> component = null;
        var initial = source.Take(3).ToArray();
        component = RenderGrid(context, initial, allowPaging: true, pageSize: 3, count: source.Length, loadData: args =>
        {
            var currentSkip = args.Skip ?? 0;
            var currentTop = args.Top ?? 3;
            calls.Add((currentSkip, currentTop));
            if (component != null)
            {
                component.Instance.Count = source.Length;
                component.Instance.Data = source.Skip(currentSkip).Take(currentTop).ToArray();
            }
        });
        await component.InvokeAsync(() => component.Instance.GoToPage(1));
        calls.Clear();
        var progress = new List<int>();

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions
        {
            Scope = DataGridExportScope.All,
            ChunkSize = 2,
            Progress = value => progress.Add(value.Loaded)
        }));

        Assert.Equal(7, workbook.Sheets[0].RowCount);
        Assert.Equal(new[] { 2, 4, 6 }, progress);
        Assert.Equal(new[] { (0, 2), (2, 2), (4, 2), (3, 3) }, calls);
        Assert.Equal(new[] { 4, 5, 6 }, component.Instance.Data.Select(row => row.Id));
    }

    [Fact]
    public async Task ToWorkbookAsync_CancellationStillRestoresLoadDataPage()
    {
        using var context = CreateContext();
        var source = Enumerable.Range(1, 6).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), $"X{index}")).ToArray();
        var calls = new List<(int Skip, int Top)>();
        IRenderedComponent<RadzenDataGrid<ExportRow>> component = null;
        component = RenderGrid(context, source.Take(3).ToArray(), allowPaging: true, pageSize: 3, count: source.Length, loadData: args =>
        {
            var currentSkip = args.Skip ?? 0;
            var currentTop = args.Top ?? 3;
            calls.Add((currentSkip, currentTop));
            if (component != null)
            {
                component.Instance.Data = source.Skip(currentSkip).Take(currentTop).ToArray();
            }
        });
        await component.InvokeAsync(() => component.Instance.GoToPage(1));
        calls.Clear();
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAsync<OperationCanceledException>(() => component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions
        {
            Scope = DataGridExportScope.All,
            ChunkSize = 2,
            CancellationToken = cancellation.Token,
            Progress = value => cancellation.Cancel()
        })));

        Assert.Equal(new[] { (0, 2), (3, 3) }, calls);
        Assert.Equal(new[] { 4, 5, 6 }, component.Instance.Data.Select(row => row.Id));
    }

    [Fact]
    public async Task CsvWorkbook_PreservesFormattedStringsSeparatorAndQuoting()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(1, "A;B\nC", 12.5m, new DateTime(2024, 8, 17), "plain") };
        var component = RenderGrid(context, rows, columns: builder =>
        {
            AddColumn(builder, 0, "Name", "Name");
            AddColumn(builder, 3, "Code", "Note");
        });
        var options = new DataGridCsvExportOptions
        {
            CsvOptions = new CsvExportOptions
            {
                Separator = ';',
                Encoding = new UTF8Encoding(false)
            }
        };

        var workbook = await component.InvokeAsync(() => component.Instance.ToCsvWorkbookAsync(options));
        using var stream = new MemoryStream();
        workbook.SaveAsCsv(stream, options.CsvOptions);
        var csv = Encoding.UTF8.GetString(stream.ToArray());

        Assert.Equal("Name;Note\r\n\"A;B\nC\";plain\r\n", csv);
    }

    [Fact]
    public async Task ToWorkbookAsync_ExportsLeafColumnsOfGroupedHeaders()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(1, "Alpha", 2m, new DateTime(2024, 8, 17), "X1") };
        var component = RenderGrid(context, rows, columns: builder =>
        {
            builder.OpenComponent<RadzenDataGridColumn<ExportRow>>(0);
            builder.AddAttribute(1, "Title", "Group");
            builder.AddAttribute(2, "Columns", (RenderFragment)(child =>
            {
                AddColumn(child, 0, "Id", "Identifier");
                AddColumn(child, 3, "Name", "Name");
            }));
            builder.CloseComponent();
            AddColumn(builder, 4, "Amount", "Amount");
        });

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync());
        var sheet = workbook.Sheets[0];

        Assert.Equal(new object[] { "Identifier", "Name", "Amount" }, Enumerable.Range(0, sheet.ColumnCount).Select(index => sheet.Cells[0, index].Value));
        Assert.Equal(1d, sheet.Cells[1, 0].Value);
    }

    [Fact]
    public async Task ToWorkbookAsync_ExportsTemplateColumnsWithExportValue()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(7, "Alpha", 2m, new DateTime(2024, 8, 17), "X1") };
        var component = RenderGrid(context, rows, columns: builder =>
        {
            AddColumn(builder, 0, "Name", "Name");
            builder.OpenComponent<RadzenDataGridColumn<ExportRow>>(3);
            builder.AddAttribute(4, "Title", "Summary");
            builder.AddAttribute(5, "Template", (RenderFragment<ExportRow>)(_ => child => child.AddContent(0, "template")));
            builder.AddAttribute(6, "ExportValue", (Func<ExportRow, object>)(row => $"{row.Id}-{row.Code}"));
            builder.CloseComponent();
        });

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync());
        var sheet = workbook.Sheets[0];

        Assert.Equal("Summary", sheet.Cells[0, 1].Value);
        Assert.Equal("7-X1", sheet.Cells[1, 1].Value);
    }

    [Fact]
    public async Task ToWorkbookAsync_LoadDataAllContinuesWhenHandlerCapsPageSize()
    {
        using var context = CreateContext();
        var source = Enumerable.Range(1, 6).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), $"X{index}")).ToArray();
        var calls = new List<(int Skip, int Top)>();
        IRenderedComponent<RadzenDataGrid<ExportRow>> component = null;
        component = RenderGrid(context, source.Take(3).ToArray(), allowPaging: true, pageSize: 3, count: source.Length, loadData: args =>
        {
            calls.Add((args.Skip ?? 0, args.Top ?? 3));
            component.Instance.Data = source.Skip(args.Skip ?? 0).Take(Math.Min(args.Top ?? 3, 2)).ToArray();
        });
        calls.Clear();

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions
        {
            Scope = DataGridExportScope.All,
            ChunkSize = 4
        }));

        Assert.Equal(7, workbook.Sheets[0].RowCount);
        Assert.Equal(new[] { (0, 4), (2, 4), (4, 4), (0, 3) }, calls);
    }

    [Fact]
    public async Task ToWorkbookAsync_ThrowsWhenExportAlreadyInProgress()
    {
        using var context = CreateContext();
        var source = Enumerable.Range(1, 4).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), $"X{index}")).ToArray();
        IRenderedComponent<RadzenDataGrid<ExportRow>> component = null;
        component = RenderGrid(context, source.Take(2).ToArray(), allowPaging: true, pageSize: 2, count: source.Length, loadData: args =>
        {
            component.Instance.Data = source.Skip(args.Skip ?? 0).Take(args.Top ?? 2).ToArray();
        });
        Task<Workbook> second = null;

        await component.InvokeAsync(() => component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions
        {
            Scope = DataGridExportScope.All,
            ChunkSize = 2,
            Progress = _ => second ??= component.Instance.ToWorkbookAsync(new DataGridExcelExportOptions { Scope = DataGridExportScope.All })
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => second);
    }

    [Fact]
    public async Task CsvWorkbook_EscapesFormulaLikeValues()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(1, "=SUM(A1:B1)", -5m, new DateTime(2024, 8, 17), "@cmd") };
        var component = RenderGrid(context, rows, culture: CultureInfo.InvariantCulture, columns: builder =>
        {
            AddColumn(builder, 0, "Name", "Name");
            AddColumn(builder, 3, "Amount", "Amount");
            AddColumn(builder, 6, "Code", "Code");
        });

        var workbook = await component.InvokeAsync(() => component.Instance.ToCsvWorkbookAsync(new DataGridCsvExportOptions()));
        var sheet = workbook.Sheets[0];

        Assert.Equal("'=SUM(A1:B1)", sheet.Cells[1, 0].Value);
        Assert.Equal("-5", sheet.Cells[1, 1].Value);
        Assert.Equal("'@cmd", sheet.Cells[1, 2].Value);
    }

    [Fact]
    public async Task ToWorkbookAsync_CurrencyFormatQuotesSymbol()
    {
        using var context = CreateContext();
        var rows = new[] { new ExportRow(1, "Alpha", 1234.5m, new DateTime(2024, 8, 17), "X1") };
        var component = RenderGrid(context, rows, culture: CultureInfo.GetCultureInfo("de-DE"), columns: builder =>
        {
            AddColumn(builder, 0, "Amount", "Amount", "{0:C2}");
        });

        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync());
        var format = workbook.Sheets[0].Cells[1, 0].Format.NumberFormat;

        Assert.Contains("#,##0.00", format);
        Assert.Contains("\"€\"", format);
        Assert.Equal(string.Format(CultureInfo.GetCultureInfo("de-DE"), "{0:C2}", 1234.5m), workbook.Sheets[0].Cells[1, 0].GetDisplayText());
    }

    [Fact]
    public async Task Export_AppliesThemeColorsToPdfAndExcel()
    {
        using var context = CreateContext();
        context.JSInterop.Setup<string[]>("Radzen.cssVariables", _ => true).SetResult(new[]
        {
            "rgb(37, 49, 60)",
            "#ffffff",
            "rgba(255, 255, 255, 1)",
            "rgb(16, 24, 32)",
            "rgb(242, 244, 247)",
            "1px solid rgb(200, 205, 210)",
            "none",
            "0.875rem",
            "0.5rem 0.75rem"
        });
        var rows = Enumerable.Range(1, 2).Select(index => new ExportRow(index, $"Row {index}", index, new DateTime(2024, 1, index), $"X{index}")).ToArray();
        var component = RenderGrid(context, rows);

        var document = await component.InvokeAsync(() => component.Instance.ToPdfDocumentAsync());
        var table = document.Sections[0].Blocks.OfType<DocumentTable>().Single();
        var workbook = await component.InvokeAsync(() => component.Instance.ToWorkbookAsync());
        var sheet = workbook.Sheets[0];

        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(37, 49, 60), table.Rows[0].Background);
        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(255, 255, 255), table.Rows[0].Font.Color);
        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(255, 255, 255), table.Rows[1].Background);
        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(242, 244, 247), table.Rows[2].Background);
        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(16, 24, 32), table.Rows[1].Font.Color);
        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(200, 205, 210), table.Rows[1].Cells[0].Borders.Bottom.Color);
        Assert.Equal(default, table.Rows[1].Cells[0].Borders.Left.Width);
        Assert.Equal("#25313C", sheet.Cells[0, 0].Format.BackgroundColor);
        Assert.Equal("#FFFFFF", sheet.Cells[0, 0].Format.Color);
        Assert.Equal("#FFFFFF", sheet.Cells[1, 0].Format.BackgroundColor);
        Assert.Equal("#F2F4F7", sheet.Cells[2, 0].Format.BackgroundColor);
        Assert.Equal("#C8CDD2", sheet.Cells[1, 0].Format.BorderBottom?.Color);
        Assert.Null(sheet.Cells[1, 0].Format.BorderLeft);
        Assert.Equal(Unit.FromPoint(10.5), table.Font.Size);
        Assert.Equal(Unit.FromPoint(6), table.Rows[1].Cells[0].PaddingTop);
        Assert.Equal(Unit.FromPoint(9), table.Rows[1].Cells[0].PaddingLeft);
        Assert.Equal(10.5, sheet.Cells[1, 0].Format.FontSize);
    }

    [Fact]
    public async Task ToPdfDocumentAsync_ExplicitFontSizeOverridesTheme()
    {
        using var context = CreateContext();
        context.JSInterop.Setup<string[]>("Radzen.cssVariables", _ => true).SetResult(new[] { null, null, null, null, null, null, null, "0.875rem", (string)null });
        var component = RenderGrid(context, new[] { new ExportRow(1, "Alpha", 1m, DateTime.Today, "X") });

        var document = await component.InvokeAsync(() => component.Instance.ToPdfDocumentAsync(new DataGridPdfExportOptions { FontSize = 8 }));
        var table = document.Sections[0].Blocks.OfType<DocumentTable>().Single();

        Assert.Equal(Unit.FromPoint(8), table.Font.Size);
    }

    [Fact]
    public async Task Export_KeepsDefaultColorsWhenThemeDisabled()
    {
        using var context = CreateContext();
        context.JSInterop.Setup<string[]>("Radzen.cssVariables", _ => true).SetResult(new[] { "rgb(1, 2, 3)", null, null, null, null, null, null, null, null });
        var component = RenderGrid(context, new[] { new ExportRow(1, "Alpha", 1m, DateTime.Today, "X") });

        var document = await component.InvokeAsync(() => component.Instance.ToPdfDocumentAsync(new DataGridPdfExportOptions { UseTheme = false }));
        var table = document.Sections[0].Blocks.OfType<DocumentTable>().Single();

        Assert.Equal(Radzen.Documents.Core.Color.FromRgb(242, 244, 247), table.Rows[0].Background);
        Assert.Null(table.Rows[1].Background);
    }

    private static TestContext CreateContext()
    {
        var context = new TestContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.JSInterop.SetupModule("_content/Radzen.Blazor/Radzen.Blazor.js");
        return context;
    }

    private static IRenderedComponent<RadzenDataGrid<ExportRow>> RenderGrid(
        TestContext context,
        IEnumerable<ExportRow> rows,
        CultureInfo culture = null,
        bool allowPaging = false,
        int pageSize = 10,
        int? count = null,
        Action<LoadDataArgs> loadData = null,
        RenderFragment columns = null)
    {
        columns ??= builder =>
        {
            AddColumn(builder, 0, "Id", "Identifier");
            AddColumn(builder, 3, "Name", "Name");
            AddColumn(builder, 6, "Amount", "Amount", "{0:N2}");
            AddColumn(builder, 9, "Date", "Date", "{0:d}");
            AddColumn(builder, 12, "Code", "Code", "Code: {0}");
            AddColumn(builder, 15, "Hidden", "Hidden", visible: false);
            builder.OpenComponent<RadzenDataGridColumn<ExportRow>>(18);
            builder.AddAttribute(19, "Title", "Template only");
            builder.AddAttribute(20, "Template", (RenderFragment<ExportRow>)(_ => child => child.AddContent(0, "template")));
            builder.CloseComponent();
        };

        return context.RenderComponent<RadzenDataGrid<ExportRow>>(parameters =>
        {
            parameters.Add(grid => grid.Data, rows);
            parameters.Add(grid => grid.Columns, columns);
            parameters.Add(grid => grid.AllowPaging, allowPaging);
            parameters.Add(grid => grid.PageSize, pageSize);
            if (culture != null)
            {
                parameters.Add(grid => grid.Culture, culture);
            }
            if (count.HasValue)
            {
                parameters.Add(grid => grid.Count, count.Value);
            }
            if (loadData != null)
            {
                parameters.Add<LoadDataArgs>(grid => grid.LoadData, loadData);
            }
        });
    }

    private static void AddColumn(RenderTreeBuilder builder, int sequence, string property, string title, string format = null, bool? visible = null)
    {
        builder.OpenComponent<RadzenDataGridColumn<ExportRow>>(sequence);
        builder.AddAttribute(sequence + 1, "Property", property);
        builder.AddAttribute(sequence + 2, "Title", title);
        if (format != null)
        {
            builder.AddAttribute(sequence + 3, "FormatString", format);
        }
        if (visible.HasValue)
        {
            builder.AddAttribute(sequence + 4, "Visible", visible.Value);
        }
        builder.CloseComponent();
    }

    private sealed record ExportRow(int Id, string Name, decimal Amount, DateTime Date, string Code)
    {
        public string Hidden => "hidden";
    }
}
