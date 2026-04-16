using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A label component for associating descriptive text with form input components.
    /// RadzenLabel creates accessible labels linked to input fields via the HTML for/id relationship.
    /// Provides descriptive text for form inputs, improving usability and accessibility. When properly associated with an input (via the Component property), clicking the label focuses the input.
    /// Features association linking to input components via the Component property (matching the input's Name), proper label/input relationships for screen readers,
    /// click behavior that focuses the associated input, and content display via Text property or custom content via ChildContent.
    /// Always use labels with form inputs for better UX and accessibility compliance. The Component property should match the Name property of the input it describes.
    /// </summary>
    /// <example>
    /// Basic label with input:
    /// <code>
    /// &lt;RadzenLabel Text="Email Address" Component="EmailInput" /&gt;
    /// &lt;RadzenTextBox Name="EmailInput" @bind-Value=@email /&gt;
    /// </code>
    /// Label with custom content:
    /// <code>
    /// &lt;RadzenLabel Component="PasswordInput"&gt;
    ///     Password &lt;span style="color: red;"&gt;*&lt;/span&gt;
    /// &lt;/RadzenLabel&gt;
    /// &lt;RadzenPassword Name="PasswordInput" @bind-Value=@password /&gt;
    /// </code>
    /// </example>
    public partial class RadzenLabel : RadzenComponent
    {
        /// <summary>
        /// Gets or sets custom child content to render as the label text.
        /// When set, overrides the <see cref="Text"/> property for displaying complex label content.
        /// </summary>
        /// <value>The label content render fragment.</value>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the name of the input component this label is associated with.
        /// Must match the Name property of the target input component to create the proper label/input relationship.
        /// When set, clicking the label will focus the associated input.
        /// </summary>
        /// <value>The target component's name for label association.</value>
        [Parameter]
        public string? Component { get; set; }

        /// <summary>
        /// Gets or sets the label text to display.
        /// For simple text labels, use this property. For complex content, use <see cref="ChildContent"/> instead.
        /// </summary>
        /// <value>The label text. Default is empty string.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the typography style applied to the label, matching the presets available on <see cref="RadzenText"/>
        /// (H1–H6, Subtitle1/2, Body1/2, Button, Caption, Overline, DisplayH1–H6).
        /// When <c>null</c> (default), no typography class is added and the label inherits the surrounding text styles.
        /// </summary>
        /// <value>The text style, or <c>null</c> to leave the label unstyled. Default is <c>null</c>.</value>
        [Parameter]
        public TextStyle? TextStyle { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return TextStyle is { } style ? $"rz-label {GetTextStyleCssClass(style)}" : "rz-label";
        }

        static string GetTextStyleCssClass(TextStyle style) => style switch
        {
            Blazor.TextStyle.DisplayH1 => "rz-text-display-h1",
            Blazor.TextStyle.DisplayH2 => "rz-text-display-h2",
            Blazor.TextStyle.DisplayH3 => "rz-text-display-h3",
            Blazor.TextStyle.DisplayH4 => "rz-text-display-h4",
            Blazor.TextStyle.DisplayH5 => "rz-text-display-h5",
            Blazor.TextStyle.DisplayH6 => "rz-text-display-h6",
            Blazor.TextStyle.H1 => "rz-text-h1",
            Blazor.TextStyle.H2 => "rz-text-h2",
            Blazor.TextStyle.H3 => "rz-text-h3",
            Blazor.TextStyle.H4 => "rz-text-h4",
            Blazor.TextStyle.H5 => "rz-text-h5",
            Blazor.TextStyle.H6 => "rz-text-h6",
            Blazor.TextStyle.Subtitle1 => "rz-text-subtitle1",
            Blazor.TextStyle.Subtitle2 => "rz-text-subtitle2",
            Blazor.TextStyle.Body1 => "rz-text-body1",
            Blazor.TextStyle.Body2 => "rz-text-body2",
            Blazor.TextStyle.Button => "rz-text-button",
            Blazor.TextStyle.Caption => "rz-text-caption",
            Blazor.TextStyle.Overline => "rz-text-overline",
            _ => "rz-text-body1",
        };
    }
}
