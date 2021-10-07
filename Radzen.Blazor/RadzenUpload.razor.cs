using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenUpload : RadzenComponent
    {
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

        [Parameter]
        public bool Auto { get; set; } = true;

        [Parameter]
        public string ChooseText { get; set; } = "Choose";

        [Parameter]
        public string Url { get; set; }

        [Parameter]
        public string Accept { get; set; }

        [Parameter]
        public bool Multiple { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        public async Task Upload()
        {
            await JSRuntime.InvokeAsync<string>("Radzen.upload", fileUpload, Url, Multiple);
        }

        IDictionary<string, string> headers = new Dictionary<string, string>();

        internal void AddHeader(string name, string value)
        {
            headers.Add(name, value);
        }

        internal void RemoveHeader(string name)
        {
            headers.Remove(name);
        }
        private bool visibleChanged = false;
        private bool firstRender = true;

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

        [JSInvokable("GetHeaders")]
        public IDictionary<string, string> GetHeaders()
        {
            return headers;
        }

        [Parameter]
        public EventCallback<UploadChangeEventArgs> Change { get; set; }

        [Parameter]
        public EventCallback<UploadProgressArgs> Progress { get; set; }

        [Parameter]
        public EventCallback<UploadCompleteEventArgs> Complete { get; set; }

        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }

        List<PreviewFileInfo> files = Enumerable.Empty<PreviewFileInfo>().ToList();

        public bool HasValue
        {
            get
            {
                return files.Any();
            }
        }

        protected async System.Threading.Tasks.Task OnRemove(PreviewFileInfo file)
        {
            files.Remove(file);
            await JSRuntime.InvokeVoidAsync("Radzen.removeFileFromUpload", fileUpload, file.Name);
            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });
        }

        [JSInvokable("RadzenUpload.OnChange")]
        public async System.Threading.Tasks.Task OnChange(IEnumerable<PreviewFileInfo> files)
        {
            this.files = files.ToList();

            await Change.InvokeAsync(new UploadChangeEventArgs() { Files = files.Select(f => new FileInfo() { Name = f.Name, Size = f.Size }).ToList() });

            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable("RadzenUpload.OnProgress")]
        public async System.Threading.Tasks.Task OnProgress(int progress, int loaded, int total, IEnumerable<FileInfo> files)
        {
            await Progress.InvokeAsync(new UploadProgressArgs() { Progress = progress, Loaded = loaded, Total = total, Files = files });
        }

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

        [JSInvokable("RadzenUpload.OnError")]
        public async System.Threading.Tasks.Task OnError(string error)
        {
            await Error.InvokeAsync(new UploadErrorEventArgs() { Message = error });
        }

        protected override string GetComponentCssClass()
        {
            return "rz-fileupload";
        }
    }
}