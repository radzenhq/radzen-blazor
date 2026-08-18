# CheckBoxList

The Blazor CheckBoxList lets users select multiple items from a data-bound list, with orientation, select-all, and item templates.

Keywords: form, edit

> API reference: [RadzenCheckBoxList API](https://blazor.radzen.com/api/checkboxlist.md)

## Examples

## Blazor CheckBoxList

The Blazor CheckBoxList lets users select multiple items from a data-bound list, with orientation, select-all, and item templates.

### Get and Set the value of CheckBoxList

As all Radzen Blazor input components the CheckBoxList has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList @bind-Value=@values TValue="int">
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```


### Get and Set the value of CheckBoxList using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList Value=@values TValue="int" Change=@(args => values = args)>
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```


### Set CheckBoxList orientation and layout

CheckBoxList layout can be configured via various properties for position and alignment such as `Orientation`, `AlignItems`, and `JustifyContent`. The behavior is similar to [RadzenStack](/stack).

```razor
<RadzenStack class="rz-p-0 rz-p-md-12" Gap="2rem">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Orientation
                <RadzenDropDown @bind-Value="@orientation" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "orientation" }})"
                            Data="@(Enum.GetValues(typeof(Orientation)).Cast<Orientation>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                AlignItems
                <RadzenDropDown @bind-Value="@alignItems" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "align items" }})"
                            Data="@(Enum.GetValues(typeof(AlignItems)).Cast<AlignItems>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                JustifyContent
                <RadzenDropDown @bind-Value="@justifyContent" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "justify content" }})"
                            Data="@(Enum.GetValues(typeof(JustifyContent)).Cast<JustifyContent>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Wrap
                <RadzenDropDown @bind-Value="@wrap" TextProperty="Text" ValueProperty="Value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "wrap" }})"
                            Data="@(Enum.GetValues(typeof(FlexWrap)).Cast<FlexWrap>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                Gap
                <RadzenTextBox @bind-Value="@gap" aria-label="gap" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenCheckBoxList @bind-Value=@values TValue="int" Orientation="@orientation" Gap="@gap" JustifyContent="@justifyContent" AlignItems="@alignItems" Wrap="@wrap">
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</RadzenStack>

@code {
    IEnumerable<int> values = new int[] { 1 };
    Orientation orientation = Orientation.Horizontal;
    AlignItems alignItems = AlignItems.Start;
    JustifyContent justifyContent = JustifyContent.Start;
    FlexWrap wrap = FlexWrap.Wrap;
    string gap = "0";
}
```


### Populate CheckBoxList items from data

Use the `Data` property to dynamically populate CheckBoxList items from a collection.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList Data="@data" @bind-Value=@values TValue="int" TextProperty="Name" ValueProperty="Id"
                        ReadOnlyProperty="ReadOnly" DisabledProperty="Disabled" />
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };

    public class MyObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool ReadOnly { get; set; }
        public bool Disabled { get; set; }
    }

    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders", ReadOnly = true}, new MyObject() { Id = 2 , Name = "Employees", Disabled = true}, new MyObject() { Id = 3 , Name = "Customers" } };
}
```


### Statically declared and populated CheckBoxList items from data

Combine statically declared items with data-bound items for flexible CheckBoxList configurations.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList Data="@data" @bind-Value=@values TValue="int" TextProperty="Name" ValueProperty="Id">
        <Items>
            <RadzenCheckBoxListItem Text="Static item" Value="0" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };

    public class MyObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };
}
```


### Select all CheckBoxList items

Implement select all functionality to quickly check or uncheck all items in the CheckBoxList.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList @bind-Value=@values AllowSelectAll="true" SelectAllText="Select all items" TValue="int" Orientation="Orientation.Vertical">
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```


### Disabled CheckBoxList item

Disable specific CheckBoxList items to prevent user interaction with those items.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList @bind-Value=@values TValue="int">
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" Disabled=true />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```


### ReadOnly CheckBoxList item

Make specific CheckBoxList items read-only to prevent changes while keeping them interactive.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList @bind-Value=@values TValue="int">
        <Items>
            <RadzenCheckBoxListItem Text="Orders" Value="1" ReadOnly=true />
            <RadzenCheckBoxListItem Text="Employees" Value="2" />
            <RadzenCheckBoxListItem Text="Customers" Value="3" />
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```


### Templated CheckBoxList item

Use the `Template` property to customize the appearance of CheckBoxList items.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBoxList @bind-Value=@values TValue="int">
        <Items>
            <RadzenCheckBoxListItem Value="1">
                <Template>
                    <span style="color: green;">Orders</span>
                </Template>
            </RadzenCheckBoxListItem>
            <RadzenCheckBoxListItem Value="2">
                <Template>
                    <span style="color: orange;">Employees</span>
                </Template>
            </RadzenCheckBoxListItem>
            <RadzenCheckBoxListItem Value="3">
                <Template>
                    <span style="color: red;">Customers</span>
                </Template>
            </RadzenCheckBoxListItem>
        </Items>
    </RadzenCheckBoxList>
</div>

@code {
    IEnumerable<int> values = new int[] { 1 };
}
```
