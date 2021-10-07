using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenFileInput<TValue> : FormComponent<TValue>
    {

        [Parameter]
        public string ChooseText { get; set; } = "Choose";

        [Parameter]
        public string Title { get; set; }

        ClassList ChooseClassList => ClassList.Create("rz-fileupload-choose rz-button rz-button-text-icon-left")
                                              .AddDisabled(Disabled);
        ClassList ButtonClassList => ClassList.Create("rz-button rz-button-icon-only")
                                              .AddDisabled(Disabled);

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-fileupload").ToString();
        }

        string name = "";
        string size = "";

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
                    return  $"{System.Text.Encoding.Default.GetString((byte[])(object)Value)}".StartsWith("data:image");
                }

                return false;
            }
        }

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

        [Parameter]
        public EventCallback<UploadErrorEventArgs> Error { get; set; }


        async System.Threading.Tasks.Task Remove(EventArgs args)
        {
            Value = default(TValue);

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);

            StateHasChanged();
        }

        [Parameter]
        public string Accept { get; set; } = "image/*";

        [Parameter]
        public int MaxFileSize { get; set; } = 5 * 1024 * 1024;
    
        [Parameter]
         public string ImageStyle { get; set; } = "width:100px;";
    }
}