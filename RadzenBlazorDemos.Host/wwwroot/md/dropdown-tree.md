# DropDown: DropDown with Tree

Combine a popup and a tree to build a Blazor DropDownTree for hierarchical single or multiple selection with filtering.

Keywords: select, picker, form, edit, dropdown, tree, hierarchical

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## DropDown with Tree

Combine a Popup and a Tree to build a Blazor DropDownTree for hierarchical single or multiple selection with filtering.

### Single selection

Click the dropdown to open a tree. Selecting a node closes the popup and updates the value.

```razor
<style>
    .rz-dropdown-tree-popup {
        display: none;
        position: absolute;
        overflow: hidden;
        width: 300px;
        border: var(--rz-panel-border);
        background-color: var(--rz-panel-background-color);
        box-shadow: var(--rz-panel-shadow);
        border-radius: var(--rz-border-radius);
    }
</style>

<div class="rz-p-0 rz-p-lg-12">
    <div style="display: inline-flex; flex-direction: column; gap: 0.5rem; width: 300px;">
        <RadzenLabel Text="Department" Component="department" />
        <div @ref=trigger style="position: relative;" @onclick="Toggle">
            <RadzenTextBox ReadOnly="true" Name="department"
                Value="@selectedText" Placeholder="Select a department..."
                Style="width: 100%; cursor: pointer;" />
            <RadzenIcon Icon="arrow_drop_down" Style="position: absolute; right: 8px; top: 50%; transform: translateY(-50%); pointer-events: none; color: var(--rz-text-tertiary-color);" />
        </div>
    </div>
</div>

<RadzenPopup @ref=popup id="departmentPopup" Style="padding: 0;" class="rz-dropdown-tree-popup"
    Close=@OnPopupClose>
    <RadzenTree Style="width: 100%; max-height: 300px; overflow: auto;" Data=@departments
        @bind-Value=@selection Change=@OnSelect>
        <RadzenTreeLevel Text=@(e => (e as Department).Name) ChildrenProperty="Children"
            HasChildren=@(e => (e as Department).Children?.Any() == true) Expanded=@(e => true) />
    </RadzenTree>
</RadzenPopup>

@code {
    ElementReference trigger;
    RadzenPopup popup;
    object selection;
    string selectedText;
    bool isOpen;
    IEnumerable<Department> departments;

    protected override void OnInitialized()
    {
        departments = new List<Department>
        {
            new Department("Company", new List<Department>
            {
                new Department("Engineering", new List<Department>
                {
                    new Department("Frontend"),
                    new Department("Backend"),
                    new Department("QA"),
                }),
                new Department("Design", new List<Department>
                {
                    new Department("UX"),
                    new Department("Visual Design"),
                }),
                new Department("Marketing", new List<Department>
                {
                    new Department("Content"),
                    new Department("SEO"),
                }),
                new Department("Sales"),
                new Department("HR"),
            }),
        };
    }

    async Task Toggle()
    {
        isOpen = !isOpen;
        await popup.ToggleAsync(trigger);
    }

    async Task OnSelect()
    {
        if(isOpen)
        {
            if (selection is Department dept)
            {
                selectedText = dept.Name;
            }
            isOpen = false;
            await popup.CloseAsync(trigger);
        }
    }

    void OnPopupClose()
    {
        isOpen = false;
    }

    record Department(string Name, List<Department> Children = null);
}
```


### Multiple selection with checkboxes

Use `AllowCheckBoxes` on the tree to enable multi-select. Selected items display as chips.

```razor
<style>
    .rz-dropdown-tree-multi-popup {
        display: none;
        position: absolute;
        overflow: hidden;
        width: 300px;
        border: var(--rz-panel-border);
        background-color: var(--rz-panel-background-color);
        box-shadow: var(--rz-panel-shadow);
        border-radius: var(--rz-border-radius);
    }
</style>

<div class="rz-p-0 rz-p-lg-12">
    <div style="display: inline-flex; flex-direction: column; gap: 0.5rem; width: 400px;">
        <RadzenLabel Text="Departments" Component="departments" />
        <div @ref=trigger style="position: relative;" @onclick="Toggle">
            <div class="rz-inputtext" style="min-height: 2.5rem; cursor: pointer; display: flex; align-items: center; flex-wrap: wrap; gap: 0.25rem; padding-right: 2rem;">
                @if (selectedItems.Any())
                {
                    @foreach (var item in selectedItems)
                    {
                        <RadzenBadge Text="@GetText(item)" BadgeStyle="BadgeStyle.Info" IsPill=true
                            Style="font-size: 0.75rem;" />
                    }
                }
                else
                {
                    <span style="color: var(--rz-text-tertiary-color);">Select departments...</span>
                }
            </div>
            <RadzenIcon Icon="arrow_drop_down" Style="position: absolute; right: 8px; top: 50%; transform: translateY(-50%); pointer-events: none; color: var(--rz-text-tertiary-color);" />
        </div>
    </div>
</div>

<RadzenPopup @ref=popup Lazy=true Style="padding: 0;" class="rz-dropdown-tree-multi-popup">
    <RadzenTree AllowCheckBoxes="true" Style="width: 100%; max-height: 300px; overflow: auto;" Data=@departments
        @bind-CheckedValues=@checkedValues>
        <RadzenTreeLevel Text=@(e => (e as Department).Name) ChildrenProperty="Children"
            HasChildren=@(e => (e as Department).Children?.Any() == true) Expanded=@(e => true) />
    </RadzenTree>
</RadzenPopup>

@code {
    ElementReference trigger;
    RadzenPopup popup;
    IEnumerable<object> checkedValues = Enumerable.Empty<object>();
    IEnumerable<Department> departments;

    IEnumerable<object> selectedItems => checkedValues?.Where(v => v is Department d && d.Children == null) ?? Enumerable.Empty<object>();

    string GetText(object data) => data is Department d ? d.Name : string.Empty;

    async Task Toggle()
    {
        await popup.ToggleAsync(trigger);
    }

    protected override void OnInitialized()
    {
        departments = new List<Department>
        {
            new Department("Company", new List<Department>
            {
                new Department("Engineering", new List<Department>
                {
                    new Department("Frontend"),
                    new Department("Backend"),
                    new Department("QA"),
                }),
                new Department("Design", new List<Department>
                {
                    new Department("UX"),
                    new Department("Visual Design"),
                }),
                new Department("Marketing", new List<Department>
                {
                    new Department("Content"),
                    new Department("SEO"),
                }),
                new Department("Sales"),
                new Department("HR"),
            }),
        };
    }

    record Department(string Name, List<Department> Children = null);
}
```


