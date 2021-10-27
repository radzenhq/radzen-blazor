# FileInput component
This article demonstrates how to use the FileInput component. The FileInput component is used to upload files as a part of a TemplateForm component. Files are uploaded as Data URI to be saved in a database table as base64 encoded string.

## Get and set the value
As all Radzen Blazor input components the FileInput has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenFileInput @bind-Value=@firstEmployee.Photo TValue="string" Change=@OnChange />
@code {
    Employee firstEmployee;

    protected override async Task OnInitializedAsync()
    {
        firstEmployee = await Task.FromResult(dbContext.Employees.FirstOrDefault());
    }

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```
