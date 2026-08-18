# Table

Blazor RadzenTable component is used to create a HTML table with rows and cells.

Keywords: table, cells, row, grid

> API reference: [RadzenTable API](https://blazor.radzen.com/api/table.md)

## Examples

## Blazor Table

Display tabular data in a standard HTML table with rows and cells.

```razor
<RadzenCard Variant="Variant.Outlined" class="rz-my-4">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
        <RadzenCheckBox @bind-Value=@allowAlternatingRows Name="CheckBox1" TValue="bool" />
        <RadzenLabel Text="Allow alternating rows" Component="CheckBox1" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center" Style="margin-top:20px">
        <div>GridLines:</div>
        <RadzenSelectBar @bind-Value="@gridLines" TextProperty="Text" ValueProperty="Value"
        Data="@(Enum.GetValues(typeof(Radzen.DataGridGridLines)).Cast<Radzen.DataGridGridLines>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
    </RadzenStack>
</RadzenCard>

<RadzenTable GridLines="@gridLines" AllowAlternatingRows="@allowAlternatingRows">
    <RadzenTableHeader>
        <RadzenTableHeaderRow>
            <RadzenTableHeaderCell>
                Column 0
            </RadzenTableHeaderCell>
            <RadzenTableHeaderCell>
                Column 1
            </RadzenTableHeaderCell>
            <RadzenTableHeaderCell>
                Column 2
            </RadzenTableHeaderCell>
        </RadzenTableHeaderRow>
    </RadzenTableHeader>
    <RadzenTableBody>
        <RadzenTableRow>
            <RadzenTableCell>
                Cell 0 1
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 0 2
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 0 3
            </RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell>
                Cell 1 1
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 1 2
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 1 3
            </RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell>
                Cell 2 1
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 2 2
            </RadzenTableCell>
            <RadzenTableCell>
                Cell 2 3
            </RadzenTableCell>
        </RadzenTableRow>
    </RadzenTableBody>
</RadzenTable>
@code{
    Radzen.DataGridGridLines gridLines = Radzen.DataGridGridLines.Default;
    bool allowAlternatingRows = true;
}
```


### Dynamic Table

You can use `for` loops to create a `RadzenTable` with dynamic columns and rows.

```razor
<RadzenTable>
    <RadzenTableHeader>
        <RadzenTableHeaderRow>
            @for (var i = 0; i < cols; i++)
            {
                var col = i;
                <RadzenTableHeaderCell>
                    @($"Column {col}")
                </RadzenTableHeaderCell>
            }
        </RadzenTableHeaderRow>
    </RadzenTableHeader>
    <RadzenTableBody>
    @for (var i = 0; i < rows; i++)
    {
        var row = i;
        <RadzenTableRow>
            @for (var j = 0; j < cols; j++)
            {
                var cell = j;
                <RadzenTableCell>
                    @($"Cell {row} {cell}")
                </RadzenTableCell>
            }
        </RadzenTableRow>
    }
    </RadzenTableBody>
</RadzenTable>
@code{
    int rows = 10;
    int cols = 10;
}
```


### Scrollable Table

To enable row scrolling set the height of the `RadzenTable` component: `&lt;RadzenTable style="height: 800px" ...&gt;`. To enable column scrolling set the width of the header cells: `&lt;RadzenTableHeaderCell Style="width:150px"&gt;`.

```razor
<RadzenTable style="height:335px">
    <RadzenTableHeader>
        <RadzenTableHeaderRow>
            @for (var i = 0; i < cols; i++)
            {
                var col = i;
                <RadzenTableHeaderCell Style="width:150px">
                    @($"Column {col}")
                </RadzenTableHeaderCell>
            }
        </RadzenTableHeaderRow>
    </RadzenTableHeader>
    <RadzenTableBody>
    @for (var i = 0; i < rows; i++)
    {
        var row = i;
        <RadzenTableRow>
            @for (var j = 0; j < cols; j++)
            {
                var cell = j;
                <RadzenTableCell>
                    @($"Cell {row} {cell}")
                </RadzenTableCell>
            }
        </RadzenTableRow>
    }
    </RadzenTableBody>
</RadzenTable>
@code{
    int rows = 50;
    int cols = 50;
}
```


### Table with merged cells

Setting the `colspan` and `rowspan` attributes allows you to span cells across columns and rows.

```razor
<RadzenTable>
    <RadzenTableHeader>
        <RadzenTableHeaderRow>
            <RadzenTableHeaderCell colspan="2">Master column</RadzenTableHeaderCell>
            <RadzenTableHeaderCell rowspan="2">Column 3</RadzenTableHeaderCell>
        </RadzenTableHeaderRow>
        <RadzenTableHeaderRow>
            <RadzenTableHeaderCell>Column1</RadzenTableHeaderCell>
            <RadzenTableHeaderCell>Column2</RadzenTableHeaderCell>
        </RadzenTableHeaderRow>
    </RadzenTableHeader>
    <RadzenTableBody>
        <RadzenTableRow>
            <RadzenTableCell>Cell 1 1</RadzenTableCell>
            <RadzenTableCell>Cell 1 2</RadzenTableCell>
            <RadzenTableCell>Cell 1 3</RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell>Cell 2 1</RadzenTableCell>
            <RadzenTableCell>Cell 2 2</RadzenTableCell>
            <RadzenTableCell>Cell 2 3</RadzenTableCell>
        </RadzenTableRow>
    </RadzenTableBody>
</RadzenTable>
```
