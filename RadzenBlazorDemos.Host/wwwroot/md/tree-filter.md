# Tree: Tree filtering

This example demonstrates how to filter RadzenTree.

Keywords: tree, treeview, filter

> API reference: [RadzenTree API](https://blazor.radzen.com/api/tree.md)

## Examples

## Tree filtering

Filtering RadzenTree as you type.

```razor
<RadzenTextBox AutoCompleteType="AutoCompleteType.Off" placeholder="Find component ..." type="search" 
    @oninput="@Filter" style="width:100%" class="rz-search-input" aria-label="find" />

<RadzenTree Style="height:300px;width:100%;">
    @foreach (var category in examples)
    {
        <RadzenTreeItem @key=category Expanded=@category.Expanded Text="@category.Name">
            @if (category.Children != null)
            {
                @foreach (var example in category.Children)
                {
                    if (example.Children != null)
                    {
                        <RadzenTreeItem @key=example Expanded=@example.Expanded Text="@example.Name">
                            @foreach (var child in example.Children)
                            {
                                <RadzenTreeItem @key=child Expanded=@child.Expanded Text="@child.Name" />
                            }
                        </RadzenTreeItem>
                    }
                    else
                    {
                        <RadzenTreeItem @key=example Expanded=@example.Expanded Text="@example.Name" />
                    }
                }
            }
        </RadzenTreeItem>
    }
</RadzenTree>

@code {
    IEnumerable<Example> examples;

    protected override void OnInitialized()
    {
        examples = ExampleService.Examples;
    }

    void Filter(ChangeEventArgs args)
    {
        var term = $"{args.Value}";

        examples = string.IsNullOrEmpty(term) ? ExampleService.Examples : ExampleService.Filter(term);
    }
}
```
