# RadioButtonList

The Blazor RadioButtonList shows a set of radio buttons bound to data, with horizontal or vertical orientation and null value support.

Keywords: toggle, form, edit

> API reference: [RadzenRadioButtonList API](https://blazor.radzen.com/api/radiobuttonlist.md)

## Examples

## Blazor RadioButtonList

The Blazor RadioButtonList shows a set of radio buttons bound to data, with horizontal or vertical orientation and null value support.

### Get and Set the value of RadioButtonList

As all Radzen Blazor input components the RadioButtonList has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList @bind-Value=@value TValue="int">
        <Items>
            <RadzenRadioButtonListItem Text="Orders" Value="1" />
            <RadzenRadioButtonListItem Text="Employees" Value="2" />
            <RadzenRadioButtonListItem Text="Customers" Value="3" />
        </Items>
    </RadzenRadioButtonList>
</div>

@code {
    int value = 1;
}
```


### Get and Set the value of RadioButtonList using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList Value=@value TValue="int" Change=@(args => value = args) >
        <Items>
            <RadzenRadioButtonListItem Text="Orders" Value="1" />
            <RadzenRadioButtonListItem Text="Employees" Value="2" />
            <RadzenRadioButtonListItem Text="Customers" Value="3" />
        </Items>
    </RadzenRadioButtonList>
</div>

@code {
    int value = 1;
}
```


### Set RadioButtonList orientation and layout

RadioButtonList layout can be configured via various properties for position and alignment such as `Orientation`, `AlignItems`, and `JustifyContent`. The behavior is similar to [RadzenStack](/stack).

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
    <RadzenRadioButtonList @bind-Value=@value TValue="int" Orientation="@orientation" Gap="@gap" JustifyContent="@justifyContent" AlignItems="@alignItems" Wrap="@wrap">
        <Items>
            <RadzenRadioButtonListItem Text="Orders" Value="1" />
            <RadzenRadioButtonListItem Text="Employees" Value="2" />
            <RadzenRadioButtonListItem Text="Customers" Value="3" />
        </Items>
    </RadzenRadioButtonList>
</RadzenStack>

@code {
    int value = 1;
    Orientation orientation = Orientation.Horizontal;
    AlignItems alignItems = AlignItems.Start;
    JustifyContent justifyContent = JustifyContent.Start;
    FlexWrap wrap = FlexWrap.Wrap;
    string gap = "0";
}
```


### Populate RadioButtonList items from data

Use the `Data` property to dynamically populate RadioButtonList items from a collection.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList Data="@data" @bind-Value=@value TValue="int" TextProperty="Name" ValueProperty="Id" />
</div>

@code {
    int value = 1;

    public class MyObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };
}
```


### Statically declared and populated RadioButtonList items from data

Combine statically declared items with data-bound items for flexible RadioButtonList configurations.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList Data="@data" @bind-Value=@value TValue="int" TextProperty="Name" ValueProperty="Id">
        <Items>
            <RadzenRadioButtonListItem Text="Static item" Value="0" />
        </Items>
    </RadzenRadioButtonList>
</div>

@code {
    int value = 1;

    public class MyObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };
}
```


### RadioButtonList with null value

RadioButtonList supports nullable value types, allowing no selection when the value is null.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList @bind-Value=@value TValue="int?">
        <Items>
            <RadzenRadioButtonListItem Text="Orders" Value="1" TValue="int?" />
            <RadzenRadioButtonListItem Text="Employees" Value="2" TValue="int?" />
            <RadzenRadioButtonListItem Text="Customers" Value="3" TValue="int?" />
        </Items>
    </RadzenRadioButtonList>
</div>

@code {
    int? value;
}
```


### Populate items programmatically and disable item

Use loops to programmatically create RadioButtonList items and set individual items as disabled.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenRadioButtonList @bind-Value=@value TValue="int">
        <Items>
            @foreach (var dataItem in data)
            {
                <RadzenRadioButtonListItem Text="@dataItem.Name" Value="@dataItem.Id" Disabled="@(dataItem.IsDisabled.HasValue ? dataItem.IsDisabled.Value : false)" Visible="@(dataItem.IsVisible.HasValue ? dataItem.IsVisible.Value : true)" />
            }
        </Items>
    </RadzenRadioButtonList>
</div>

@code {
    int value = 1;

    public class MyObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool? IsDisabled { get; set; }
        public bool? IsVisible { get; set; }
    }

    IEnumerable<MyObject> data = new MyObject[]
    {
        new MyObject() {
            Id = 1,
            Name = "Orders"
        },
        new MyObject() {
            Id = 2,
            Name = "Employees"
        },
        new MyObject() {
            Id = 3,
            Name = "Customers"
        },
        new MyObject() {
            Id = 4,
            Name = "Companies",
            IsDisabled = true,
        },
        new MyObject() {
            Id = 5,
            Name = "Companies (Old)",
            IsDisabled = true,
            IsVisible = false
        }
    };
}
```
