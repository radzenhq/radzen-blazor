using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Radzen.Blazor.Markdown;
using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Blazor;

#nullable enable
#pragma warning disable BL0006 // Do not use RenderTree types
#pragma warning disable ASP0006 // Do not use non-literal sequence numbers

/// <summary>
/// A markdown rendering component that parses and displays Markdown syntax as formatted HTML.
/// RadzenMarkdown converts Markdown text (headings, lists, links, code blocks, etc.) into rich HTML content with security features.
/// Parses CommonMark-compliant markdown and renders it as HTML. Ideal for documentation, blog posts, README files, or any content authored in Markdown format.
/// Features full support for standard Markdown syntax (headings, bold, italic, lists, links, images, code, blockquotes, tables), optional HTML tag support within markdown with security filtering,
/// dangerous tag filtering (script, iframe, object) to prevent XSS attacks, automatic anchor link creation for headings (configurable depth),
/// control over allowed HTML tags and attributes, and flexible input via child content or Text property.
/// Parses markdown and renders it as Blazor components/HTML for display. Use AllowHtml = false to strictly render only Markdown syntax without any HTML pass-through.
/// </summary>
/// <example>
/// Basic markdown rendering:
/// <code>
/// &lt;RadzenMarkdown&gt;
/// # Welcome
/// This is **bold** and this is *italic*.
/// - List item 1
/// - List item 2
/// [Link to Radzen](https://radzen.com)
/// &lt;/RadzenMarkdown&gt;
/// </code>
/// Markdown from variable:
/// <code>
/// &lt;RadzenMarkdown Text=@markdownContent /&gt;
/// @code {
///     string markdownContent = "## Documentation\nThis is the content...";
/// }
/// </code>
/// Markdown with auto-linking headings:
/// <code>
/// &lt;RadzenMarkdown AutoLinkHeadingDepth="3" Text=@readme /&gt;
/// </code>
/// </example>
public partial class RadzenMarkdown : RadzenComponent
{
    /// <summary>
    /// Gets or sets the markdown content as a render fragment.
    /// The markdown text should be placed directly inside the component tags. Overridden by <see cref="Text"/> if both are set.
    /// </summary>
    /// <value>The markdown content render fragment.</value>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets whether HTML tags within the markdown are rendered or escaped.
    /// When true (default), safe HTML tags are allowed. Dangerous tags (script, iframe, style, object) are always filtered.
    /// When false, all HTML is treated as plain text and displayed literally.
    /// </summary>
    /// <value><c>true</c> to allow safe HTML; <c>false</c> to escape all HTML. Default is <c>true</c>.</value>
    [Parameter]
    public bool AllowHtml { get; set; } = true;

    /// <summary>
    /// Gets or sets a whitelist of HTML tags permitted in the markdown when <see cref="AllowHtml"/> is true.
    /// If set, only these tags will be rendered; others are stripped. If not set, uses a default list of safe tags.
    /// </summary>
    /// <value>The allowed HTML tag names, or null to use the default safe tag list.</value>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlTags { get; set; }

    /// <summary>
    /// Gets or sets a whitelist of HTML attributes permitted on HTML tags when <see cref="AllowHtml"/> is true.
    /// If set, only these attributes are rendered; others are stripped. If not set, uses a default list of safe attributes.
    /// </summary>
    /// <value>The allowed HTML attribute names, or null to use the default safe attribute list.</value>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlAttributes { get; set; }

    /// <summary>
    /// Gets or sets the markdown content as a string.
    /// When set, takes precedence over <see cref="ChildContent"/>. Use this to bind markdown from a variable.
    /// </summary>
    /// <value>The markdown text content.</value>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the maximum heading level (1-6) for which to automatically generate anchor links.
    /// For example, setting to 3 creates anchors for h1, h2, and h3 headings.
    /// Set to 0 to disable auto-linking. Auto-links enable table of contents navigation.
    /// </summary>
    /// <value>The maximum heading depth for auto-linking (0-6). Default is 0 (disabled).</value>
    [Parameter]
    public int AutoLinkHeadingDepth { get; set; }

    /// <inheritdoc />
    protected override string GetComponentCssClass()
    {
        return "rz-markdown";
    }

    private void RenderChildContent(RenderTreeBuilder builder)
    {
        if (string.IsNullOrEmpty(Text))
        {
            var buffer = new RenderTreeBuilder();

            ChildContent?.Invoke(buffer);

            var frames = buffer.GetFrames();

            ProcessFramesWithMarkers(builder, frames);
        }
        else
        {
            var document = MarkdownParser.Parse(Text);

            Render(document, builder, Empty);
        }
    }

    private void Render(Document document, RenderTreeBuilder builder, Action<RenderTreeBuilder, int> outlet)
    {
        var options = new BlazorMarkdownRendererOptions
        {
            AutoLinkHeadingDepth = AutoLinkHeadingDepth,
            AllowHtml = AllowHtml,
            AllowedHtmlAttributes = AllowedHtmlAttributes,
            AllowedHtmlTags = AllowedHtmlTags,
        };

        var visitor = new BlazorMarkdownRenderer(options, builder, outlet);

        document.Accept(visitor);
    }

    private static void Empty(RenderTreeBuilder builder, int marker) 
    { 
    }

