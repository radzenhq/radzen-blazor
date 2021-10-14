using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenUpload.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenUpload : RadzenComponent
    {
        /// <summary>
        /// The file upload
        /// </summary>
        protected ElementReference fileUpload;

        /// <summary>
        /// The identifier
        /// </summary>
        string _Id;
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
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
        /// Gets or sets a value indicating whether this <see cref="RadzenUpload"/> is automatic.
        /// </summary>
        /// <value><c>true</c> if automatic; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Auto { get; set; } = true;

        /// <summary>
        /// Gets or sets the choose text.
        /// </summary>
        /// <value>The choose text.</value>
        [Parameter]
        public string ChooseText { get; set; } = "Choose";

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        [Parameter]
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the accept.
        /// </summary>
        /// <value>The accept.</value>
        [Parameter]
        public string Accept { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenUpload"/> is multiple.
        /// </summary>
        /// <value><c>true</c> if multiple; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Multiple { get; set; }

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Uploads this instance.
        /// </summary>
        public async Task Upload()
        {
            await JSRuntime.InvokeAsync<string>("Radzen.upload", fileUpload, Url, Multiple);
        }

        /// <summary>
        /// The headers
        /// </summary>
        IDictionary<string, string> headers = new Dictionary<string, string>();

        /// <summary>
        /// Adds the header.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        internal void AddHeader(string name, string value)
        {
            headers.Add(name, value);
        }

        /// <summary>
        /// Removes the header.
        /// </summary>
        /// <param name="name">The name.</param>
        internal void RemoveHeader(string name)
        {
            headers.Remove(name);
        }
        /// <summary>
        /// The visible changed
        /// </summary>
        private bool visibleChanged = false;
        /// <summary>
        /// The first render
        /// </summary>
        private bool firstRender = true;

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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

        /// <summary>
        /// On after render as an asynchronous operation.
        /// </summary>
        /// <param name="firstRender">if set to <c>true</c> [first render].</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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
        /// Gets or sets the change.
        /// </summary>
        /// <value>The change.</value>
        [Parameter]
        public EventCallback<UploadChangeEventArgs> Change { get; set; }

        /// <summary>
        /// Gets or sets the progress.
        /// </summary>
        /// <value>The progress.</value>
        [Parameter]
        public EventCallback<UploadProgressArgs> Progress { get; set; }

        /// <summary>
        /// Gets or sets the complete.
        /// </summary>
        /// <value>The complete.</value>
        [Parameter]
        public EventCallback<UploadCompleteEventArgs> Complete { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }

        /// <summary>
        /// The files
        /// </summary>
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
        /// Called when [remove].
        /// </summary>
        /// <param name="file">The file.</param>
        protected async System.Threading.Tasks.Task OnRemove(PreviewFileInfo file)
        {
            files.Remove(file);
            await JSRuntime.InvokeVoidAsync("Radzen.removeFileFromUpload", fileUpload, file.Name);
            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });
        }

        /// <summary>
        /// Called when [change].
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
        /// Called when [progress].
        /// </summary>
        /// <param name="progress">The progress.</param>
        /// <param name="loaded">The loaded.</param>
        /// <param name="total">The total.</param>
        /// <param name="files">The files.</param>
        [JSInvokable("RadzenUpload.OnProgress")]
        public async System.Threading.Tasks.Task OnProgress(int progress, int loaded, int total, IEnumerable<FileInfo> files)
        {
            await Progress.InvokeAsync(new UploadProgressArgs() { Progress = progress, Loaded = loaded, Total = total, Files = files });
        }

        /// <summary>
        /// Called when [complete].
        /// </summary>
        /// <param name="response">The response.</param>
        [JSInvokable("RadzenUpload.OnComplete")]
        public async System.Threading.Tasks.Task OnComplete(string response)
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

            await Complete.InvokeAsync(new UploadCompleteEventArgs() { RawResponse = response, JsonResponse = doc });
        }

        /// <summary>
        /// Called when [error].
        /// </summary>
        /// <param name="error">The error.</param>
        [JSInvokable("RadzenUpload.OnError")]
        public async System.Threading.Tasks.Task OnError(string error)
        {
            await Error.InvokeAsync(new UploadErrorEventArgs() { Message = error });
        }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-fileupload";
        }
    }
}