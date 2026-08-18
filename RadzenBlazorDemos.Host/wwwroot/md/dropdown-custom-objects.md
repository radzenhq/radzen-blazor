# DropDown: Custom objects binding

Bind the Blazor DropDown to custom objects or enums, using TextProperty and ValueProperty to control the display text and the bound value.

Keywords: select, picker, form, edit, dropdown, custom, enum

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## DropDown data binding to custom objects

Bind the Blazor DropDown to custom objects or enums, using TextProperty and ValueProperty to control the display text and the bound value.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownCustomObjects" />
    <RadzenDropDown Data=@data TValue="MyObject" @bind-Value=@singleValue
                    AllowClear="true" AllowFiltering="true" Style="width: 100%; max-width: 400px;" Name="DropDownCustomObjects">
    </RadzenDropDown>
</RadzenStack>

@code {

    class MyObject
    { 
        public int Id { get; set; }
        public string Name { get; set; }

        public override bool Equals(object o)
        {
            var other = o as MyObject;

            return other?.Id == Id;
        }

        public override string ToString()
        {
            return $"Id: {Id}, Name: {Name}";
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    MyObject singleValue = new MyObject() { Id = 5, Name = "Name5" };
    IEnumerable<MyObject> data = Enumerable.Range(0, 100).Select(i => new MyObject() { Id = i, Name = $"Name{i}" });
}
```


### DropDown data binding to enum

Bind a DropDown directly to an enum type for easy selection of enumeration values.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownEnums" />
    <RadzenDropDown Data="@(Enum.GetValues(typeof(ColorType)).Cast<Enum>())" @bind-Value=@color
                    AllowClear="true" AllowFiltering="true" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" Style="width: 100%; max-width: 400px;" Name="DropDownEnums">
    </RadzenDropDown>
</RadzenStack>

@code {
    ColorType? color;

    public enum ColorType
    {
        Red,
        Green,
        Blue,
        [Display(Description = "Almond Green")]
        AlmondGreen,
        [Display(Description = "Amber Gray")]
        AmberGray,
        [Display(Description = "Apple Blue... ")]
        AppleBlueSeaGreen,
        //[Display(Description = "Miss", ResourceType = typeof(ResourceFile)] localization example
        [Display(Description = "Azure")]
        AzureBlue
    }
}
```