    private void ProcessFramesWithMarkers(RenderTreeBuilder builder, ArrayRange<RenderTreeFrame> frames)
    {
        var markdown = new StringBuilder();
        var outletFrames = new Dictionary<int, (int startIndex, int endIndex)>();
        var markerId = 0;

        void ProcessRange(int start, int end)
        {
            var index = start;

            while (index < end)
            {
                var frame = frames.Array[index];

                if (frame.FrameType == RenderTreeFrameType.Text || frame.FrameType == RenderTreeFrameType.Markup)
                {
                    var content = frame.FrameType == RenderTreeFrameType.Text ? frame.TextContent : frame.MarkupContent;
                    markdown.Append(content);
                    index++;
                }
                else if (frame.FrameType == RenderTreeFrameType.Element)
                {
                    // Special-case: flatten <span class="rbs-text"> by inlining its children into markdown
                    if (string.Equals(frame.ElementName, "span", StringComparison.OrdinalIgnoreCase) && ElementHasCssClass(frames, index, "rbs-text"))
                    {
                        var subtreeEnd = index + frame.ElementSubtreeLength;

                        // Skip over attribute frames to reach first child
                        var childIndex = index + 1;
                        while (childIndex < subtreeEnd && frames.Array[childIndex].FrameType == RenderTreeFrameType.Attribute)
                        {
                            childIndex++;
                        }

                        // Inline-process children
                        ProcessRange(childIndex, subtreeEnd);

                        // Skip the entire element subtree
                        index = subtreeEnd;
                    }
                    else
                    {
                        // Insert a marker for this element and skip its subtree
                        var marker = string.Format(BlazorMarkdownRenderer.Outlet, markerId);
                        markdown.Append(marker);

                        var subtreeLength = frame.ElementSubtreeLength;
                        outletFrames.Add(markerId, (index, index + subtreeLength));
                        markerId++;
                        index += subtreeLength;
                    }
                }
                else if (frame.FrameType == RenderTreeFrameType.Component)
                {
                    // Insert a marker for this component and skip its subtree
                    var marker = string.Format(BlazorMarkdownRenderer.Outlet, markerId);
                    markdown.Append(marker);

                    var subtreeLength = frame.ComponentSubtreeLength;
                    outletFrames.Add(markerId, (index, index + subtreeLength));
                    markerId++;
                    index += subtreeLength;
                }
                else
                {
                    // Skip other frame types
                    index++;
                }
            }
        }

        ProcessRange(0, frames.Count);

        var document = MarkdownParser.Parse(markdown.ToString());

        void RenderOutlet(RenderTreeBuilder outletBuilder, int markerId)
        {
            if (outletFrames.TryGetValue(markerId, out var componentFrame))
            {
                CopyFrames(outletBuilder, frames, componentFrame.startIndex, componentFrame.endIndex);
            }
        }

        Render(document, builder, RenderOutlet);
    }

    private static void CopyFrames(RenderTreeBuilder builder, ArrayRange<RenderTreeFrame> frames, int startIndex, int endIndex)
    {
        while (startIndex < endIndex)
        {
            var frame = frames.Array[startIndex];

            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    builder.OpenElement(frame.Sequence, frame.ElementName);

                    CopyFrames(builder, frames, startIndex + 1, startIndex + frame.ElementSubtreeLength);

                    builder.CloseElement();

                    startIndex += frame.ElementSubtreeLength;
                    break;

                case RenderTreeFrameType.Component:
                    builder.OpenComponent(frame.Sequence, frame.ComponentType);

                    CopyFrames(builder, frames, startIndex + 1, startIndex + frame.ComponentSubtreeLength);

                    builder.CloseComponent();

                    startIndex += frame.ComponentSubtreeLength;
                    break;
                case RenderTreeFrameType.Attribute:
                    builder.AddAttribute(frame.Sequence, frame.AttributeName, frame.AttributeValue);
                    startIndex ++;
                    break;
                case RenderTreeFrameType.ElementReferenceCapture:
                    builder.AddElementReferenceCapture(frame.Sequence, frame.ElementReferenceCaptureAction);
                    startIndex ++;
                    break;
                case RenderTreeFrameType.ComponentReferenceCapture:
                    builder.AddComponentReferenceCapture(frame.Sequence, frame.ComponentReferenceCaptureAction);
                    startIndex ++;
                    break;
#if NET8_0_OR_GREATER
                case RenderTreeFrameType.ComponentRenderMode:
                    builder.AddComponentRenderMode(frame.ComponentRenderMode);
                    startIndex ++;
                    break;
#endif
                case RenderTreeFrameType.Text:
                    builder.AddContent(frame.Sequence, frame.TextContent);
                    startIndex ++;
                    break;
                case RenderTreeFrameType.Markup:
                    builder.AddMarkupContent(frame.Sequence, frame.MarkupContent);
                    startIndex ++;
                    break;
                default:
                    throw new InvalidOperationException($"Unexpected frame type {frame.FrameType}");

            }
        }
    }

    private static bool ElementHasCssClass(ArrayRange<RenderTreeFrame> frames, int elementIndex, string value)
    {
        var elementFrame = frames.Array[elementIndex];
        var end = elementIndex + elementFrame.ElementSubtreeLength;
        var index = elementIndex + 1;

        while (index < end && frames.Array[index].FrameType == RenderTreeFrameType.Attribute)
        {
            var attributeFrame = frames.Array[index];

            if (string.Equals(attributeFrame.AttributeName, "class", StringComparison.OrdinalIgnoreCase))
            {
                if (attributeFrame.AttributeValue is string className)
                {
                    var parts = className.Split([' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

                    for (var i = 0; i < parts.Length; i++)
                    {
                        if (string.Equals(parts[i], value, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            index++;
        }

        return false;
    }
}
#pragma warning restore BL0006 // Do not use RenderTree types
#pragma warning restore ASP0006 // Do not use non-literal sequence numbers