# Column

The Blazor Column defines a responsive column within a Row's 12-column grid, sized per breakpoint.

Keywords: column, col, layout, responsive, grid

> API reference: [RadzenColumn API](https://blazor.radzen.com/api/column.md)

## Examples

## Blazor Column

The Blazor Column defines a responsive column within a Row's 12-column grid, sized per breakpoint.

### Auto-layout columns

If column `Size` is not specified, the column width is automatically calculated with respect to the remaining free space available.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 1 of 4
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 2 of 4
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 3 of 4
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 4 of 4
    </RadzenColumn>
</RadzenRow>
```


### Column sizes

When setting column `Size`, make sure that the total sum of all column sizes is not greater than 12. Otherwise, columns might wrap to a second row.

```razor
<RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H4" class="rz-mt-5">One column with a predefined Size</RadzenText>
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="4" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="4"
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Auto
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Auto
    </RadzenColumn>
</RadzenRow>

<RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H4" class="rz-mt-5">All columns with a predefined Size</RadzenText>
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="4" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-2 rz-p-md-5">
        Size="4"
    </RadzenColumn>
    <RadzenColumn Size="5" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-2 rz-p-md-5">
        Size="5"
    </RadzenColumn>
    <RadzenColumn Size="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-2 rz-p-md-5">
        Size="3"
    </RadzenColumn>
</RadzenRow>
```


### Responsive column sizes

Resize your browser window to see how the column adapts to the predefined breakpoint sizes. See [Breakpoints](/breakpoints) to learn more.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="12" SizeXS="11" SizeSM="10" SizeMD="9" SizeLG="8" SizeXL="7" SizeXX="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="12" SizeXS="11" SizeSM="10" SizeMD="9" SizeLG="8" SizeXL="7" SizeXX="6"
    </RadzenColumn>
</RadzenRow>
```


### Column wrapping

4 RadzenColumns with Size="6" render on two lines in a RadzenRow. Use `RowGap` property to specify the vertical spacing between columns on two or more lines in a row.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="0.5rem" RowGap="0.5rem">
    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="6"
    </RadzenColumn>
    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="6"
    </RadzenColumn>
    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="6"
    </RadzenColumn>
    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="6"
    </RadzenColumn>
</RadzenRow>
```


### Column offset

The `Offset` property moves the column to the right following the grid column layout. E.g. `Offset="3"` offsets 3 columns to the right.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="6" Offset="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Size="6" Offset="3"
    </RadzenColumn>
</RadzenRow>
```


### Responsive offsetting


```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Offset="0" OffsetXS="1" OffsetSM="2" OffsetMD="3" OffsetLG="4" OffsetXL="5" OffsetXX="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Offset="0" OffsetXS="1" OffsetSM="2" OffsetMD="3" OffsetLG="4" OffsetXL="5" OffsetXX="6"
    </RadzenColumn>
</RadzenRow>
```


### Column order

The `Order` property is used to reorder columns visually.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="4" Order="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 1, Order="3"
    </RadzenColumn>
    <RadzenColumn Size="4" OrderLG="1" class="rz-background-color-success-lighter rz-color-on-success-lighter rz-p-5">
        Column 2, Order="1"
    </RadzenColumn>
    <RadzenColumn Size="4" OrderLG="2" class="rz-background-color-danger-lighter rz-color-on-danger-lighter rz-p-5">
        Column 3, Order="2"
    </RadzenColumn>
</RadzenRow>
```


### Responsive column ordering

You can reorder columns on different screen sizes. Resize your browser window to see how the columns reorder. See [Breakpoints](/breakpoints) to learn more.

```razor
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Order="1" OrderMD="4" OrderLG="6" OrderXL="8" class="rz-background-color-danger-lighter rz-color-on-danger-lighter rz-p-5">
        Order="1", OrderMD="4" OrderLG="6" OrderXL="8"
    </RadzenColumn>
    <RadzenColumn Order="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Order="3"
    </RadzenColumn>
    <RadzenColumn Order="5" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Order="5"
    </RadzenColumn>
    <RadzenColumn Order="7" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Order="7"
    </RadzenColumn>
</RadzenRow>
```


### Nested Layouts


```razor
<RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H4" class="rz-mt-5">Auto-layout columns</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-3">Example with 3 levels of nesting. You can nest rows and columns indefinitely.</RadzenText>
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
        Level 1
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter">
        <RadzenRow>
            <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
                Level 2
            </RadzenColumn>
            <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter">
                <RadzenRow>
                    <RadzenColumn class="rz-background-color-info-lighter rz-color-info-darker rz-py-5">
                        Level 3
                    </RadzenColumn>
                    <RadzenColumn class="rz-background-color-info-lighter rz-color-info-darker rz-py-5">
                        Level 3
                    </RadzenColumn>
                </RadzenRow>
            </RadzenColumn>
            <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
                Level 2
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
</RadzenRow>

<RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.H4" class="rz-mt-5">Columns with a predefined Size</RadzenText>
<RadzenText TextStyle="TextStyle.Body2" class="rz-mb-3">The second column contains a nested row with 4 columns.</RadzenText>
<RadzenRow class="rz-text-align-center rz-border-info-light" Gap="1rem">
    <RadzenColumn Size="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
        Size="3"
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
        Auto size
        <RadzenRow class="rz-text-align-center rz-border-info-light rz-mt-5">
            <RadzenColumn Size="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
                Size="3"
            </RadzenColumn>
            <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
                Size="6"
                <RadzenRow class="rz-text-align-center rz-border-info-light rz-mt-5">
                    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-info-darker rz-py-5">
                        Size="6"
                    </RadzenColumn>
                    <RadzenColumn Size="6" class="rz-background-color-info-lighter rz-color-info-darker rz-py-5">
                        Size="6"
                    </RadzenColumn>
                </RadzenRow>
            </RadzenColumn>
            <RadzenColumn Size="3" class="rz-background-color-info-lighter rz-color-on-info-lighter rz-py-5">
                Size="3"
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
</RadzenRow>
```


### Gutters

By default, the spacing between columns is set to `1rem`, via the `--rz-gap` CSS variable. Spacing between columns can be controlled by setting the `Gap` property of the parent `RadzenRow` component.

```razor
<RadzenRow Wrap="FlexWrap.NoWrap" Gap="10px" class="rz-text-align-center rz-border-info-light">
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">1</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">2</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">3</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">4</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">5</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">6</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">7</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">8</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">9</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">10</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">11</RadzenColumn>
    <RadzenColumn class="rz-background-color-danger-lighter rz-color-on-danger-lighter">12</RadzenColumn>
</RadzenRow>
<RadzenRow Gap="10px" class="rz-text-align-center rz-border-info-light">
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 1 of 3
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 2 of 3
    </RadzenColumn>
    <RadzenColumn class="rz-background-color-info-lighter rz-color-on-info-lighter rz-p-5">
        Column 3 of 3
    </RadzenColumn>
</RadzenRow>
```
