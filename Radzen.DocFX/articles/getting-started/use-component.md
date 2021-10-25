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
<RadzenButton Text="Hello World!" Click=@OnButtonClick>
@code {
    void OnButtonClick()
    {

    }
}
```