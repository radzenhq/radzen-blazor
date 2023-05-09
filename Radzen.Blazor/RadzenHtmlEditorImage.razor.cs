using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A tool which inserts and uploads images in a <see cref="RadzenHtmlEditor" />.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenHtmlEditor @bind-Value=@html&gt;
    ///  &lt;RadzenHtmlEditorImage /&gt;
    /// &lt;/RadzenHtmlEdito&gt;
    /// @code {
    ///   string html = "@lt;strong&gt;Hello&lt;/strong&gt; world!";
    /// }
    /// </code>
    /// </example>
    public partial class RadzenHtmlEditorImage : RadzenHtmlEditorButtonBase
    {
        class ImageAttributes
        {
            public string Src { get; set; }
            public string Alt { get; set; }
            public string Width { get; set; }
            public string Height { get; set; }
        }

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Insert image"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get; set; } = "Insert image";

        /// <summary>
        /// Specifies the text of the label suggesting the user to select a file for upload. Set to <c>"Select image file to upload"</c> by default.
        /// </summary>
        [Parameter]
        public string SelectText { get; set; } = "Select image file to upload";

        /// <summary>
        /// Specifies the text of the upload label. Set to <c>"Browse"</c> by default.
        /// </summary>
        [Parameter]
        public string UploadChooseText { get; set; } = "Browse";

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a web address. Set to <c>"or enter a web address"</c> by default.
        /// </summary>
        [Parameter]
        public string UrlText { get; set; } = "or enter a web address";

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a alternative text (<c>alt</c>) for the image. Set to <c>"Alternative text"</c> by default.
        /// </summary>
        [Parameter]
        public string AltText { get; set; } = "Alternative text";

        /// <summary>
        /// Specifies the text of button which inserts the image. Set to <c>"OK"</c> by default.
        /// </summary>
        [Parameter]
        public string OkText { get; set; } = "OK";

        /// <summary>
        /// Specifies the text of button which cancels image insertion and closes the dialog. Set to <c>"Cancel"</c> by default.
        /// </summary>
        [Parameter]
        public string CancelText { get; set; } = "Cancel";

        /// <summary>
        /// Specifies the text of label for image width. Set to <c>"Image Width"</c> by default.
        /// </summary>
        [Parameter]
        public string WidthText { get; set; } = "Image Width";

        /// <summary>
        /// Specifies the text of label for image height. Set to <c>"Image Height"</c> by default.
        /// </summary>
        [Parameter]
        public string HeightText { get; set; } = "Image Height";

        ImageAttributes Attributes { get; set; }
        RadzenUpload FileUpload { get; set; }

        async Task OnSubmit()
        {
            if (FileUpload.HasValue)
            {
                await FileUpload.Upload();
            }
            else
            {
                await InsertHtml();
            }
        }

        async Task OnUploadComplete(UploadCompleteEventArgs args)
        {
            if (args.JsonResponse.RootElement.TryGetProperty("url", out var property))
            {
                Attributes.Src = property.GetString();
                await InsertHtml();
            }
            else
            {
                DialogService.Close(true);
            }
        }

        async Task OnUploadError(UploadErrorEventArgs args)
        {
            await Editor.OnError(args.Message);
        }

        async Task InsertHtml()
        {
            DialogService.Close(true);

            await Editor.RestoreSelectionAsync();

            if (!string.IsNullOrEmpty(Attributes.Src))
            {
                var html = new StringBuilder();
                html.AppendFormat("<img src=\"{0}\"", Attributes.Src);
                if (!string.IsNullOrEmpty(Attributes.Alt))
                {
                    html.AppendFormat(" alt=\"{0}\"", Attributes.Alt);
                }
                if (!string.IsNullOrEmpty(Attributes.Width))
                {
                    html.AppendFormat(" width=\"{0}\"", Attributes.Width);
                }
                if (!string.IsNullOrEmpty(Attributes.Height))
                {
                    html.AppendFormat(" height=\"{0}\"", Attributes.Height);
                }
                html.AppendFormat(">");

                await Editor.ExecuteCommandAsync("insertHTML", html.ToString());
            }
        }
    }
}
