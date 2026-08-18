# SelectBar

The Blazor SelectBar is a button-group selector for single or multiple choices, with icons and templates.

Keywords: form, edit

> API reference: [RadzenSelectBar API](https://blazor.radzen.com/api/selectbar.md)

## Examples

## Blazor SelectBar

The Blazor SelectBar is a button-group selector for single or multiple choices, with icons and templates.

### Get and Set the value of SelectBar

As all Radzen Blazor input components the SelectBar has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenDataGrid Data="@Items" TItem="GridItem">
    <HeaderTemplate>
        <RadzenStack AlignItems="AlignItems.Center">
            <RadzenSelectBar TValue="int" Size="ButtonSize.Small" @bind-Value="@SelectedAnimal">
                <Items>
                    <RadzenSelectBarItem Value="1" Text="Cat " Disabled="@_disableCat" />
                    <RadzenSelectBarItem Value="2" Text="Birds" />
                </Items>
            </RadzenSelectBar>
        </RadzenStack>
    </HeaderTemplate>
    <Columns>
        <RadzenDataGridColumn Title="Name" Property="Name" />
        <RadzenDataGridColumn Title="Action">
            <Template Context="data">
                <RadzenStack  Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center">
                    <RadzenSelectBar TValue="int" Size="ButtonSize.Small" @bind-Value="@data.Action" Change="() => CheckHeader()">
                        <Items>
                            <RadzenSelectBarItem Value="1" Text="Eats food" />
                            <RadzenSelectBarItem Value="2" Text="drinks water" />
                            <RadzenSelectBarItem Value="3" Text="flaps wings" />
                        </Items>
                    </RadzenSelectBar>
                </RadzenStack>
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>

@code {
    public List<GridItem> Items { get; private set; }
    public int SelectedAnimal { get; private set; }

    private const int _flapsWingsAction = 3;
    private const int _birdAnimal = 2;

    private bool _disableCat;

    protected override void OnInitialized()
    {
        Items = new List<GridItem>
        {
            new() { Name = "Animal Action", Action = 1 },
            new() { Name = "Animal Action", Action = 2 }
        };
    }

    private void CheckHeader()
    {
        _disableCat = false;
        if (Items.Any(x => x.Action == _flapsWingsAction))
        {
            _disableCat = true;
            SelectedAnimal = _birdAnimal;
        }
    }

    public class GridItem
    {
        public string Name { get; set; }
        public int Action { get; set; }
    }
}
```


### Get and Set the value of SelectBar using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    bool value;
}
```


### Multiple selection

Use `Multiple="true"` to enable selection of multiple items in the SelectBar.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@values TValue="IEnumerable<int>" Multiple="true">
        <Items>
            <RadzenSelectBarItem Text="Orders" Value="1" />
            <RadzenSelectBarItem Text="Employees" Value="2" />
            <RadzenSelectBarItem Text="Customers" Value="3" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    IEnumerable<int> values = new int[] { 1, 2 };
}
```


### Populate SelectBar items from data

Use the `Data` property to dynamically populate SelectBar items from a collection.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar Multiple="true" Data="@data" @bind-Value=@values TValue="IEnumerable<int>" TextProperty="Name" ValueProperty="Id" />
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


### Statically declared and populated SelectBar items from data

Combine statically declared items with data-bound items for flexible SelectBar configurations.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar Multiple="true" Data="@data" @bind-Value=@values TValue="IEnumerable<int>" TextProperty="Name" ValueProperty="Id">
        <Items>
            <RadzenSelectBarItem Text="Static item" Value="0" />
        </Items>
    </RadzenSelectBar>
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


### Populate items programmatically and disable item

Use loops to programmatically create SelectBar items and set individual items as disabled.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@value TValue="int">
        <Items>
            @foreach (var dataItem in data)
            {
                <RadzenSelectBarItem Text="@dataItem.Name" Value="@dataItem.Id" Disabled="@(dataItem.IsDisabled.HasValue ? dataItem.IsDisabled.Value : false)" Visible="@(dataItem.IsVisible.HasValue ? dataItem.IsVisible.Value : true)" />
            }
        </Items>
    </RadzenSelectBar>
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


### SelectBar with icons

Use the `Icon` property on SelectBar items to display icons alongside or instead of text.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Icon="filter" Text="On" Value="true" />
            <RadzenSelectBarItem Icon="filter_none" Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    bool value;
}
```


### SelectBar with images

Use the `ImageUrl` property on SelectBar items to display images in the selection buttons.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Image="images/radzen-nuget.png" ImageStyle="zoom: 30%; margin-inline-end: 20px;" Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    bool value;
}
```


### SelectBar with template

Use the `Template` property to fully customize the appearance of SelectBar items.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Value="true">
                <Template>
                    <i>On</i>
                </Template>
            </RadzenSelectBarItem>
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    bool value;
}
```


### SelectBar Size

Use the `Size` property to control the size of the SelectBar buttons (ExtraSmall, Small, Medium, Large).

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenSelectBar Size="ButtonSize.ExtraSmall" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
    <RadzenSelectBar Size="ButtonSize.Small" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
    <RadzenSelectBar Size="ButtonSize.Medium" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
    <RadzenSelectBar Size="ButtonSize.Large" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</div>

@code {
    bool value;
}
```


### SelectBar Orientation

Use the `Orientation` property to display SelectBar items horizontally or vertically.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="1rem" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenSelectBar Orientation="Orientation.Horizontal" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
    <RadzenSelectBar Orientation="Orientation.Vertical" @bind-Value=@value TValue="bool">
        <Items>
            <RadzenSelectBarItem Text="On" Value="true" />
            <RadzenSelectBarItem Text="Off" Value="false" />
        </Items>
    </RadzenSelectBar>
</RadzenStack>

@code {
    bool value;
}
```
