using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    public enum TextStyle
    {
        DisplayH1,
        DisplayH2,
        DisplayH3,
        DisplayH4,
        DisplayH5,
        DisplayH6,
        H1,
        H2,
        H3,
        H4,
        H5,
        H6,
        Subtitle1,
        Subtitle2,
        Body1,
        Body2,
        Button,
        Caption,
        Overline
    }

    public enum TagName
    {
        Div,
        Span,
        P,
        H1, 
        H2, 
        H3, 
        H4, 
        H5, 
        H6,
        A,
        Button,
        Pre,
        Auto
    }
    /// <summary>
    /// A component which displays text or makup with predefined styling.
    /// </summary>
    /// <example>
    ///   <code>
    /// &lt;RadzenText TextStyle="TextStyle.H1"&gt;
    ///  Hello World
    /// &lt;/RadzenText&gt;
    ///   </code>
    /// </example>
    public class RadzenText : RadzenComponent
    {
        /// <summary>
        /// The text that will be displayed.
        /// </summary>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// The child content (markup) that will be displayed. Setting the <see cref="Text"/> property will override it.
        /// </summary>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// The style of the text. Set to <see cref="TextStyle.Body1"/> by default.
        /// </summary>
        [Parameter]
        public TextStyle TextStyle { get; set; } = TextStyle.Body1;

        /// <summary>
        /// The horozontal alignment of the text.
        /// </summary>
        [Parameter]
        public TextAlign TextAlign { get; set;} = TextAlign.Left;

        /// <summary>
        /// The tag name of the element that will be rendered. Set to <see cref="TagName.Auto"/> which uses a default tag name depending on the current <see cref="TextStyle" />.
        /// </summary>
        [Parameter]
        public TagName TagName { get; set; } = TagName.Auto;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            var tagName = "span";
            var className = "";
            var alignClassName = "";

            switch (TextStyle)
            {
                case TextStyle.DisplayH1:
                    tagName = "h1";
                    className = "rz-text-display-h1";
                    break;
                case TextStyle.DisplayH2:
                    tagName = "h2";
                    className = "rz-text-display-h2";
                    break;
                case TextStyle.DisplayH3:
                    tagName = "h3";
                    className = "rz-text-display-h3";
                    break;
                case TextStyle.DisplayH4:
                    tagName = "h4";
                    className = "rz-text-display-h4";
                    break;
                case TextStyle.DisplayH5:
                    tagName = "h5";
                    className = "rz-text-display-h5";
                    break;
                case TextStyle.DisplayH6:
                    tagName = "h6";
                    className = "rz-text-display-h6";
                    break;
                case TextStyle.H1:
                    tagName = "h1";
                    className = "rz-text-h1";
                    break;
                case TextStyle.H2:
                    tagName = "h2";
                    className = "rz-text-h2";
                    break;
                case TextStyle.H3:
                    tagName = "h3";
                    className = "rz-text-h3";
                    break;
                case TextStyle.H4:
                    tagName = "h4";
                    className = "rz-text-h4";
                    break;
                case TextStyle.H5:
                    tagName = "h5";
                    className = "rz-text-h5";
                    break;
                case TextStyle.H6:
                    tagName = "h6";
                    className = "rz-text-h6";
                    break;
                case TextStyle.Subtitle1:
                    tagName = "h6";
                    className = "rz-text-subtitle1";
                    break;
                case TextStyle.Subtitle2:
                    tagName = "h6";
                    className = "rz-text-subtitle2";
                    break;
                case TextStyle.Body1:
                    tagName = "p";
                    className = "rz-text-body1";
                    break;
                case TextStyle.Body2:
                    tagName = "p";
                    className = "rz-text-body2";
                    break;
                case TextStyle.Button:
                    tagName = "span";
                    className = "rz-text-button";
                    break;
                case TextStyle.Caption:
                    tagName = "span";
                    className = "rz-text-caption";
                    break;
                case TextStyle.Overline:
                    tagName = "span";
                    className = "rz-text-overline";
                    break;
            }

            switch (TextAlign)
            {
                case TextAlign.Center:
                    alignClassName = "rz-text-align-center";
                    break;
                case TextAlign.End:
                    alignClassName = "rz-text-align-end";
                    break;
                case TextAlign.Justify:
                    alignClassName = "rz-text-align-justify";
                    break;
                case TextAlign.Start:
                    alignClassName = "rz-text-align-start";
                    break;
                case TextAlign.Left:
                    alignClassName = "rz-text-align-left";
                    break;
                case TextAlign.Right:
                    alignClassName = "rz-text-align-right";
                    break;
                case TextAlign.JustifyAll:
                    alignClassName = "rz-text-align-justify-all";
                    break;
            }

            switch (TagName)
            {
                case TagName.Div:
                    tagName = "div";
                    break;
                case TagName.Span:
                    tagName = "span";
                    break;
                case TagName.P:
                    tagName = "p";
                    break;
                case TagName.H1:
                    tagName = "h1";
                    break;
                case TagName.H2:
                    tagName = "h2";
                    break;
                case TagName.H3:
                    tagName = "h3";
                    break;
                case TagName.H4:
                    tagName = "h4";
                    break;
                case TagName.H5:
                    tagName = "h5";
                    break;
                case TagName.H6:
                    tagName = "h6";
                    break;
                case TagName.A:
                    tagName = "a";
                    break;
                case TagName.Button:
                    tagName = "button";
                    break;
                case TagName.Pre:
                    tagName = "pre";
                    break;
            }

            var classList = ClassList.Create(className)
                                     .Add(Attributes)
                                     .Add(alignClassName, TextAlign != TextAlign.Left);

            if (Visible)
            {
                builder.OpenElement(0, tagName);
                builder.AddAttribute(1, "style", Style);
                builder.AddMultipleAttributes(2, Attributes);
                builder.AddAttribute(3, "class", classList.ToString());

                if (!string.IsNullOrEmpty(Text))
                {
                    builder.AddContent(4, Text);
                }
                else
                {
                    builder.AddContent(4, ChildContent);
                }
                builder.AddElementReferenceCapture(5, capture => Element = capture);
                builder.CloseElement();
            }
        }
    }
}