# Tree component
This article demonstrates how to use RadzenBlazorTree.

## Inline definition
The most straight forward way to configure a RadzenTree is to specify its items inline.
```
<RadzenTree>
    <RadzenTreeItem Text="BMW">
        <RadzenTreeItem Text="M3" />
        <RadzenTreeItem Text="M5" />
    </RadzenTreeItem>
    <RadzenTreeItem Text="Audi">
        <RadzenTreeItem Text="RS4" />
        <RadzenTreeItem Text="RS6" />
    </RadzenTreeItem>
    <RadzenTreeItem Text="Mercedes">
        <RadzenTreeItem Text="C63 AMG" />
        <RadzenTreeItem Text="S63 AMG" />
    </RadzenTreeItem>
</RadzenTree>
```
## Data-binding
Often you would like to data-bind RadzenTree to a collection of items. For example a collection of categories that has related products.
```
public class Category
{
   public string CategoryName { get; set; }
   public IEnumerable<Product> Products { get; set; }
}

public class Product
{
   public string ProductName { get; set; }
}
```
In this case you need to set the `Data` property of RadzenTree to your collection and add a few `RadzenTreeLevel`s that specify how each level of items should be data-bound.

```
<RadzenTree Data="@Northwind.Categories.Include(c => c.Products)">
    <RadzenTreeLevel TextProperty="CategoryName" ChildrenProperty="Products" />
    <RadzenTreeLevel TextProperty="ProductName" HasChildren="@((product) => false)" />
</RadzenTree>
```
The first `RadzenTreeLevel` says that the first (root) level of items have their `Text` set to the `CategoryName` property of the data. They also have children that are data-bound to the `Products` property.

The second `RadzenTreeLevel` says that the second level of items have their `Text` set to the `ProductName` property of the data. The don't have children (specified via `HasChildren`).

> [!NOTE] 
> Check how this example uses the `Include` Entity Framework method to load the related products of a category. If this isn't done the children items won't populate.

### Load on demand
The previous example loads all tree items instantly. Sometimes you would like to have full control over when children data is loaded. In that case you should use the `Expand` event.

```
<RadzenTree Data="@Northwind.Categories" Expand="@OnExpand">
    <RadzenTreeLevel TextProperty="CategoryName"/>
</RadzenTree>
@code {
    void OnExpand(TreeExpandEventArgs args)
    {
        var category = args.Value as Category;

        args.Children.Data = category.Include(c => c.Products).Products;
        args.Children.TextProperty = "ProductName";
        args.Children.HasChildren = (product) => false;
    }
}
```

Here we have only one level (for the categories). Children are populated in the `OnExpand` method that handles the `Expand` event.
To populate items on demand you need to configure the `Children` property of the `args` - set `Data`, `TextProperty` and `HasChildren`.

### Mixed data
The examples so far dealt with trees that had the same type of node per level - first level was categories and second level was products.
Here is how to have mixed types of nodes per level - files and directories in this case.

```
<RadzenTree Data="@entries" Expand="@LoadFiles">
    <RadzenTreeLevel Text="@GetTextForNode" />
</RadzenTree>
@code {
    IEnumerable<string> entries = null;

    protected override void OnInitialized()
    {
        entries = Directory.GetDirectories(HostEnvironment.ContentRootPath);
    }

    string GetTextForNode(object data)
    {
        return Path.GetFileName((string)data);
    }

    void LoadFiles(TreeExpandEventArgs args)
    {
        var directory = args.Value as string;

        args.Children.Data = Directory.EnumerateFileSystemEntries(directory);
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = (path) => Directory.Exists((string)path);
    }
}
```

## Templates
To customize the way a tree item appears (e.g. add icons, images or other custom markup) you can use the `Template` option.

### Templates in inline definition

Here is an example how to define a tree item template using inline definition.

```
<RadzenTree>
    <RadzenTreeItem Text="BMW">
        <Template>
            <strong>@context.Text</strong>
        </Template>
        <ChildContent>
            <RadzenTreeItem Text="M3" />
            <RadzenTreeItem Text="M5" />
        </ChildContent>
    </RadzenTreeItem>
</RadzenTree>
```

The `context` is the current `RadzenTreeItem`. You can use its properties as you see fit.

> [!IMPORTANT]
> Defining children requires a `ChildContent` element when a template is specified.

