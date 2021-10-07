using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenFileInput.
    /// Implements the <see cref="Radzen.FormComponent{TValue}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{TValue}" />
    public partial class RadzenFileInput<TValue> : FormComponent<TValue>
    {

        /// <summary>
        /// Gets or sets the choose text.
        /// </summary>
        /// <value>The choose text.</value>
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
        ClassList ChooseClassList => ClassList.Create("rz-fileupload-choose rz-button rz-button-text-icon-left")
                                              .AddDisabled(Disabled);
        /// <summary>
        /// Gets the button class list.
        /// </summary>
        /// <value>The button class list.</value>
        ClassList ButtonClassList => ClassList.Create("rz-button rz-button-icon-only")
                                              .AddDisabled(Disabled);

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-fileupload").ToString();
        }

        /// <summary>
        /// The name
        /// </summary>
        string name = "";
        /// <summary>
        /// The size
        /// </summary>
        string size = "";

        /// <summary>
        /// The file upload
        /// </summary>
        protected ElementReference fileUpload;

        /// <summary>
        /// Gets a value indicating whether this instance is image.
        /// </summary>
        /// <value><c>true</c> if this instance is image; otherwise, <c>false</c>.</value>
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
                    return  $"{System.Text.Encoding.Default.GetString((byte[])(object)Value)}".StartsWith("data:image");
                }

                return false;
            }
        }

        /// <summary>
        /// Called when [change].
        /// </summary>
        async Task OnChange()
        {
            string uploadValue;

            try
            {
                uploadValue = await JSRuntime.InvokeAsync<string>("Radzen.readFileAsBase64", fileUpload, MaxFileSize);

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
            catch(Exception ex)
            {
                await Error.InvokeAsync(new UploadErrorEventArgs() { Message = $"Unable to read file as base64 string. {ex.Message}" });
            }
        }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }


        /// <summary>
        /// Removes the specified arguments.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs"/> instance containing the event data.</param>
        async System.Threading.Tasks.Task Remove(EventArgs args)
        {
            Value = default(TValue);

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        /// <summary>
        /// Gets or sets the accept.
        /// </summary>
        /// <value>The accept.</value>
        [Parameter]
        public string Accept { get; set; } = "image/*";

        /// <summary>
        /// Gets or sets the maximum size of the file.
        /// </summary>
        /// <value>The maximum size of the file.</value>
        [Parameter]
        public int MaxFileSize { get; set; } = 5 * 1024 * 1024;

        /// <summary>
        /// Gets or sets the image style.
        /// </summary>
        /// <value>The image style.</value>
        [Parameter]
         public string ImageStyle { get; set; } = "width:100px;";
    }
}