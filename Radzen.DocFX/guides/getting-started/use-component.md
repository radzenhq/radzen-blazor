# How to use a Radzen Blazor component
This article will show you how to add a Radzen Blazor component to a page, sets its properties and handle its events.

## Add component 
To add a Radzen Blazor component type its tag name in a `.razor` file e.g. `<RadzenButton>`. All Radzen components
start with a common prefix `Radzen` to make it easier for you to find in auto complete.
```
<RadzenButton />
```
## Set properties
To set a component property assign the corresponding attribute to the desired value.

### [Constant](#tab/constant)
```
<RadzenButton Text="Hello world!" />
```
### [Property or field](#tab/property)
```
<RadzenButton Text=@text />
@code {
    string text = "Hello World!";
}
```
***
## Handle events
To handle an event create a method and assign the corresponding attribute to the method name. 
```
<RadzenButton Text="Hello World!" Click=@OnButtonClick />
@code {
    void OnButtonClick()
    {

    }
}
```
## Common properties
The Radzen Blazor components share a few common properties that are often used.
### Style
The `Style` property is used to specify inline CSS settings. It renders as the `style` HTML attribute. Use it to set the width and height of a component.
```
<RadzenButton Style="width: 100px; height: 20px" />
```
### Visible
The `Visible` property is used to toggle a component. If it is set to `false` the component will not render.
```
<RadzenTextBox Visible=@visible />
<RadzenButton Text="Toggle" Click=@ToggleVisible />
@code {
   bool visible = true;
   void ToggleVisible()
   {
       visible = !visible;
   }
}
```
### Value
All Radzen input components (e.g. RadzenTextBox, RadzenDropDown, RadzenListBox) have a `Value` property. It is used to get and set the value of the component. It can also be used to data-bind the value to a property or field. Data-binding means that changing the component value (e.g. typing text) also updates a property or field. Use `@bind-Value` for this case.
```
<RadzenTextBox @bind-Value=@firstName />
@code {
    // The initial RadzenTextBox value is "Jane". Typing in RadzenTextBox will update firstName.
    string firstName = "Jane";
}
```
### Disabled
It is a common requirement to disable input components or buttons. The `Disabled` property allows that.
```
<RadzenButton Disabled="true" />
```
### Data
Some Radzen Blazor components display a collection of items. All of them have a `Data` property which must be set to that collection.
```
<RadzenDropDown Data=@items TextProperty="Name" />
@code {
    class DataItem
    {
        public string Name { get; set; }
        public int Value { get; set; }
    }

    IEnumerable<DataItem> items = new List<DataItem>()
    {
        new DataItem { Name = "Jane Doe", Value = 1},
        new DataItem { Name = "John Doe", Value = 2},
    };
}
```