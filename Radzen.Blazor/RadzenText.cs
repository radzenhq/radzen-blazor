using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// The display style of the text. It overrides the tag name and provides predefined styling.
    /// </summary>
    public enum TextStyle
    {
        /// <summary>Display as largest header.</summary>
        DisplayH1,
        /// <summary>Display as second largest header.</summary>
        DisplayH2,
        /// <summary>Display as third largest header.</summary>
        DisplayH3,
        /// <summary>Display as fourth largest header.</summary>
        DisplayH4,
        /// <summary>Display as fifth largest header.</summary>
        DisplayH5,
        /// <summary>Display as sixth largest header.</summary>
        DisplayH6,
        /// <summary>Display as H1 element.</summary>
        H1,
        /// <summary>Display as H2 element.</summary>
        H2,
        /// <summary>Display as H3 element.</summary>
        H3,
        /// <summary>Display as H4 element.</summary>
        H4,
        /// <summary>Display as H5 element.</summary>
        H5,
        /// <summary>Display as H6 element.</summary>
        H6,
        /// <summary>Display as subtitle.</summary>
        Subtitle1,
        /// <summary>Display as a smaller subtitle.</summary>
        Subtitle2,
        /// <summary>Display as a paragraph.</summary>
        Body1,
        /// <summary>Display as a smaller paragraph.</summary>
        Body2,
        /// <summary>Display as button text.</summary>
        Button,
        /// <summary>Display as a caption.</summary>
        Caption,
        /// <summary>Display as overline.</summary>
        Overline
    }

    /// <summary>
    /// The tag name of the element that will be rendered.
    /// </summary>

    public enum TagName
    {
        /// <summary>Use &lt;div&gt; to render the text.</summary>
        Div,
        /// <summary>Use &lt;span&gt; to render the text.</summary>
        Span,
        /// <summary>Use &lt;p&gt; to render the text.</summary>
        P,
        /// <summary>Use &lt;h1&gt; to render the text.</summary>
        H1,
        /// <summary>Use &lt;h2&gt; to render the text.</summary>
        H2,
        /// <summary>Use &lt;h3&gt; to render the text.</summary>
        H3,
        /// <summary>Use &lt;h4&gt; to render the text.</summary>
        H4,
        /// <summary>Use &lt;h5&gt; to render the text.</summary>
        H5,
        /// <summary>Use &lt;h6&gt; to render the text.</summary>
        H6,
        /// <summary>Use &lt;a&gt; to render the text.</summary>
        A,
        /// <summary>Use &lt;button&gt; to render the text.</summary>
        Button,
        /// <summary>Use &lt;pre&gt; to render the text.</summary>
        Pre,
        /// <summary>The tag name will be determined depending on the TextStyle.</summary>
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
        class RadzenTextAnchor : ComponentBase, IDisposable
        {
            [Inject]
            NavigationManager NavigationManager { get; set; }

            [Inject]
            IJSRuntime JSRuntime { get; set; }

            [Parameter]
            public string Path { get; set; }

            private string GetAnchor()
            {
                var fragments = Path.Split('#');

                return fragments.Length > 1 ? fragments[1] : fragments[0];
            }

            private ElementReference element;

            protected override void OnInitialized()
            {
                NavigationManager.LocationChanged += OnLocationChanged;
            }

            void OnLocationChanged(object sender, LocationChangedEventArgs e)
            {
                if (e.Location.EndsWith(GetAnchor(), StringComparison.InvariantCultureIgnoreCase))
                {
                    JSRuntime.InvokeVoidAsync("Element.prototype.scrollIntoView.call", element);
                }
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(1, "a");
                builder.AddAttribute(2, "name", GetAnchor());
                builder.AddAttribute(3, "href", Path);
                builder.AddAttribute(4, "class", "rz-link");
                builder.AddAttribute(5, "target", "_top"); // To support relative links without the Blazor router interfering
                builder.AddElementReferenceCapture(6, capture => element = capture);
                builder.OpenComponent<RadzenIcon>(7);
                builder.AddAttribute(8, "Icon", "link");
                builder.CloseComponent();

                builder.CloseElement();
            }

            void IDisposable.Dispose()
            {
                NavigationManager.LocationChanged -= OnLocationChanged;
            }
        }

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

        /// <summary>
        /// Gets or sets the anchor name. If set an additional anchor will be rendered. Clicking on the anchor will scroll the page to the element with the same id.
        /// </summary>
        [Parameter]
        public string Anchor { get; set; }

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
                builder.AddAttribute(4, "id", GetId());

                if (!string.IsNullOrEmpty(Text))
                {
                    builder.AddContent(5, Text);
                }
                else
                {
                    builder.AddContent(5, ChildContent);
                }

                if (!string.IsNullOrEmpty(Anchor))
                {
                    builder.OpenComponent<RadzenTextAnchor>(6);
                    builder.AddAttribute(7, nameof(RadzenTextAnchor.Path), Anchor);
                    builder.CloseComponent();
                }
                builder.AddElementReferenceCapture(8, capture => Element = capture);
                builder.CloseElement();
            }
        }
    }
}