### Templates in data-binding
The `RadzenTreeLevel` also supports templates.
```
<RadzenTree Data="@Northwind.Categories.Include(c => c.Products)">
    <RadzenTreeLevel TextProperty="CategoryName" ChildrenProperty="Products">
        <Template>
            <strong>@(context as Category).CategoryName</strong>
        </Template>
    </RadzenTreeLevel>
    <RadzenTreeLevel TextProperty="ProductName" HasChildren="@((product) => false)" />
</RadzenTree>
```

Again the current `RadzenTreeItem` is represented as the `context` variable. Use the `Value` property to get the
current data item.

### Templates in load on demand
One can specify templates even in load on demand scenarios. The template should either be a custom Blazor component, or
you should use a `RenderFragment`.

```
<RadzenTree Data="@entries" Expand="@LoadFiles">
    <RadzenTreeLevel Text="@GetTextForNode" Template="@FileOrFolderTemplate" />
</RadzenTree>
@code {
    IEnumerable<string> entries = null;

    protected override void OnInitialized()
    {
        entries = Directory.GetDirectories(HostEnvironment.ContentRootPath);
    }

    string GetTextForNode(object data)
    {
        return Path.GetFileName((string)data);
    }

    RenderFragment<RadzenTreeItem> FileOrFolderTemplate = (context) => builder =>
    {
        string path = context.Value as string;
        bool isDirectory = Directory.Exists(path);

        // Add a RadzenIcon to the template

        builder.OpenComponent<RadzenIcon>(0);
        builder.AddAttribute(1, "Icon", isDirectory ? "folder" : "insert_drive_file");

        // Set some margin if the current data item is a file (!isDirectory)

        if (!isDirectory)
        {
            builder.AddAttribute(2, "Style", "margin-left: 24px");
        }

        builder.CloseComponent();

        // Append the current item text
        builder.AddContent(3, context.Text);
    };

    void LoadFiles(TreeExpandEventArgs args)
    {
        var directory = args.Value as string;

        args.Children.Data = Directory.EnumerateFileSystemEntries(directory);
        args.Children.Text = GetTextForNode;
        args.Children.HasChildren = (path) => Directory.Exists((string)path);

        // Propagate the Template to the children
        args.Children.Template = FileOrFolderTemplate;
    }
}
```

## Selection
You can control the selection state of RadzenTree component via its `Value` property. The tree item whose Value property is equal to the specified value will be selected.

> [!IMPORTANT]
> The type of the value you set must be `object`. RadzenTree can be bound to items of different type has its Value needs to be an `object`.

```
<RadzenTree Data="@categories" @bind-Value=@selectedCategory Change=@OnChange>
    <RadzenTreeLevel TextProperty="CategoryName"/>
</RadzenTree>
@code {
  IEnumerable<Category> categories;

  // Not that selectedCategory is object and not Category.
  object selectedCategory = null;

  protected override void OnInitialized()
  {
    categories = NorthwindDataContext.Categories;
    // Select the first category (if available)
    selectedCategory = categories.FirstOrDefault();
  }

  void OnChange()
  {
    // The selectedCategory field will contain the Category instance which the selected tree item represents

    var categoryName = (selectedCategory as Category).CategoryName;
  }
}
```

To clear the current selection set the Value property to `null`.

```
<RadzenTree Data="@categories" @bind-Value=@selectedCategory Change=@OnChange>
    <RadzenTreeLevel TextProperty="CategoryName"/>
    <RadzenButton Click=@ClearSelection Text="Clear selection" />
</RadzenTree>
@code {
  IEnumerable<Category> categories;
  object selectedCategory = null;

  void ClearSelection()
  {
     selectedCategory = null;
  }
}
```
## Checkboxes
RadzenTree supports checkboxes. To enable them set the `AllowCheckBoxes` property to `true`. To get or set the currently checked
items use `@bind-CheckedValues`.

```
<RadzenTree AllowCheckBoxes="true" @bind-CheckedValues="@CheckedValues">
</RadzenTree>
```

There are two more properties that are related to checkboxes:
- AllowCheckChildren - specifies what hapepens when a parent item is checked. If set to `true` checking parent items also checks all of its children.
- AllowCheckParents - specifies what hapepens with a parent item when one of its children is checked. If set to `true` checking a child item will affect the checked state of its parents.
