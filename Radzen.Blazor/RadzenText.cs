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
    /// A text display component with predefined typography styles matching Material Design text hierarchy.
    /// RadzenText provides consistent text formatting for headings, subtitles, body text, captions, and more with semantic HTML tags.
    /// Simplifies typography by providing preset styles that match your theme's design system, allowing consistent, professionally designed text formatting without managing CSS classes manually.
    /// Supports text styles (Display headings H1-H6, subtitles, body text, captions, button text, overlines), automatically uses appropriate HTML tags (h1-h6, p, span) based on style,
    /// manual HTML tag specification via TagName property, built-in text alignment (left, right, center, justify), and optional anchor links for heading navigation.
    /// TextStyle.DisplayH1-H6 provide large display headings, TextStyle.H1-H6 provide standard headings, Subtitle1/2 for subtitles, Body1/2 for paragraphs, Caption for small text, and Overline for labels.
    /// </summary>
    /// <example>
    /// Heading text:
    /// <code>
    /// &lt;RadzenText TextStyle="TextStyle.H3"&gt;Welcome to Our Application&lt;/RadzenText&gt;
    /// </code>
    /// Subtitle with alignment:
    /// <code>
    /// &lt;RadzenText TextStyle="TextStyle.Subtitle1" TextAlign="TextAlign.Center"&gt;
    ///     This is a centered subtitle
    /// &lt;/RadzenText&gt;
    /// </code>
    /// Body text with custom tag:
    /// <code>
    /// &lt;RadzenText TextStyle="TextStyle.Body1" TagName="TagName.Div"&gt;
    ///     This is body text rendered as a div element.
    /// &lt;/RadzenText&gt;
    /// </code>
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

            private string GetPath()
            {
                var uri = new Uri(NavigationManager.Uri);

                var anchor = GetAnchor();

                return $"{uri.PathAndQuery}#{anchor}";
            }

            protected override void BuildRenderTree(RenderTreeBuilder builder)
            {
                builder.OpenElement(1, "a");
                builder.AddAttribute(2, "id", GetAnchor());
                builder.AddAttribute(3, "href", GetPath());
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
        /// Gets or sets the plain text content to display.
        /// For simple text display, use this property. For rich content with markup, use <see cref="ChildContent"/> instead.
        /// When set, takes precedence over ChildContent.
        /// </summary>
        /// <value>The text content to display.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the child content (markup) to display.
        /// Use this for rich content with HTML or other Blazor components. Overridden by <see cref="Text"/> if both are set.
        /// </summary>
        /// <value>The child content render fragment.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the typography style that determines the text size, weight, and appearance.
        /// Options include display headings (DisplayH1-H6), standard headings (H1-H6), subtitles, body text, captions, and more.
        /// Each style provides consistent formatting matching the theme's design system.
        /// </summary>
        /// <value>The text style. Default is <see cref="TextStyle.Body1"/>.</value>
        [Parameter]
        public TextStyle TextStyle { get; set; } = TextStyle.Body1;

        /// <summary>
        /// Gets or sets the horizontal text alignment within the container.
        /// Options include Left, Right, Center, Justify, Start, End, and JustifyAll.
        /// </summary>
        /// <value>The text alignment. Default is <see cref="TextAlign.Left"/>.</value>
        [Parameter]
        public TextAlign TextAlign { get; set;} = TextAlign.Left;

        /// <summary>
        /// Gets or sets the HTML element tag to render.
        /// When set to Auto (default), the tag is chosen automatically based on <see cref="TextStyle"/>
        /// (e.g., H1 style uses &lt;h1&gt; tag). Override to use a specific tag regardless of style.
        /// </summary>
        /// <value>The HTML tag name. Default is <see cref="TagName.Auto"/>.</value>
        [Parameter]
        public TagName TagName { get; set; } = TagName.Auto;

        /// <summary>
        /// Gets or sets an anchor identifier for creating linkable headings.
        /// When set, adds a clickable link icon next to the text that scrolls to this element when clicked.
        /// The anchor href will be the current page URL with #anchorname appended.
        /// </summary>
        /// <value>The anchor identifier for heading links.</value>
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

            var @class = ClassList.Create(className)
                                  .Add(Attributes)
                                  .Add(alignClassName, TextAlign != TextAlign.Left)
                                  .ToString();

            if (Visible)
            {
                builder.OpenElement(0, tagName);
                builder.AddAttribute(1, "style", Style);
                builder.AddMultipleAttributes(2, Attributes);
                builder.AddAttribute(3, "class", @class);
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