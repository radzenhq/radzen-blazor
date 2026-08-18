# Spreadsheet: Permissions

Lock the spreadsheet for view-only embedding with ReadOnly, disable individual features with Allow* flags, or veto commands dynamically with a CommandExecuting handler.

Keywords: spreadsheet, permissions, readonly, read-only, view-only, allow, allowediting, allowfiltering, allowsorting, commandexecuting, preventdefault, audit, role, restrict

> API reference: [RadzenSpreadsheet API](https://blazor.radzen.com/api/spreadsheet.md)

## Examples

## Spreadsheet Permissions

Lock the spreadsheet for view-only embedding, disable individual features, or veto commands dynamically with a `CommandExecuting` handler. These are component-level rules — for workbook-level rules that round-trip through XLSX, see the [Protection demo](spreadsheet-protection).

### Read-only mode

Set `ReadOnly="true"` to reject every mutating command. The user can still scroll, select, and copy — formatting, autofill, paste, undo, sheet add/remove and every toolbar action are all silently dropped.

```razor
<RadzenSpreadsheet Workbook=@workbook ReadOnly="true" style="height: 500px" />

@code {
    Workbook workbook = new Workbook();

    protected override void OnInitialized()
    {
        var sheet = workbook.AddSheet("Q4 Report", 12, 6);

        sheet.BeginUpdate();

        sheet.Columns[0] = 160;
        sheet.Columns[1] = 110;
        sheet.Columns[2] = 110;
        sheet.Columns[3] = 110;
        sheet.Columns[4] = 110;

        var titleFormat = new Format { Bold = true, FontSize = 14, BackgroundColor = "#1565c0", Color = "#ffffff", TextAlign = Radzen.TextAlign.Center };
        var headerFormat = new Format { Bold = true, BackgroundColor = "#e3f2fd", Color = "#005da1", TextAlign = Radzen.TextAlign.Center };
        var currency = new Format { NumberFormat = "$#,##0" };
        var totalFormat = new Format { Bold = true, NumberFormat = "$#,##0", BackgroundColor = "#e8f5e9", Color = "#0f2e10" };

        sheet.MergedCells.Add(RangeRef.Parse("A1:E1"));
        sheet.Cells["A1"].Value = "Q4 2025 — Final Sales (locked)";
        sheet.Cells["A1"].Format = titleFormat;
        sheet.Rows[0] = 32;

        string[] headers = { "Region", "Oct", "Nov", "Dec", "Total" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cells[1, i].Value = headers[i];
            sheet.Cells[1, i].Format = headerFormat;
        }

        var rows = new (string Region, double Oct, double Nov, double Dec)[]
        {
            ("North America", 45200, 52100, 61300),
            ("Europe",        38400, 41200, 44500),
            ("Asia Pacific",  29100, 35600, 48900),
            ("Latin America", 15800, 18200, 24600),
            ("Middle East",   12300, 14500, 19200),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            int r = i + 2;
            sheet.Cells[r, 0].Value = rows[i].Region;
            sheet.Cells[r, 1].Value = rows[i].Oct; sheet.Cells[r, 1].Format = currency;
            sheet.Cells[r, 2].Value = rows[i].Nov; sheet.Cells[r, 2].Format = currency;
            sheet.Cells[r, 3].Value = rows[i].Dec; sheet.Cells[r, 3].Format = currency;
            sheet.Cells[r, 4].Formula = $"=SUM(B{r + 1}:D{r + 1})";
            sheet.Cells[r, 4].Format = totalFormat;
        }

        sheet.Rows.Frozen = 2;

        sheet.EndUpdate();
    }
}
```


### Configuration toggles

Each `Allow*` parameter independently gates a single feature area. The `Show*` parameters control which parts of the chrome are rendered. All flags are independent — flip them in any combination.

```razor
<RadzenStack Gap="1rem" class="rz-mb-4">
    <RadzenFieldset Text="Permissions">
        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="1.5rem">
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowEditing />
                <RadzenLabel Text="AllowEditing" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowFiltering />
                <RadzenLabel Text="AllowFiltering" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowSorting />
                <RadzenLabel Text="AllowSorting" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowCellFormatting />
                <RadzenLabel Text="AllowCellFormatting" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowClipboard />
                <RadzenLabel Text="AllowClipboard" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@allowUndoRedo />
                <RadzenLabel Text="AllowUndoRedo" />
            </RadzenStack>
        </RadzenStack>
    </RadzenFieldset>

    <RadzenFieldset Text="Visibility">
        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="1.5rem">
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@showToolbar />
                <RadzenLabel Text="ShowToolbar" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@showFormulaBar />
                <RadzenLabel Text="ShowFormulaBar" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
                <RadzenSwitch @bind-Value=@showSheetTabs />
                <RadzenLabel Text="ShowSheetTabs" />
            </RadzenStack>
        </RadzenStack>
    </RadzenFieldset>
</RadzenStack>

<RadzenSpreadsheet Workbook=@workbook style="height: 500px"
                   AllowEditing=@allowEditing
                   AllowFiltering=@allowFiltering
                   AllowSorting=@allowSorting
                   AllowCellFormatting=@allowCellFormatting
                   AllowClipboard=@allowClipboard
                   AllowUndoRedo=@allowUndoRedo
                   ShowToolbar=@showToolbar
                   ShowFormulaBar=@showFormulaBar
                   ShowSheetTabs=@showSheetTabs />

@code {
    Workbook workbook = new Workbook();

    bool allowEditing = true;
    bool allowFiltering = true;
    bool allowSorting = true;
    bool allowCellFormatting = true;
    bool allowClipboard = true;
    bool allowUndoRedo = true;
    bool showToolbar = true;
    bool showFormulaBar = true;
    bool showSheetTabs = true;

    protected override void OnInitialized()
    {
        var sheet = workbook.AddSheet("Reviews", 12, 5);

        sheet.BeginUpdate();

        sheet.Columns[0] = 160;
        sheet.Columns[1] = 120;
        sheet.Columns[2] = 110;
        sheet.Columns[3] = 240;

        var headerFormat = new Format { Bold = true, BackgroundColor = "#1565c0", Color = "#ffffff", TextAlign = Radzen.TextAlign.Center };
        var doneFormat = new Format { BackgroundColor = "#c8e6c9", Color = "#0f2e10" };
        var reviewFormat = new Format { BackgroundColor = "#fff8e1", Color = "#ff6f00" };

        string[] headers = { "Product", "Reviewer", "Status", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cells[0, i].Value = headers[i];
            sheet.Cells[0, i].Format = headerFormat;
        }

        var rows = new (string Product, string Reviewer, string Status, string Notes)[]
        {
            ("Laptop Pro 14",  "Alice", "Done",      "Looks great"),
            ("Ultra Monitor",  "Bob",   "In Review", "Verify color accuracy"),
            ("Ergo Keyboard",  "Alice", "Done",      "Approved"),
            ("Wireless Mouse", "Carol", "In Review", "Battery life?"),
            ("Noise Headset",  "Bob",   "Done",      "Solid mic"),
            ("USB Dock",       "Alice", "In Review", "Heat under load"),
            ("Office Chair",   "Carol", "Done",      "Comfortable"),
            ("Standing Desk",  "Bob",   "In Review", "Stability check pending"),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            int r = i + 1;
            sheet.Cells[r, 0].Value = rows[i].Product;
            sheet.Cells[r, 1].Value = rows[i].Reviewer;
            sheet.Cells[r, 2].Value = rows[i].Status;
            sheet.Cells[r, 2].Format = rows[i].Status == "Done" ? doneFormat : reviewFormat;
            sheet.Cells[r, 3].Value = rows[i].Notes;
        }

        sheet.Rows.Frozen = 1;

        sheet.EndUpdate();
    }
}
```


### Dynamic veto with CommandExecuting

Subscribe to `CommandExecuting` to veto specific commands at runtime — for role checks, audit gates, or any logic that depends on backend state. The handler is `async`, so you can `await` a backend check before calling `PreventDefault()`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem" class="rz-mb-4">
    <RadzenLabel Text="Role:" />
    <RadzenSelectBar @bind-Value=@isApprover TValue="bool" Size="ButtonSize.Small">
        <Items>
            <RadzenSelectBarItem Value="false" Text="Reviewer" />
            <RadzenSelectBarItem Value="true" Text="Approver" />
        </Items>
    </RadzenSelectBar>
    <RadzenText TextStyle="TextStyle.Body2" class="rz-color-secondary">
        Reviewers can edit values. Only approvers can change cell formatting.
    </RadzenText>
</RadzenStack>

<RadzenSpreadsheet Workbook=@workbook CommandExecuting=@OnCommandExecutingAsync style="height: 500px" />

@code {
    Workbook workbook = new Workbook();
    bool isApprover;

    async Task OnCommandExecutingAsync(SpreadsheetCommandEventArgs args)
    {
        // Formatting is the gated action in this scenario. The handler is async so a
        // real app can await a backend role check before deciding — here we simulate
        // network latency with a short delay.
        if (args.Command is FormatCommand)
        {
            await Task.Delay(150);

            if (!isApprover)
            {
                args.PreventDefault();
                NotificationService.Notify(NotificationSeverity.Warning,
                    "Formatting denied",
                    "Switch to Approver to change cell formatting.");
            }
        }
    }

    protected override void OnInitialized()
    {
        var sheet = workbook.AddSheet("Reviews", 12, 5);

        sheet.BeginUpdate();

        sheet.Columns[0] = 160;
        sheet.Columns[1] = 120;
        sheet.Columns[2] = 110;
        sheet.Columns[3] = 240;

        var headerFormat = new Format { Bold = true, BackgroundColor = "#1565c0", Color = "#ffffff", TextAlign = Radzen.TextAlign.Center };

        string[] headers = { "Product", "Reviewer", "Status", "Notes" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cells[0, i].Value = headers[i];
            sheet.Cells[0, i].Format = headerFormat;
        }

        var rows = new (string Product, string Reviewer, string Status, string Notes)[]
        {
            ("Laptop Pro 14",  "Alice", "Done",      "Looks great"),
            ("Ultra Monitor",  "Bob",   "In Review", "Verify color accuracy"),
            ("Ergo Keyboard",  "Alice", "Done",      "Approved"),
            ("Wireless Mouse", "Carol", "In Review", "Battery life?"),
            ("Noise Headset",  "Bob",   "Done",      "Solid mic"),
            ("USB Dock",       "Alice", "In Review", "Heat under load"),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            int r = i + 1;
            sheet.Cells[r, 0].Value = rows[i].Product;
            sheet.Cells[r, 1].Value = rows[i].Reviewer;
            sheet.Cells[r, 2].Value = rows[i].Status;
            sheet.Cells[r, 3].Value = rows[i].Notes;
        }

        sheet.Rows.Frozen = 1;

        sheet.EndUpdate();
    }
}
```
