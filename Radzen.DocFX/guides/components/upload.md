# Upload component
This article demonstrates how to use the Upload component.

Single file upload

```
<RadzenUpload Url="upload/single" Progress="@((args) => OnProgress(args, "Single file upload"))" />
```

Multiple files upload
```
<RadzenUpload Multiple="true" Url="upload/multiple" Progress="@((args) => OnProgress(args, "Multiple files upload"))" />
```

Upload images only
```
<RadzenUpload Multiple="true" Accept="image/*" Url="upload/multiple" Progress="@((args) => OnProgress(args, "Images only upload"))" />
```

Upload with additional parameter
```
<RadzenUpload Multiple="true" Accept="image/*" Url=@($"upload/{1}") Progress="@((args) => OnProgress(args, "Upload with additional parameter"))" />

@code {
    int progress;
    string info;

    void OnProgress(UploadProgressArgs args, string name)
    {
        this.info = $"% '{name}' / {args.Loaded} of {args.Total} bytes.";
        this.progress = args.Progress;
    }
}
```

The upload component allows you to choose single or multiple files and will initiate immediately `POST` request to specified `Url`. You can filter file types using `Accept` property (for example images only: `Accept="image/*"`).

## <a id="controller">Example upload controller implementation</a>

```cs
public partial class UploadController : Controller
{
    [HttpPost("upload/single")]
    public IActionResult Single(IFormFile file)
    {
        try
        {
            // Put your code here
            return StatusCode(200);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("upload/multiple")]
    public IActionResult Multiple(IFormFile[] files)
    {
        try
        {
            // Put your code here
            return StatusCode(200);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    [HttpPost("upload/{id}")]
    public IActionResult Post(IFormFile[] files, int id)
    {
        try
        {
            // Put your code here
            return StatusCode(200);
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }
}
```

> [!Warning]
> When uploading single file the argument name of the controller method should be `file`, when uploading multiple the argument name should be `files.`