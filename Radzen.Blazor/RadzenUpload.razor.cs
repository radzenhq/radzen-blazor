using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenUpload component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenUpload Url="upload/single" Progress=@(args => OnProgress(args, "Single file upload"))/&gt;
    /// @code {
    ///  void OnProgress(UploadProgressArgs args, string name)
    ///  {
    ///    this.info = $"% '{name}' / {args.Loaded} of {args.Total} bytes.";
    ///    this.progress = args.Progress;
    ///    if (args.Progress == 100)
    ///    {
    ///        console.Clear();
    ///
    ///        foreach (var file in args.Files)
    ///        {
    ///            console.Log($"Uploaded: {file.Name} / {file.Size} bytes");
    ///        }
    ///    }
    ///  }
    /// }
    /// </code>
    /// </example>
    public partial class RadzenUpload : RadzenComponent
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets file input reference.
        /// </summary>
        protected ElementReference fileUpload;
        string _Id;
        string Id
        {
            get
            {
                if (_Id == null)
                {
                    _Id = $"{Guid.NewGuid()}";
                }

                return _Id;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenUpload"/> upload is automatic.
        /// </summary>
        /// <value><c>true</c> if upload automatic; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Auto { get; set; } = true;

        /// <summary>
        /// Gets or sets the choose button text.
        /// </summary>
        /// <value>The choose button text.</value>
        [Parameter]
        public string ChooseText { get; set; } = "Choose";

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        [Parameter]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the parameter name. If not set 'file' parameter name will be used for single file and 'files' for multiple files.
        /// </summary>
        /// <value>The parameter name.</value>
        [Parameter]
        public string ParameterName { get; set; }

        /// <summary>
        /// Gets or sets the accepted MIME types.
        /// </summary>
        /// <value>The accepted MIME types.</value>
        [Parameter]
        public string Accept { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenUpload"/> is multiple.
        /// </summary>
        /// <value><c>true</c> if multiple; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenUpload"/> is disabled.
        /// </summary>
        /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets the choose class list.
        /// </summary>
        /// <value>The choose class list.</value>
        ClassList ChooseClassList => ClassList.Create("rz-fileupload-choose rz-button")
                                              .AddDisabled(Disabled);

        /// <summary>
        /// Gets the button class list.
        /// </summary>
        /// <value>The button class list.</value>
        ClassList ButtonClassList => ClassList.Create("rz-button rz-button-icon-only rz-light")
                                              .AddDisabled(Disabled);

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Uploads this instance selected files.
        /// </summary>
        public async Task Upload()
        {
            await JSRuntime.InvokeAsync<string>("Radzen.upload", fileUpload, Url, Multiple, false, ParameterName);
        }

        readonly IDictionary<string, string> headers = new Dictionary<string, string>();

        internal void AddHeader(string name, string value)
        {
            if (name != null)
            {
                headers.Add(name, value);
            }
        }

        internal void RemoveHeader(string name)
        {
            if (name != null)
            {
                headers.Remove(name);
            }
        }

        private bool visibleChanged = false;
        private bool firstRender = true;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);

            if (visibleChanged && !firstRender)
            {
                if (Visible == false)
                {
                    Dispose();
                }
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            this.firstRender = firstRender;

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.uploads", Reference, Id);
                }
            }
        }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <returns>IDictionary&lt;System.String, System.String&gt;.</returns>
        [JSInvokable("GetHeaders")]
        public IDictionary<string, string> GetHeaders()
        {
            return headers;
        }

        /// <summary>
        /// Gets or sets the change callback.
        /// </summary>
        /// <value>The change callback.</value>
        [Parameter]
        public EventCallback<UploadChangeEventArgs> Change { get; set; }

        /// <summary>
        /// Gets or sets the progress callback.
        /// </summary>
        /// <value>The progress callback.</value>
        [Parameter]
        public EventCallback<UploadProgressArgs> Progress { get; set; }

        /// <summary>
        /// Gets or sets the complete callback.
        /// </summary>
        /// <value>The complete callback.</value>
        [Parameter]
        public EventCallback<UploadCompleteEventArgs> Complete { get; set; }

        /// <summary>
        /// Gets or sets the error callback.
        /// </summary>
        /// <value>The error callback.</value>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }

        List<PreviewFileInfo> files = Enumerable.Empty<PreviewFileInfo>().ToList();

        /// <summary>
        /// Gets a value indicating whether this instance has value.
        /// </summary>
        /// <value><c>true</c> if this instance has value; otherwise, <c>false</c>.</value>
        public bool HasValue
        {
            get
            {
                return files.Any();
            }
        }

        /// <summary>
        /// Clear selected file(s) from the upload selection
        /// </summary>
        public async System.Threading.Tasks.Task ClearFiles()
        {
            while(files.Count > 0)
            {
                await OnRemove(files[0], false);
            }

            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });
        }

        /// <summary>
        /// Called on file remove.
        /// </summary>
        /// <param name="fileName">The name of the file to remove.</param>
        /// <param name="ignoreCase">Specify true is file name casing should be ignored (default: false)</param>
        public async System.Threading.Tasks.Task RemoveFile(string fileName, bool ignoreCase = false)
        {
            var comparisonMethod = ignoreCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var fileInfo = files.FirstOrDefault(f => string.Equals(f.Name, fileName, comparisonMethod));
            if (fileInfo != null)
            {
                await OnRemove(fileInfo);
            }
        }

        /// <summary>
        /// Called on file remove.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="fireChangeEvent">If the linked <see cref="Change" /> event should be fired as a result of this removal (default: true)</param>
        protected async System.Threading.Tasks.Task OnRemove(PreviewFileInfo file, bool fireChangeEvent = true)
        {
            files.Remove(file);
            await JSRuntime.InvokeVoidAsync("Radzen.removeFileFromUpload", fileUpload, file.Name);
            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });
        }

        /// <summary>
        /// Called on file change.
        /// </summary>
        /// <param name="files">The files.</param>
        [JSInvokable("RadzenUpload.OnChange")]
        public async System.Threading.Tasks.Task OnChange(IEnumerable<PreviewFileInfo> files)
        {
            this.files = files.ToList();

            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });

            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Called on progress.
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="loaded">The loaded.</param>
        /// <param name="total">The total.</param>
        /// <param name="files">The files.</param>
        /// <param name="cancel">The cancelled state.</param>
        [JSInvokable("RadzenUpload.OnProgress")]
        public async System.Threading.Tasks.Task<bool> OnProgress(int progress, long loaded, long total, IEnumerable<FileInfo> files, bool cancel)
        {
            var args = new UploadProgressArgs() { Progress = progress, Loaded = loaded, Total = total, Files = files, Cancel = cancel };
            await Progress.InvokeAsync(args); ;

            return args.Cancel;
        }

        /// <summary>
        /// Called when upload is complete.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cancelled">Flag indicating if the upload was cancelled</param>
        [JSInvokable("RadzenUpload.OnComplete")]
        public async System.Threading.Tasks.Task OnComplete(string response, bool cancelled)
        {
            System.Text.Json.JsonDocument doc = null;

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    doc = System.Text.Json.JsonDocument.Parse(response);
                }
                catch (System.Text.Json.JsonException)
                {
                    //
                }
            }

            await Complete.InvokeAsync(new UploadCompleteEventArgs() { RawResponse = response, JsonResponse = doc, Cancelled = cancelled });
        }

        /// <summary>
        /// Called on upload error.
        /// </summary>
        /// <param name="error">The error.</param>
        [JSInvokable("RadzenUpload.OnError")]
        public async System.Threading.Tasks.Task OnError(string error)
        {
            await Error.InvokeAsync(new UploadErrorEventArgs() { Message = error });
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-fileupload";
        }
    }
}
