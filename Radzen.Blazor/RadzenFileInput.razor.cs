using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

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
        ClassList ChooseClassList => ClassList.Create("rz-fileupload-choose rz-button btn-secondary")
                                              .AddDisabled(Disabled);
        /// <summary>
        /// Gets the button class list.
        /// </summary>
        /// <value>The button class list.</value>
        ClassList ButtonClassList => ClassList.Create("rz-button rz-button-icon-only btn-light")
                                              .AddDisabled(Disabled);

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-fileupload").ToString();
        }

        string name = "";
        string size = "";

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

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

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
    }
}
