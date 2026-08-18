# Upload

The Blazor Upload component uploads single or multiple files to a server endpoint, with progress, validation, and custom headers.

Keywords: upload, file

> API reference: [RadzenUpload API](https://blazor.radzen.com/api/upload.md)

## Examples

## Blazor Upload

The Blazor Upload component uploads single or multiple files to a server endpoint, with drag-and-drop, progress tracking, validation, and custom headers.

### Upload files

To get uploaded files handle the Change event and use its event arguments.

```razor
<RadzenCard Variant="Variant.Outlined">
    <RadzenUpload Multiple="true" Change=@OnChange Style="width: 100%"
        InputAttributes="@(new Dictionary<string,object>{ { "aria-label", "select file" }})" />
</RadzenCard>
<EventConsole @ref=@console />

@code {
    EventConsole console;

    void OnChange(UploadChangeEventArgs args)
    {
        console.Log($"Files uploaded:");

        foreach (var file in args.Files)
        {
            console.Log($"File: {file.Name} / {file.Size} bytes");

            try
            {
                long maxFileSize = 10 * 1024 * 1024;
                // read file
                var stream = file.OpenReadStream(maxFileSize);
                stream.Close();
            }
            catch (Exception ex)
            {
                console.Log($"Client-side file read error: {ex.Message}");
            }
        }
    }
}
```


### Upload files to a server

To upload files to a server, set the `Url` property of the component. The URL should point to an action method in your server-side code that handles the file upload. By default, the upload uses the HTTP `POST` method and sends files as `multipart/form-data` (standard form upload). You can change this via the `Method` property to use a different HTTP method, or enable `Stream` to upload the file's raw data. When `Stream` is set to `true`, only single-file upload is supported.

```razor
<UploadUrl />
```


### Upload multiple files

To upload multiple files set the `Multiple` property of the component to `true`.

```razor
<UploadMultiple />
```


### Trigger upload from code

To trigger the upload from code set the `Auto` property of the component to `false`. The upload can be triggered by calling the `Upload()` method of the component.

```razor
<UploadFromCode />
```


### File filter

To filter the files that can be uploaded use the `Accept` property of the component. The Value should be a comma separated list [unique file type specifiers](https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes/accept#unique_file_type_specifiers)

```razor
<UploadImage />
```


### Use parameters

To send additional parameters with the upload request use the `Url` property of the component.

```razor
<UploadParameters />
```


### Show upload progress

This demo shows how to use the `RadzenProgressBar` component to show upload progress by handling the `Progress` event.

```razor
<UploadProgress />
```


### Drag and drop files to upload

This demo shows how to use the `RadzenUpload` component to allow users to drag and drop files for upload. The `ChooseText` property is used to set the text displayed when no files are selected.

```razor
<UploadDragDrop />
```


### Send custom HTTP headers

To send custom HTTP headers with the upload request use `RadzenUploadHeader`.

```razor
<UploadCustomHeaders />
```


### Specify action method parameter name

To specify the action method parameter name use the `ParameterName` property of the component. The value should be a string. By default the parameter name is `file` or `files` if the `Multiple` property is set to `true`.

```razor
<UploadParameterName />
```
