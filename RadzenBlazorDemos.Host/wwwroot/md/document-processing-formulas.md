# Spreadsheet: Formulas

Evaluate Excel formulas in Blazor and C#. Use them as cell formulas in a workbook, calculate them in code, or add your own custom Excel functions.

Keywords: document, processing, formula, formulas, excel, evaluate, calculate, engine, evaluator, custom, function, compound, vlookup, sum, average, if, iferror, edate, sumif

> API reference: [RadzenSpreadsheet API](https://blazor.radzen.com/api/spreadsheet.md)

## Examples

## Blazor Formulas

Evaluate Excel formulas in Blazor and C#. Use them as cell formulas in a workbook, calculate them directly in code, or register your own custom Excel functions. Supports Excel functions including SUM, AVERAGE, IF, VLOOKUP, XLOOKUP, EDATE and SUMIF.

### Formulas in code

Use Excel formulas inside a workbook. Edit any Qty or Price below and every dependent cell recomputes live.

```razor
<RadzenStack Gap="1rem">
    <RadzenTable>
        <RadzenTableHeader>
            <RadzenTableHeaderRow>
                <RadzenTableHeaderCell>Product</RadzenTableHeaderCell>
                <RadzenTableHeaderCell Style="text-align:right">Qty</RadzenTableHeaderCell>
                <RadzenTableHeaderCell Style="text-align:right">Price</RadzenTableHeaderCell>
                <RadzenTableHeaderCell Style="text-align:right">Subtotal</RadzenTableHeaderCell>
            </RadzenTableHeaderRow>
        </RadzenTableHeader>
        <RadzenTableBody>
            @for (var i = 0; i < productCount; i++)
            {
                var r = i + 1;
                <RadzenTableRow>
                    <RadzenTableCell>@sheet.Cells[r, 0].GetValueAsString()</RadzenTableCell>
                    <RadzenTableCell Style="text-align:right">
                        <RadzenNumeric TValue="double" Value=@GetDouble(r, 1)
                            ValueChanged=@(v => SetCellValue(r, 1, v)) Min="0" Style="width:90px" />
                    </RadzenTableCell>
                    <RadzenTableCell Style="text-align:right">
                        <RadzenNumeric TValue="double" Value=@GetDouble(r, 2)
                            ValueChanged=@(v => SetCellValue(r, 2, v)) Min="0" Format="0.00" Style="width:110px" />
                    </RadzenTableCell>
                    <RadzenTableCell Style="text-align:right; font-family:monospace">
                        <span style="opacity:0.6">@sheet.Cells[r, 3].Formula</span>
                        &nbsp;→&nbsp;
                        <strong>@sheet.Cells[r, 3].GetValueAsString()</strong>
                    </RadzenTableCell>
                </RadzenTableRow>
            }
            <RadzenTableRow>
                <RadzenTableCell Style="font-weight:600">Subtotal</RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell Style="text-align:right; font-family:monospace">
                    <span style="opacity:0.6">@sheet.Cells[5, 3].Formula</span>
                    &nbsp;→&nbsp;
                    <strong>@sheet.Cells[5, 3].GetValueAsString()</strong>
                </RadzenTableCell>
            </RadzenTableRow>
            <RadzenTableRow>
                <RadzenTableCell>Tax (8%)</RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell Style="text-align:right; font-family:monospace">
                    <span style="opacity:0.6">@sheet.Cells[6, 3].Formula</span>
                    &nbsp;→&nbsp;
                    <strong>@sheet.Cells[6, 3].GetValueAsString()</strong>
                </RadzenTableCell>
            </RadzenTableRow>
            <RadzenTableRow>
                <RadzenTableCell>Discount (subtotal &gt; $1000)</RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell Style="text-align:right; font-family:monospace">
                    <span style="opacity:0.6">@sheet.Cells[7, 3].Formula</span>
                    &nbsp;→&nbsp;
                    <strong>@sheet.Cells[7, 3].GetValueAsString()</strong>
                </RadzenTableCell>
            </RadzenTableRow>
            <RadzenTableRow>
                <RadzenTableCell Style="font-weight:700">Total</RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell></RadzenTableCell>
                <RadzenTableCell Style="text-align:right; font-family:monospace; font-weight:700">
                    <span style="opacity:0.6; font-weight:400">@sheet.Cells[8, 3].Formula</span>
                    &nbsp;→&nbsp;
                    @sheet.Cells[8, 3].GetValueAsString()
                </RadzenTableCell>
            </RadzenTableRow>
        </RadzenTableBody>
    </RadzenTable>
</RadzenStack>

@code {
    Workbook wb = default!;
    Worksheet sheet = default!;
    const int productCount = 3;

    protected override void OnInitialized()
    {
        wb = new Workbook();
        sheet = wb.AddSheet("Order", 12, 4);

        sheet.Cells[0, 0].Value = "Product";
        sheet.Cells[0, 1].Value = "Qty";
        sheet.Cells[0, 2].Value = "Price";
        sheet.Cells[0, 3].Value = "Subtotal";

        var seed = new (string Name, double Qty, double Price)[]
        {
            ("Laptop Pro", 2, 1200),
            ("USB Dock", 4, 80),
            ("Wireless Mouse", 5, 25),
        };
        for (var i = 0; i < seed.Length; i++)
        {
            var r = i + 1;
            sheet.Cells[r, 0].Value = seed[i].Name;
            sheet.Cells[r, 1].Value = seed[i].Qty;
            sheet.Cells[r, 2].Value = seed[i].Price;
            sheet.Cells[r, 3].Formula = $"=B{r + 1}*C{r + 1}";
        }

        sheet.Cells[5, 3].Formula = "=SUM(D2:D4)";
        sheet.Cells[6, 3].Formula = "=D6*0.08";
        sheet.Cells[7, 3].Formula = "=IF(D6>1000,-100,0)";
        sheet.Cells[8, 3].Formula = "=D6+D7+D8";
    }

    double GetDouble(int row, int col)
    {
        var v = sheet.Cells[row, col].Value;
        return v is null ? 0d : System.Convert.ToDouble(v);
    }

    void SetCellValue(int row, int col, double value)
    {
        sheet.Cells[row, col].Value = value;
    }
}
```


### Stateless evaluation

Calculate any Excel formula directly in C# without creating a workbook. Type an expression and get the result.

```razor
<RadzenStack Gap="0.5rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
        <RadzenTextBox @bind-Value=@expression Style="font-family:monospace; flex:1"
            Change=@(_ => Recompute()) />
        <RadzenButton Icon="play_arrow" Text="Evaluate" Click=@Recompute />
    </RadzenStack>
    <RadzenAlert AlertStyle=@alertStyle Variant="Variant.Flat" AllowClose="false">
        <strong>Result:</strong> @result
    </RadzenAlert>
</RadzenStack>

@code {
    string expression = "=SUM(1, 2, 3) + IF(2>1, 10, 20)";
    string result = "";
    AlertStyle alertStyle = AlertStyle.Light;

    protected override void OnInitialized() => Recompute();

    void Recompute()
    {
        try
        {
            var value = Formula.Evaluate(expression);
            result = value?.ToString() ?? "(null)";
            alertStyle = AlertStyle.Success;
        }
        catch (System.Exception ex)
        {
            result = ex.Message;
            alertStyle = AlertStyle.Warning;
        }
    }
}
```


### Stateful evaluation

Calculate Excel formulas with cell references. Change any input and all dependent formulas recompute automatically.

```razor
<RadzenTable>
    <RadzenTableHeader>
        <RadzenTableHeaderRow>
            <RadzenTableHeaderCell>Cell</RadzenTableHeaderCell>
            <RadzenTableHeaderCell>Value</RadzenTableHeaderCell>
        </RadzenTableHeaderRow>
    </RadzenTableHeader>
    <RadzenTableBody>
        <RadzenTableRow>
            <RadzenTableCell Style="font-family:monospace">A1</RadzenTableCell>
            <RadzenTableCell>
                <RadzenNumeric @bind-Value=@a1 TValue="double" Change=@(_ => Recompute()) Style="width:120px" />
            </RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell Style="font-family:monospace">B1</RadzenTableCell>
            <RadzenTableCell>
                <RadzenNumeric @bind-Value=@b1 TValue="double" Change=@(_ => Recompute()) Style="width:120px" />
            </RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell Style="font-family:monospace">C1</RadzenTableCell>
            <RadzenTableCell Style="font-family:monospace">
                <span style="opacity:0.6">=A1+B1</span>
                &nbsp;→&nbsp;
                <strong>@c1Result</strong>
            </RadzenTableCell>
        </RadzenTableRow>
        <RadzenTableRow>
            <RadzenTableCell Style="font-family:monospace">D1</RadzenTableCell>
            <RadzenTableCell Style="font-family:monospace">
                <span style="opacity:0.6">=C1*2</span>
                &nbsp;→&nbsp;
                <strong>@d1Result</strong>
            </RadzenTableCell>
        </RadzenTableRow>
    </RadzenTableBody>
</RadzenTable>

@code {
    double a1 = 2;
    double b1 = 3;
    string c1Result = "";
    string d1Result = "";

    protected override void OnInitialized() => Recompute();

    void Recompute()
    {
        var engine = new FormulaEngine();
        engine.Set("A1", a1);
        engine.Set("B1", b1);
        engine.Set("C1", "=A1+B1");
        engine.Set("D1", "=C1*2");
        c1Result = engine.Get("C1")?.ToString() ?? "(null)";
        d1Result = engine.Get("D1")?.ToString() ?? "(null)";
    }
}
```


### Custom functions

Add your own Excel functions and call them from any cell formula. Below: a custom `COMPOUND(principal, rate, years)` function for compound interest.

```razor
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
        <RadzenLabel Text="Principal" Component="principal" />
        <RadzenNumeric @bind-Value=@principal TValue="double" Min="0" Step="100"
            Change=@(_ => Recompute()) Name="principal" Style="width:130px" />

        <RadzenLabel Text="Annual rate (0.05 = 5%)" Component="rate" />
        <RadzenNumeric @bind-Value=@rate TValue="double" Step="0.01" Format="0.00"
            Change=@(_ => Recompute()) Name="rate" Style="width:90px" />

        <RadzenLabel Text="Years" Component="years" />
        <RadzenNumeric @bind-Value=@years TValue="double" Min="1" Max="50"
            Change=@(_ => Recompute()) Name="years" Style="width:80px" />
    </RadzenStack>

    <RadzenTable>
        <RadzenTableHeader>
            <RadzenTableHeaderRow>
                <RadzenTableHeaderCell>Context</RadzenTableHeaderCell>
                <RadzenTableHeaderCell>Formula</RadzenTableHeaderCell>
                <RadzenTableHeaderCell>Result</RadzenTableHeaderCell>
            </RadzenTableHeaderRow>
        </RadzenTableHeader>
        <RadzenTableBody>
            <RadzenTableRow>
                <RadzenTableCell>FormulaEngine (headless)</RadzenTableCell>
                <RadzenTableCell Style="font-family:monospace">=COMPOUND(@principal, @rate, @years)</RadzenTableCell>
                <RadzenTableCell Style="font-family:monospace">@engineResult</RadzenTableCell>
            </RadzenTableRow>
            <RadzenTableRow>
                <RadzenTableCell>Worksheet.FunctionRegistry</RadzenTableCell>
                <RadzenTableCell Style="font-family:monospace">=COMPOUND(A1, B1, C1)</RadzenTableCell>
                <RadzenTableCell Style="font-family:monospace">@worksheetResult</RadzenTableCell>
            </RadzenTableRow>
        </RadzenTableBody>
    </RadzenTable>

    <RadzenAlert AlertStyle="AlertStyle.Info" Variant="Variant.Flat" AllowClose="false">
        Both contexts share the exact same <code>FormulaFunction</code> subclass. Register it on the
        <code>FunctionStore</code> exposed by either <code>FormulaEngine.Functions</code> or
        <code>Worksheet.FunctionRegistry</code>, and any cell formula can call it.
    </RadzenAlert>
</RadzenStack>

@code {
    double principal = 1000;
    double rate = 0.05;
    double years = 3;
    string engineResult = "";
    string worksheetResult = "";

    protected override void OnInitialized() => Recompute();

    void Recompute()
    {
        // 1) Stateless via FormulaEngine
        var engine = new FormulaEngine();
        engine.Functions.Add<CompoundFunction>();
        engineResult = engine.Evaluate($"=COMPOUND({principal},{rate},{years})")?.ToString() ?? "(null)";

        // 2) Inside a Workbook via Worksheet.FunctionRegistry
        var wb = new Workbook();
        var sheet = wb.AddSheet("Calc", 1, 4);
        sheet.FunctionRegistry.Add<CompoundFunction>();
        sheet.Cells[0, 0].Value = principal;
        sheet.Cells[0, 1].Value = rate;
        sheet.Cells[0, 2].Value = years;
        sheet.Cells[0, 3].Formula = "=COMPOUND(A1, B1, C1)";
        worksheetResult = sheet.Cells[0, 3].Value?.ToString() ?? "(null)";
    }

    /// <summary>
    /// Compound interest: principal × (1 + rate)^years.
    /// One class, registered on either context, callable from any cell.
    /// </summary>
    public sealed class CompoundFunction : FormulaFunction
    {
        public override string Name => "COMPOUND";

        public override FunctionParameter[] Parameters =>
        [
            new("principal", ParameterType.Single, isRequired: true),
            new("rate",      ParameterType.Single, isRequired: true),
            new("years",     ParameterType.Single, isRequired: true),
        ];

        public override CellData Evaluate(FunctionArguments args)
        {
            System.ArgumentNullException.ThrowIfNull(args);
            var p = args.GetSingle("principal")?.GetValueOrDefault<double>() ?? 0d;
            var r = args.GetSingle("rate")?.GetValueOrDefault<double>() ?? 0d;
            var y = args.GetSingle("years")?.GetValueOrDefault<double>() ?? 0d;
            return CellData.FromNumber(p * System.Math.Pow(1d + r, y));
        }
    }
}
```