### Filtering

Add a search box inside the popup to filter tree items.

```razor
<style>
    .rz-dropdown-tree-filter-popup {
        display: none;
        position: absolute;
        overflow: hidden;
        width: 300px;
        border: var(--rz-panel-border);
        background-color: var(--rz-panel-background-color);
        box-shadow: var(--rz-panel-shadow);
        border-radius: var(--rz-border-radius);
    }
</style>

<div class="rz-p-0 rz-p-lg-12">
    <div style="display: inline-flex; flex-direction: column; gap: 0.5rem; width: 300px;">
        <RadzenLabel Text="Location" Component="location" />
        <div @ref=trigger style="position: relative;" @onclick="Toggle">
            <RadzenTextBox ReadOnly="true" Name="location"
                Value="@selectedText" Placeholder="Select a location..."
                Style="width: 100%; cursor: pointer;" />
            <RadzenIcon Icon="arrow_drop_down" Style="position: absolute; right: 8px; top: 50%; transform: translateY(-50%); pointer-events: none; color: var(--rz-text-tertiary-color);" />
        </div>
    </div>
</div>

<RadzenPopup @ref=popup id="locationPopup" Style="padding: 0;" class="rz-dropdown-tree-filter-popup"
    Close=@OnPopupClose>
    <RadzenStack Gap="0">
        <RadzenTextBox Placeholder="Search..." @oninput=@OnSearch Style="width: 100%; border: none; border-bottom: var(--rz-panel-border);" />
        <RadzenTree Style="width: 100%; max-height: 300px; overflow: auto;" Data=@filteredLocations
            @bind-Value=@selection Change=@OnSelect>
            <RadzenTreeLevel Text=@(e => (e as Location).Name) ChildrenProperty="FilteredChildren"
                HasChildren=@(e => (e as Location).FilteredChildren?.Any() == true) Expanded=@(e => true) />
        </RadzenTree>
    </RadzenStack>
</RadzenPopup>

@code {
    ElementReference trigger;
    RadzenPopup popup;
    object selection;
    string selectedText;
    string searchText = "";
    bool isOpen;
    List<Location> locations;
    IEnumerable<Location> filteredLocations;

    protected override void OnInitialized()
    {
        locations = new List<Location>
        {
            new Location("North America", new List<Location>
            {
                new Location("United States", new List<Location>
                {
                    new Location("New York"),
                    new Location("San Francisco"),
                    new Location("Chicago"),
                    new Location("Austin"),
                }),
                new Location("Canada", new List<Location>
                {
                    new Location("Toronto"),
                    new Location("Vancouver"),
                }),
            }),
            new Location("Europe", new List<Location>
            {
                new Location("United Kingdom", new List<Location>
                {
                    new Location("London"),
                    new Location("Manchester"),
                }),
                new Location("Germany", new List<Location>
                {
                    new Location("Berlin"),
                    new Location("Munich"),
                }),
                new Location("France", new List<Location>
                {
                    new Location("Paris"),
                    new Location("Lyon"),
                }),
            }),
            new Location("Asia", new List<Location>
            {
                new Location("Japan", new List<Location>
                {
                    new Location("Tokyo"),
                    new Location("Osaka"),
                }),
                new Location("India", new List<Location>
                {
                    new Location("Mumbai"),
                    new Location("Bangalore"),
                }),
            }),
        };
        filteredLocations = locations;
    }

    async Task Toggle()
    {
        isOpen = !isOpen;
        await popup.ToggleAsync(trigger);
    }

    async Task OnSelect()
    {
        if(isOpen)
        {
            if (selection is Location loc)
            {
                selectedText = loc.Name;
            }
            isOpen = false;
            await popup.CloseAsync(trigger);
        }
    }

    void OnPopupClose()
    {
        isOpen = false;
    }

    void OnSearch(ChangeEventArgs args)
    {
        searchText = args.Value?.ToString() ?? "";
        filteredLocations = string.IsNullOrWhiteSpace(searchText)
            ? locations
            : FilterLocations(locations, searchText).ToList();
    }

    IEnumerable<Location> FilterLocations(IEnumerable<Location> items, string search)
    {
        foreach (var item in items)
        {
            var matchesName = item.Name.Contains(search, StringComparison.OrdinalIgnoreCase);
            var filteredChildren = item.Children != null
                ? FilterLocations(item.Children, search).ToList()
                : null;

            if (matchesName || filteredChildren?.Any() == true)
            {
                yield return new Location(item.Name, item.Children)
                {
                    FilteredChildren = matchesName ? item.Children : filteredChildren
                };
            }
        }
    }

    class Location
    {
        public string Name { get; set; }
        public List<Location> Children { get; set; }
        public List<Location> FilteredChildren { get; set; }

        public Location(string name, List<Location> children = null)
        {
            Name = name;
            Children = children;
            FilteredChildren = children;
        }
    }
}
```
