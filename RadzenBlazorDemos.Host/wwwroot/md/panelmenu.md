# PanelMenu

The Blazor PanelMenu is a vertical, expandable sidebar menu with nested items - ideal for app navigation.

Keywords: navigation, menu

> API reference: [RadzenPanelMenu API](https://blazor.radzen.com/api/panelmenu.md)

## Examples

## Blazor PanelMenu

The Blazor PanelMenu is a vertical, expandable sidebar menu with nested items - ideal for app navigation.

### Statically declared items

Create a collapsible menu by statically declaring `RadzenPanelMenuItem` components in markup.

```razor
<RadzenStack AlignItems="AlignItems.Center" class="rz-p-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
        <RadzenCheckBox @bind-Value=@multiple Name="CheckBox1" TValue="bool" />
        <RadzenLabel Text="Allow multiple expand" Component="CheckBox1" />
    </RadzenStack>

    <RadzenPanelMenu Click="OnParentClicked" Style="width:300px" Multiple="@multiple">
        <RadzenPanelMenuItem Text="General" Icon="home">
            <RadzenPanelMenuItem Text="Buttons" Path="buttons" Icon="account_circle"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Menu" Path="menu" Icon="line_weight"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="FileInput" Path="fileinput" Icon="attach_file"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Dialog" Path="dialog" Icon="perm_media"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Notification" Path="notification" Icon="announcement"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Inputs" Icon="payment">
            <RadzenPanelMenuItem Text="CheckBox" Path="checkbox" Icon="check_circle"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TextBox" Path="textbox" Icon="input"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TextArea" Path="textarea" Icon="description"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Password" Path="password" Icon="payment"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Numeric" Path="numeric" Icon="aspect_ratio"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DatePicker" Path="datepicker" Icon="date_range"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Data" Icon="save">
            <RadzenPanelMenuItem Text="DataGrid" Path="datagrid" Icon="grid_on"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DataList" Path="datalist" Icon="list"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DropDown" Path="dropdown" Icon="dns"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DropDownDataGrid" Path="dropdown-datagrid" Icon="receipt"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="ListBox" Path="listbox" Icon="view_list"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TemplateForm" Path="templateform" Icon="line_style"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Containers" Icon="account_box">
            <RadzenPanelMenuItem Text="Tabs" Path="tabs" Icon="tab"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Panel" Path="panel" Icon="content_paste"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Fieldset" Path="fieldset" Icon="account_balance_wallet"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Card" Path="card" Icon="line_style"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="More">
            <RadzenPanelMenuItem Click="OnChildClicked" Text="Item1"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Click="OnChildClicked" Text="Item2"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="More items">
                <RadzenPanelMenuItem Text="More sub items">
                    <RadzenPanelMenuItem Click="OnChildClicked" Text="Item3"></RadzenPanelMenuItem>
                    <RadzenPanelMenuItem Click="OnChildClicked" Text="Item4"></RadzenPanelMenuItem>
                </RadzenPanelMenuItem>
            </RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Disabled Menu" Icon="desktop_access_disabled" Disabled="true"></RadzenPanelMenuItem>
    </RadzenPanelMenu>

</RadzenStack>
<EventConsole @ref=@console />
@code {
    bool multiple = true;
    EventConsole console;
    
    void OnParentClicked(MenuItemEventArgs args)
    {
        console.Log($"{args.Text} clicked from parent");
    }

    void OnChildClicked(MenuItemEventArgs args)
    {
        console.Log($"{args.Text} from child clicked");
    }
}
```


### Programmatically created items with Expanded binding

Generate menu items dynamically from data and control expansion state with two-way binding.

```razor
<RadzenStack AlignItems="AlignItems.Center" class="rz-p-12">
    <RadzenPanelMenu Style="width:300px">
        <ChildContent>
            @foreach (var item in data)
            {
                <RadzenPanelMenuItem Text="@item.Text" @bind-Expanded="@item.Expanded">
                    <ChildContent>
                        @foreach (var subItem in item.Items)
                        {
                            <RadzenPanelMenuItem Text="@subItem.Text"  />
                        }
                    </ChildContent>
                    </RadzenPanelMenuItem>
                }
        </ChildContent>
    </RadzenPanelMenu>
</RadzenStack>
    
@code {

    static List<MenuModel> data = Enumerable.Range(0, 5).Select(i => new MenuModel(() => data)
    {
        Text = $"Menu{i}",
        Expanded = i == 0,
        Items = Enumerable.Range(0, 5).Select(j => new MenuModel(() => data)
        {
            Text = $"SubMenu{i}{j}"
        })
    }).ToList();

    public class MenuModel : INotifyPropertyChanged
    {
        Func<List<MenuModel>> collection;
        public MenuModel(Func<List<MenuModel>> collection)
        {
            this.collection = collection;
        }

        public string Text { get; set; }

        bool _expanded;
        public bool Expanded 
        {
            get
            {
                return _expanded;    
            }
            set
            {
                if (_expanded != value)
                {
                    collection()?.Where(i => i != this).ToList().ForEach(s => s.Expanded = false);

                    _expanded = value;
                    OnPropertyChanged(nameof(Expanded));
                }
            }
        }

        public IEnumerable<MenuModel> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null) 
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
```


### Set the display style of menu items

Customize the visual style of menu items using the `DisplayStyle` property for different appearance options.

```razor
<RadzenStack Gap="1rem" AlignItems="AlignItems.Center" class="rz-p-12">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1.5rem">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem;">
                <RadzenLabel Text="DisplayStyle:" Component="DropDown1" />
                <RadzenSelectBar @bind-Value="@DisplayStyle" TextProperty="Text" ValueProperty="Value" Name="DropDown1"
                                Data="@(Enum.GetValues(typeof(MenuItemDisplayStyle)).Cast<MenuItemDisplayStyle>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem;">
                <RadzenCheckBox @bind-Value=@isShowArrow Name="CheckBox2" TValue="bool" />
                <RadzenLabel Text="Show Arrow" Component="CheckBox2" />
            </RadzenStack>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem;">
                <RadzenCheckBox @bind-Value=@changeOnOver Name="CheckBox3" TValue="bool" />
                <RadzenLabel Text="Change style on mouse over" Component="CheckBox3" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>

    <RadzenPanelMenu DisplayStyle="@DisplayStyle" ShowArrow="@isShowArrow" Multiple=false
                     @onmouseover="@(args => { if(changeOnOver) { DisplayStyle = MenuItemDisplayStyle.IconAndText; } })"
                     @onmouseout="@(args => { if(changeOnOver) { DisplayStyle = MenuItemDisplayStyle.Icon; } })">
        <RadzenPanelMenuItem Text="General" Icon="home">
            <RadzenPanelMenuItem Text="Buttons" Path="buttons" Icon="account_circle"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Menu" Path="menu" Icon="line_weight"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="FileInput" Path="fileinput" Icon="attach_file"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Dialog" Path="dialog" Icon="perm_media"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Notification" Path="notification" Icon="announcement"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Inputs" Icon="payment">
            <RadzenPanelMenuItem Text="CheckBox" Path="checkbox" Icon="check_circle"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TextBox" Path="textbox" Icon="input"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TextArea" Path="textarea" Icon="description"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Password" Path="password" Icon="payment"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Numeric" Path="numeric" Icon="aspect_ratio"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DatePicker" Path="datepicker" Icon="date_range"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Data" Icon="save">
            <RadzenPanelMenuItem Text="DataGrid" Path="datagrid" Icon="grid_on"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DataList" Path="datalist" Icon="list"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DropDown" Path="dropdown" Icon="dns"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="DropDownDataGrid" Path="dropdown-datagrid" Icon="receipt"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="ListBox" Path="listbox" Icon="view_list"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="TemplateForm" Path="templateform" Icon="line_style"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
        <RadzenPanelMenuItem Text="Containers" Icon="account_box">
            <RadzenPanelMenuItem Text="Tabs" Path="tabs" Icon="tab"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Panel" Path="panel" Icon="content_paste"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Fieldset" Path="fieldset" Icon="account_balance_wallet"></RadzenPanelMenuItem>
            <RadzenPanelMenuItem Text="Card" Path="card" Icon="line_style"></RadzenPanelMenuItem>
        </RadzenPanelMenuItem>
    </RadzenPanelMenu>

</RadzenStack>

@code {
    MenuItemDisplayStyle DisplayStyle = MenuItemDisplayStyle.IconAndText;

    bool isShowArrow = true;
    bool changeOnOver;
}
```


### Navigation Rail

Use `DisplayStyle="MenuItemDisplayStyle.IconAndTextStacked"` to arrange icons above text for a modern navigation rail layout.

```razor
<RadzenStack Gap="1rem" AlignItems="AlignItems.Center" class="rz-p-12">
    <RadzenPanelMenu DisplayStyle="MenuItemDisplayStyle.IconAndTextStacked" Multiple=false Style="width: 10rem;">
        <RadzenPanelMenuItem Text="Resources" Icon="folder" Path="resources" />
        <RadzenPanelMenuItem Text="Console" Icon="description" Path="console" />
        <RadzenPanelMenuItem Text="Logs" Icon="article" Path="logs" />
        <RadzenPanelMenuItem Text="Traces" Icon="timeline" Path="traces" />
        <RadzenPanelMenuItem Text="Metrics" Icon="bar_chart" Path="metrics" />
    </RadzenPanelMenu>
</RadzenStack>
```
