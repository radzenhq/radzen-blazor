using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenFileInput component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenFileInput @bind-Value=@employee.Photo TValue="string" Change=@(args => Console.WriteLine($"File content as base64 string: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenFileInput<TValue> : FormComponent<TValue>
    {
        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets the choose button text.
        /// </summary>
        /// <value>The choose button text.</value>
        [Parameter]
        public string ChooseText { get; set; } = "Choose";

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        [Parameter]
        public string Title { get; set; }

        /// <summary>
        /// Gets the choose class list.
        /// </summary>
        /// <value>The choose class list.</value>
        ClassList ChooseClassList => ClassList.Create("rz-fileupload-choose rz-button rz-secondary")
                                              .AddDisabled(Disabled);
        /// <summary>
        /// Gets the button class list.
        /// </summary>
        /// <value>The button class list.</value>
        ClassList ButtonClassList => ClassList.Create("rz-button rz-button-icon-only rz-light")
                                              .AddDisabled(Disabled);

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-fileupload").ToString();
        }

        /// <summary>
        /// Gets file input reference.
        /// </summary>
        protected ElementReference fileUpload;

        private bool IsImage
        {
            get
            {
                if (Value == null)
                {
                    return false;
                }
                else if (Value is string)
                {
                    return $"{Value}".StartsWith("data:image");
                }
                else if (Value is byte[])
                {
                    return $"{System.Text.Encoding.Default.GetString((byte[])(object)Value)}".StartsWith("data:image");
                }

                return false;
            }
        }

        async Task OnChange()
        {
            string uploadValue;

            try
            {
                uploadValue = await JSRuntime.InvokeAsync<string>("Radzen.readFileAsBase64", fileUpload, MaxFileSize, MaxWidth, MaxHeight);

                if (typeof(TValue) == typeof(byte[]))
                {
                    Value = (TValue)(object)System.Text.Encoding.Default.GetBytes($"{uploadValue}");
                }
                else if (typeof(TValue) == typeof(string))
                {
                    Value = (TValue)(object)uploadValue;
                }

                await ValueChanged.InvokeAsync(Value);
                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
                await Change.InvokeAsync(Value);

                StateHasChanged();
            }
            catch (Exception ex)
            {
                await Error.InvokeAsync(new UploadErrorEventArgs() { Message = $"Unable to read file as base64 string. {ex.Message}" });
            }
        }

        /// <summary>
        /// Called on file change.
        /// </summary>
        /// <param name="files">The file.</param>
        [JSInvokable("RadzenUpload.OnChange")]
        public async System.Threading.Tasks.Task OnChange(IEnumerable<PreviewFileInfo> files)
        {
            var file = files.FirstOrDefault();

            FileSize = file.Size;
            await FileSizeChanged.InvokeAsync(FileSize);

            FileName = file.Name;
            await FileNameChanged.InvokeAsync(FileName);

            await OnChange();
        }

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

        private bool visibleChanged = false;

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender || visibleChanged)
            {
                visibleChanged = false;

                if (Visible)
                {
                    await JSRuntime.InvokeVoidAsync("Radzen.uploads", Reference, Id);
                }
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            visibleChanged = parameters.DidParameterChange(nameof(Visible), Visible);

            await base.SetParametersAsync(parameters);
        }

        /// <summary>
        /// Gets or sets the error callback.
        /// </summary>
        /// <value>The error callback.</value>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }

        /// <summary>
        /// Gets or sets the image click callback.
        /// </summary>
        /// <value>The image click callback.</value>
        [Parameter]
        public EventCallback<MouseEventArgs> ImageClick { get; set; }

        bool clicking;
        /// <summary>
        /// Handles the <see cref="E:ImageClick" /> event.
        /// </summary>
        /// <param name="args">The <see cref="MouseEventArgs"/> instance containing the event data.</param>
        public async Task OnImageClick(MouseEventArgs args)
        {
            if (clicking)
            {
                return;
            }

            try
            {
                clicking = true;

                await ImageClick.InvokeAsync(args);
            }
            finally
            {
                clicking = false;
            }
        }

        async System.Threading.Tasks.Task Remove(EventArgs args)
        {
            Value = default(TValue);
            FileSize = null;
            FileName = null;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            await FileSizeChanged.InvokeAsync(FileSize);

            await FileNameChanged.InvokeAsync(FileName);

            await JSRuntime.InvokeVoidAsync("Radzen.removeFileFromFileInput", fileUpload);

            StateHasChanged();
        }

        /// <summary>
        /// Gets or sets the comma-separated accepted MIME types.
        /// </summary>
        /// <value>The comma-separated accepted MIME types.</value>
        [Parameter]
        public string Accept { get; set; } = "image/*";

        /// <summary>
        /// Gets or sets the maximum size of the file.
        /// </summary>
        /// <value>The maximum size of the file.</value>
        [Parameter]
        public int MaxFileSize { get; set; } = 5 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the maximum width of the file, keeping aspect ratio.
        /// </summary>
        /// <value>The maximum width of the file.</value>
        [Parameter]
        public int MaxWidth { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum height of the file, keeping aspect ratio.
        /// </summary>
        /// <value>The maximum height of the file.</value>
        [Parameter]
        public int MaxHeight { get; set; } = 0;

        /// <summary>
        /// Gets or sets the image style.
        /// </summary>
        /// <value>The image style.</value>
        [Parameter]
        public string ImageStyle { get; set; } = "width:100px;";

        /// <summary>
        /// Gets or sets the image file name.
        /// </summary>
        /// <value>The image file name.</value>
        [Parameter]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the FileName changed.
        /// </summary>
        /// <value>The FileName changed.</value>
        [Parameter]
        public EventCallback<string> FileNameChanged { get; set; }

        /// <summary>
        /// Gets or sets the image file size.
        /// </summary>
        /// <value>The image file size.</value>
        [Parameter]
        public long? FileSize { get; set; }

        /// <summary>
        /// Gets or sets the FileSize changed.
        /// </summary>
        /// <value>The FileSize changed.</value>
        [Parameter]
        public EventCallback<long?> FileSizeChanged { get; set; }
    }
}
