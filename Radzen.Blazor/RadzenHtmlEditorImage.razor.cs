using System;
using System.Globalization;
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
            public string? Src { get; set; }
            public string? Alt { get; set; }
            public string? Width { get; set; }
            public string? Height { get; set; }
        }

        private string? title;

        /// <summary>
        /// Specifies the title (tooltip) displayed when the user hovers the tool. Set to <c>"Insert image"</c> by default.
        /// </summary>
        [Parameter]
        public string Title { get => title ?? Localize(nameof(RadzenStrings.HtmlEditorImage_Title)); set => title = value; }

        private string? selectText;

        /// <summary>
        /// Specifies the text of the label suggesting the user to select a file for upload. Set to <c>"Select image file to upload"</c> by default.
        /// </summary>
        [Parameter]
        public string SelectText { get => selectText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_SelectText)); set => selectText = value; }

        private string? uploadChooseText;

        /// <summary>
        /// Specifies the text of the upload label. Set to <c>"Browse"</c> by default.
        /// </summary>
        [Parameter]
        public string UploadChooseText { get => uploadChooseText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_UploadChooseText)); set => uploadChooseText = value; }

        private string? urlText;

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a web address. Set to <c>"or enter a web address"</c> by default.
        /// </summary>
        [Parameter]
        public string UrlText { get => urlText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_UrlText)); set => urlText = value; }

        private string? altText;

        /// <summary>
        /// Specifies the text of the label suggesting the user to enter a alternative text (<c>alt</c>) for the image. Set to <c>"Alternative text"</c> by default.
        /// </summary>
        [Parameter]
        public string AltText { get => altText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_AltText)); set => altText = value; }

        private string? okText;

        /// <summary>
        /// Specifies the text of button which inserts the image. Set to <c>"OK"</c> by default.
        /// </summary>
        [Parameter]
        public string OkText { get => okText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_OkText)); set => okText = value; }

        private string? cancelText;

        /// <summary>
        /// Specifies the text of button which cancels image insertion and closes the dialog. Set to <c>"Cancel"</c> by default.
        /// </summary>
        [Parameter]
        public string CancelText { get => cancelText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_CancelText)); set => cancelText = value; }

        private string? widthText;

        /// <summary>
        /// Specifies the text of label for image width. Set to <c>"Image Width"</c> by default.
        /// </summary>
        [Parameter]
        public string WidthText { get => widthText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_WidthText)); set => widthText = value; }

        private string? heightText;

        /// <summary>
        /// Specifies the text of label for image height. Set to <c>"Image Height"</c> by default.
        /// </summary>
        [Parameter]
        public string HeightText { get => heightText ?? Localize(nameof(RadzenStrings.HtmlEditorImage_HeightText)); set => heightText = value; }

        /// <summary>
        /// Specifies whether to show the image width section. Set it to false to hide it. Default value is true.
        /// </summary>
        [Parameter]
        public bool ShowWidth { get; set; } = true;

        /// <summary>
        /// Specifies whether to show the image height section. Set it to false to hide it. Default value is true.
        /// </summary>
        [Parameter]
        public bool ShowHeight { get; set; } = true;

        /// <summary>
        /// Specifies whether to show the web address section. Set it to false to hide it. Default value is true.
        /// </summary>
        [Parameter]
        public bool ShowSrc { get; set; } = true;

        /// <summary>
        /// Specifies whether to show the alternative text section. Set it to false to hide it. Default value is true.
        /// </summary>
        [Parameter]
        public bool ShowAlt { get; set; } = true;

        ImageAttributes? Attributes { get; set; }
        RadzenUpload? FileUpload { get; set; }

        async Task OnSubmit()
        {
            if (FileUpload?.HasValue == true)
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
            if (args?.JsonResponse?.RootElement != null && args.JsonResponse.RootElement.TryGetProperty("url", out var property))
            {
                Attributes?.Src = property.GetString();
                await InsertHtml();
            }
            else
            {
                DialogService.Close(true);
            }

            if (Editor != null && args != null)
            {
                await Editor.RaiseUploadComplete(args);
            }
        }

        async Task OnUploadError(UploadErrorEventArgs args)
        {
            if (Editor != null && args != null && args.Message != null)
            {
                await Editor.OnError(args.Message);
            }
        }

        async Task InsertHtml()
        {
            DialogService.Close(true);

            if (Editor != null)
            {
                await Editor.RestoreSelectionAsync();
            }

            if (!string.IsNullOrEmpty(Attributes?.Src))
            {
                var html = new StringBuilder();
                html.AppendFormat(CultureInfo.InvariantCulture, "<img src=\"{0}\"", Attributes.Src);
                if (!string.IsNullOrEmpty(Attributes.Alt))
                {
                    html.AppendFormat(CultureInfo.InvariantCulture, " alt=\"{0}\"", Attributes.Alt);
                }
                if (!string.IsNullOrEmpty(Attributes.Width))
                {
                    html.AppendFormat(CultureInfo.InvariantCulture, " width=\"{0}\"", Attributes.Width);
                }
                if (!string.IsNullOrEmpty(Attributes.Height))
                {
                    html.AppendFormat(CultureInfo.InvariantCulture, " height=\"{0}\"", Attributes.Height);
                }
                html.AppendFormat(CultureInfo.InvariantCulture, ">");

                if (Editor != null)
                {
                    await Editor.ExecuteCommandAsync("insertHTML", html.ToString());
                }
            }
        }
    }
}
