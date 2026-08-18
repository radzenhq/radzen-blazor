# FileInput

The Blazor FileInput uploads a file as base64 with preview support, bound directly to your model.

Keywords: upload, form, edit

> API reference: [RadzenFileInput API](https://blazor.radzen.com/api/fileinput.md)

## Examples

## Blazor FileInput

The Blazor FileInput uploads a file as base64 with preview support, bound directly to your model.

```razor
@inherits DbContextPage

<RadzenStack AlignItems="AlignItems.Center">
    <RadzenCard class="rz-m-0 rz-m-md-12" Style="width: 100%; max-width: 600px;">
        <RadzenText TextStyle="TextStyle.H4" TagName="TagName.P">Employee: <strong>@(firstEmployee.FirstName + " " + firstEmployee.LastName)</strong></RadzenText>
        <RadzenFileInput @bind-Value=@firstEmployee.Photo @bind-FileName=@fileName @bind-FileSize=@fileSize TValue="string" Style="width: 100%" 
            Change=@(args => OnChange(args, "FileInput")) Error=@(args => OnError(args, "FileInput")) InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select file" }})"/>
    </RadzenCard>
</RadzenStack>

<EventConsole @ref=@console />

@code {
    Employee firstEmployee;
    EventConsole console;

    string fileName;
    long? fileSize;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        firstEmployee = await Task.FromResult(dbContext.Employees.FirstOrDefault());
    }

    void OnChange(string value, string name)
    {
        console.Log($"{name} value changed");
    }

    void OnError(UploadErrorEventArgs args, string name)
    {
        console.Log($"{args.Message}");
    }
}
```


### Byte Array Support

The FileInput component also supports the use of the byte[] type.

```razor
<RadzenStack AlignItems="AlignItems.Center">
    <RadzenCard class="rz-m-0 rz-m-md-12" Style="width: 100%; max-width: 600px;">
        <RadzenFileInput @bind-Value=@photo @bind-FileName=@fileName @bind-FileSize=@fileSize TValue="byte[]" Style="width: 100%" 
            Change=@(args => OnChange(args, "FileInput")) Error=@(args => OnError(args, "FileInput")) InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select file" }})"/>
    </RadzenCard>
</RadzenStack>

<EventConsole @ref=@console />

@code {
    EventConsole console;
    byte[] photo;

    string fileName;
    long? fileSize;

    void OnChange(byte[] value, string name)
    {
        console.Log($"{name} value changed");
    }

    void OnError(UploadErrorEventArgs args, string name)
    {
        console.Log($"{args.Message}");
    }
}
```
