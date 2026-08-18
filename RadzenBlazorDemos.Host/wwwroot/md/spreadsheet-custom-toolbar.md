# Spreadsheet: Custom Toolbar

Replace the built-in toolbar with your own selection of tools. Reuse the predefined tool components in any order or layout, and add custom tools that dispatch undoable commands.

Keywords: spreadsheet, toolbar, custom, custom tools, childcontent, command, icommand, undo, extend

> API reference: [RadzenSpreadsheet API](https://blazor.radzen.com/api/spreadsheet.md)

## Examples

## Spreadsheet Custom Toolbar

Replace the built-in toolbar with your own selection of tools. Pick from the public tool components, reorder them, group them as you like, and add custom tools that dispatch undoable commands.

### Predefined tools in a custom layout

Add any of the `RadzenSpreadsheet*` tool components (e.g. `RadzenSpreadsheetBold`, `RadzenSpreadsheetColor`) to the spreadsheet's `ChildContent`. The host cascades the active `Worksheet` so the tools wire themselves up - no parameters needed. Include `RadzenSpreadsheetTableDesignToolset` to keep the contextual Table Design toolset.

```razor
<RadzenSpreadsheet Workbook=@workbook style="height: 500px">
    <RadzenTabsItem Text="File">
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.125rem" AlignItems="AlignItems.Center">
            <RadzenSpreadsheetOpen />
            <RadzenSpreadsheetSave />
        </RadzenStack>
    </RadzenTabsItem>
    <RadzenTabsItem Text="Edit">
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.125rem" AlignItems="AlignItems.Center">
            <RadzenSpreadsheetUndo />
            <RadzenSpreadsheetRedo />
            <div class="rz-toolbar-separator"></div>
            <RadzenSpreadsheetBold />
            <RadzenSpreadsheetItalic />
            <RadzenSpreadsheetUnderline />
            <div class="rz-toolbar-separator"></div>
            <RadzenSpreadsheetColor />
            <RadzenSpreadsheetBackgroundColor />
            <div class="rz-toolbar-separator"></div>
            <RadzenSpreadsheetMergeCells />
            <div class="rz-toolbar-separator"></div>
            <RadzenSpreadsheetAutoFilter />
            <RadzenSpreadsheetCustomSort />
        </RadzenStack>
    </RadzenTabsItem>
    <RadzenSpreadsheetTableDesignToolset />
</RadzenSpreadsheet>

@code {
    Workbook workbook = new Workbook();

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


### Custom tool with an undoable command

Build your own tool component and dispatch a custom `ICommand` through `ISpreadsheet.ExecuteAsync`. Declare a `SpreadsheetFeature` on the command and your tool participates in `ReadOnly`, the matching `Allow*` flag, the `CommandExecuting` event, and the undo/redo stack for free.

```razor
<RadzenSpreadsheet Workbook=@workbook SelectedToolsetIndex="0" style="height: 500px">
    <RadzenTabsItem Text="Review">
        <RadzenStack Orientation="Orientation.Horizontal" Gap="0.125rem" AlignItems="AlignItems.Center">
            <RadzenSpreadsheetUndo />
            <RadzenSpreadsheetRedo />
            <div class="rz-toolbar-separator"></div>
            <StampReviewedTool />
        </RadzenStack>
    </RadzenTabsItem>
</RadzenSpreadsheet>

@code {
    Workbook workbook = new Workbook();

    protected override void OnInitialized()
    {
        var sheet = workbook.AddSheet("Reviews", 10, 4);

        sheet.BeginUpdate();

        sheet.Columns[0] = 160;
        sheet.Columns[1] = 260;
        sheet.Columns[2] = 240;

        var headerFormat = new Format { Bold = true, BackgroundColor = "#1565c0", Color = "#ffffff", TextAlign = Radzen.TextAlign.Center };

        string[] headers = { "Product", "Notes", "Reviewed" };
        for (int i = 0; i < headers.Length; i++)
        {
            sheet.Cells[0, i].Value = headers[i];
            sheet.Cells[0, i].Format = headerFormat;
        }

        var rows = new (string Product, string Notes)[]
        {
            ("Laptop Pro 14",  "Looks great"),
            ("Ultra Monitor",  "Verify color accuracy"),
            ("Ergo Keyboard",  "Approved"),
            ("Wireless Mouse", "Battery life?"),
            ("Noise Headset",  "Solid mic"),
            ("USB Dock",       "Heat under load"),
        };

        for (int i = 0; i < rows.Length; i++)
        {
            int r = i + 1;
            sheet.Cells[r, 0].Value = rows[i].Product;
            sheet.Cells[r, 1].Value = rows[i].Notes;
        }

        // Start the cursor on the first empty Reviewed cell so the tool is one click away.
        sheet.Selection.Select(CellRef.Parse("C2"));
        sheet.Rows.Frozen = 1;

        sheet.EndUpdate();
    }
}
```
