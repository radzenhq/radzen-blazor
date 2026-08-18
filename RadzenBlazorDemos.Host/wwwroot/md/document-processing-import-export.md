# Spreadsheet: Import & Export

Import and export Excel (XLSX) and CSV files in Blazor. Upload a file, parse the data, and display the rows, or generate a file users can download.

Keywords: document, processing, import, export, xlsx, csv, excel, upload, download, read, write, parse, separator, encoding, quoting

> API reference: [RadzenSpreadsheet API](https://blazor.radzen.com/api/spreadsheet.md)

## Examples

## Import & Export

Read and write Excel (XLSX) and CSV files in Blazor. Upload a file from the user, parse the data, and display the rows; or generate a file in code and offer it as a download.

### XLSX

Import an Excel file uploaded by the user, or download an Excel file generated in code.

```razor
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenButton ButtonStyle="ButtonStyle.Base" Icon="upload_file" class="rz-fileupload-choose">
            <InputFile OnChange=@OnImportAsync accept=".xlsx" />
            <span class="rz-button-text">Import XLSX</span>
        </RadzenButton>
        <RadzenButton Icon="download" Text="Download XLSX" ButtonStyle="ButtonStyle.Primary"
            Click=@DownloadXlsx />
    </RadzenStack>

    <RadzenAlert AlertStyle="AlertStyle.Info" Variant="Variant.Flat" AllowClose="false">
        @status
    </RadzenAlert>

    @if (rows.Count > 0)
    {
        <RadzenTable>
            <RadzenTableHeader>
                <RadzenTableHeaderRow>
                    @foreach (var headerCell in rows[0])
                    {
                        <RadzenTableHeaderCell>@headerCell</RadzenTableHeaderCell>
                    }
                </RadzenTableHeaderRow>
            </RadzenTableHeader>
            <RadzenTableBody>
                @for (var r = 1; r < Math.Min(rows.Count, 25); r++)
                {
                    var row = rows[r];
                    <RadzenTableRow>
                        @foreach (var cell in row)
                        {
                            <RadzenTableCell>@cell</RadzenTableCell>
                        }
                    </RadzenTableRow>
                }
            </RadzenTableBody>
        </RadzenTable>
        @if (rows.Count > 25)
        {
            <RadzenText TextStyle="TextStyle.Caption">
                Showing first 25 rows of @rows.Count.
            </RadzenText>
        }
    }
</RadzenStack>

@code {
    Workbook workbook = BuildSampleWorkbook();
    List<List<string>> rows = new();
    string status = "Showing the pre-built sample workbook. Click Import XLSX to load your own file, or Download XLSX to save the current workbook.";

    protected override void OnInitialized()
    {
        Render(workbook);
    }

    static Workbook BuildSampleWorkbook()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Sales", 11, 5);
        sheet.BeginUpdate();

        var headerFormat = new Format { Bold = true, BackgroundColor = "#305496", Color = "#ffffff", TextAlign = TextAlign.Center };
        var currencyFormat = new Format { NumberFormat = "$#,##0.00" };

        sheet.Columns[0] = 110; sheet.Columns[1] = 130;
        for (var c = 2; c < 5; c++) sheet.Columns[c] = 110;

        string[] headers = { "Region", "Product", "Q1", "Q2", "Total" };
        for (var c = 0; c < headers.Length; c++)
        {
            sheet.Cells[0, c].Value = headers[c];
            sheet.Cells[0, c].Format = headerFormat;
        }

        var data = new (string Region, string Product, double Q1, double Q2)[]
        {
            ("EMEA", "Laptop Pro", 45200, 52100),
            ("EMEA", "Ultra Monitor", 18500, 21300),
            ("EMEA", "Ergo Keyboard", 8200, 7600),
            ("AMER", "Wireless Mouse", 5400, 6100),
            ("AMER", "Noise Headset", 12300, 14500),
            ("AMER", "USB Dock", 22100, 25400),
            ("APAC", "Office Chair", 15800, 17200),
            ("APAC", "Standing Desk", 28500, 31200),
            ("APAC", "Webcam Pro", 9800, 10750),
            ("APAC", "Cable Organizer", 3200, 3450),
        };
        for (var i = 0; i < data.Length; i++)
        {
            var r = i + 1;
            sheet.Cells[r, 0].Value = data[i].Region;
            sheet.Cells[r, 1].Value = data[i].Product;
            sheet.Cells[r, 2].Value = data[i].Q1; sheet.Cells[r, 2].Format = currencyFormat;
            sheet.Cells[r, 3].Value = data[i].Q2; sheet.Cells[r, 3].Format = currencyFormat;
            sheet.Cells[r, 4].Formula = $"=C{r + 1}+D{r + 1}";
            sheet.Cells[r, 4].Format = new Format { Bold = true, NumberFormat = "$#,##0.00" };
        }
        sheet.Rows.Frozen = 1;
        sheet.EndUpdate();
        return wb;
    }

    void Render(Workbook wb)
    {
        rows.Clear();
        var sheet = wb.Sheets[0];
        for (var r = 0; r < sheet.RowCount; r++)
        {
            var row = new List<string>();
            var hasContent = false;
            for (var c = 0; c < sheet.ColumnCount; c++)
            {
                var cell = sheet.Cells[r, c];
                var text = cell.GetValueAsString() ?? "";
                if (!string.IsNullOrEmpty(text)) hasContent = true;
                row.Add(text);
            }
            if (hasContent) rows.Add(row);
        }
    }

    async Task OnImportAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null) return;

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;

        workbook = Workbook.LoadFromStream(ms);
        Render(workbook);
        status = $"Imported {file.Name}: {rows.Count} populated rows across {workbook.Sheets.Count} sheet(s).";
    }

    async Task DownloadXlsx()
    {
        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;
        using var streamRef = new DotNetStreamReference(ms);
        await JSRuntime.InvokeVoidAsync("Radzen.downloadFile", "sales.xlsx", streamRef);
    }
}
```


### CSV

Import or export CSV files with configurable separator, quoting, and encoding (including UTF-8 BOM). The preview updates as you change options.

```razor
<RadzenStack Gap="1rem">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.25rem">
                <RadzenLabel Text="Separator" Component="sepDD" />
                <RadzenDropDown TValue="char" Data=@separators TextProperty="Text" ValueProperty="Value"
                    @bind-Value=@separator Name="sepDD" Style="width:140px" />
            </RadzenStack>

            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.25rem">
                <RadzenLabel Text="Quoting" Component="quotingDD" />
                <RadzenDropDown TValue="CsvQuoting" Data=@quotingOptions TextProperty="Text" ValueProperty="Value"
                    @bind-Value=@quoting Name="quotingDD" Style="width:120px" />
            </RadzenStack>

            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.25rem">
                <RadzenCheckBox @bind-Value=@withBom Name="bomChk" TValue="bool" />
                <RadzenLabel Text="UTF-8 BOM" Component="bomChk" />
            </RadzenStack>

            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.25rem">
                <RadzenCheckBox @bind-Value=@parseValues Name="parseChk" TValue="bool" />
                <RadzenLabel Text="Parse values on import" Component="parseChk" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
        <RadzenButton ButtonStyle="ButtonStyle.Base" Icon="upload_file" class="rz-fileupload-choose">
            <InputFile OnChange=@OnImportAsync accept=".csv,text/csv" />
            <span class="rz-button-text">Import CSV</span>
        </RadzenButton>
        <RadzenButton Icon="download" Text="Download CSV" ButtonStyle="ButtonStyle.Primary"
            Click=@DownloadCsv />
    </RadzenStack>

    <RadzenAlert AlertStyle="AlertStyle.Info" Variant="Variant.Flat" AllowClose="false">
        @status
    </RadzenAlert>

    @if (rows.Count > 0)
    {
        <RadzenTable>
            <RadzenTableHeader>
                <RadzenTableHeaderRow>
                    @foreach (var headerCell in rows[0])
                    {
                        <RadzenTableHeaderCell>@headerCell</RadzenTableHeaderCell>
                    }
                </RadzenTableHeaderRow>
            </RadzenTableHeader>
            <RadzenTableBody>
                @for (var r = 1; r < Math.Min(rows.Count, 25); r++)
                {
                    var row = rows[r];
                    <RadzenTableRow>
                        @foreach (var cell in row)
                        {
                            <RadzenTableCell>@cell</RadzenTableCell>
                        }
                    </RadzenTableRow>
                }
            </RadzenTableBody>
        </RadzenTable>
        @if (rows.Count > 25)
        {
            <RadzenText TextStyle="TextStyle.Caption">
                Showing first 25 rows of @rows.Count.
            </RadzenText>
        }
    }

    <RadzenStack Gap="0.5rem">
        <RadzenLabel Text="Generated CSV (preview)" />
        <RadzenTextArea @bind-Value=@previewText Rows="10" ReadOnly="true"
            Style="font-family:monospace; white-space:pre" />
    </RadzenStack>
</RadzenStack>

@code {
    Workbook workbook = BuildSampleWorkbook();
    List<List<string>> rows = new();
    string status = "Adjust the options above and click Download CSV. Or import a CSV to load it into the Workbook.";
    string previewText = "";

    char separator = ',';
    CsvQuoting quoting = CsvQuoting.Minimal;
    bool withBom = true;
    bool parseValues = true;

    record SeparatorOption(string Text, char Value);
    record QuotingOption(string Text, CsvQuoting Value);

    SeparatorOption[] separators =
    [
        new("Comma (,)", ','),
        new("Semicolon (;)", ';'),
        new("Tab", '\t'),
        new("Pipe (|)", '|'),
    ];

    QuotingOption[] quotingOptions =
    [
        new("Minimal", CsvQuoting.Minimal),
        new("Always", CsvQuoting.Always),
        new("Never", CsvQuoting.Never),
    ];

    protected override void OnInitialized()
    {
        Render(workbook);
        UpdatePreview();
    }

    static Workbook BuildSampleWorkbook()
    {
        var wb = new Workbook();
        var sheet = wb.AddSheet("Data", 6, 4);
        sheet.Cells[0, 0].SetValue("Country");
        sheet.Cells[0, 1].SetValue("City");
        sheet.Cells[0, 2].SetValue("Population");
        sheet.Cells[0, 3].SetValue("Note");

        var data = new (string Country, string City, double Population, string Note)[]
        {
            ("DE", "Berlin", 3677472, "Capital, has comma in this field"),
            ("FR", "Paris", 2102650, "It's the City of Light"),
            ("ES", "Madrid", 3223334, "Hosts \"La Liga\" matches"),
            ("US", "New York", 8804190, "Spans 5 boroughs"),
            ("JP", "Tokyo", 13960000, "Largest urban area in the world"),
        };
        for (var i = 0; i < data.Length; i++)
        {
            var r = i + 1;
            sheet.Cells[r, 0].Value = data[i].Country;
            sheet.Cells[r, 1].Value = data[i].City;
            sheet.Cells[r, 2].Value = data[i].Population;
            sheet.Cells[r, 3].Value = data[i].Note;
        }
        return wb;
    }

    void Render(Workbook wb)
    {
        rows.Clear();
        var sheet = wb.Sheets[0];
        for (var r = 0; r < sheet.RowCount; r++)
        {
            var row = new List<string>();
            var hasContent = false;
            for (var c = 0; c < sheet.ColumnCount; c++)
            {
                var cell = sheet.Cells[r, c];
                var text = cell.GetValueAsString() ?? "";
                if (!string.IsNullOrEmpty(text)) hasContent = true;
                row.Add(text);
            }
            if (hasContent) rows.Add(row);
        }
    }

    CsvExportOptions BuildExportOptions() => new CsvExportOptions
    {
        Separator = separator,
        Quoting = quoting,
        Encoding = withBom
            ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            : new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
    };

    void UpdatePreview()
    {
        using var ms = new MemoryStream();
        workbook.SaveAsCsv(ms, BuildExportOptions());
        var bytes = ms.ToArray();
        var preamble = BuildExportOptions().Encoding.GetPreamble();
        var skip = preamble.Length > 0 && bytes.Length >= preamble.Length
            && preamble.AsSpan().SequenceEqual(bytes.AsSpan(0, preamble.Length))
            ? preamble.Length : 0;
        previewText = Encoding.UTF8.GetString(bytes, skip, bytes.Length - skip);
    }

    protected override bool ShouldRender()
    {
        UpdatePreview();
        return true;
    }

    async Task OnImportAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null) return;

        using var ms = new MemoryStream();
        await file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024).CopyToAsync(ms);
        ms.Position = 0;

        var options = new CsvImportOptions
        {
            Separator = separator,
            ParseValues = parseValues,
        };
        workbook = Workbook.LoadFromCsv(ms, options);
        Render(workbook);
        var sheet = workbook.Sheets[0];
        status = $"Imported {file.Name}: {sheet.RowCount} rows × {sheet.ColumnCount} cols.";
        UpdatePreview();
    }

    async Task DownloadCsv()
    {
        using var ms = new MemoryStream();
        workbook.SaveAsCsv(ms, BuildExportOptions());
        ms.Position = 0;
        using var streamRef = new DotNetStreamReference(ms);
        await JSRuntime.InvokeVoidAsync("Radzen.downloadFile", "data.csv", streamRef);
    }
}
```
