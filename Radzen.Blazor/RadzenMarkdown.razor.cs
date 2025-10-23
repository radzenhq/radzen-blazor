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
/// A component which renders markdown content.
/// </summary>
/// <example>
/// &lt;RadzenMarkdown&gt;
/// # Hello, world!
/// - This is a list item
/// - This is another list item
/// > This is a blockquote
/// &lt;/RadzenMarkdown&gt;
/// </example>
public partial class RadzenMarkdown : RadzenComponent
{
    /// <summary>
    /// Gets or sets the markdown content.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to allow HTML content in the markdown. Certain dangerous HTML tags (script, style, object, iframe) and attributes are removed.
    /// Set to <c>true</c> by default.
    /// </summary>
    [Parameter]
    public bool AllowHtml { get; set; } = true;

    /// <summary>
    /// Gets or sets a list of allowed HTML tags. If set, only these tags will be allowed in the markdown content. By default would use a list of safe HTML tags.
    /// Considered only if <see cref="AllowHtml"/> is set to <c>true</c>.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlTags { get; set; }

    /// <summary>
    /// Gets or sets a list of allowed HTML attributes. If set, only these attributes will be allowed in the markdown content. By default would use a list of safe HTML attributes.
    /// Considered only if <see cref="AllowHtml"/> is set to <c>true</c>.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? AllowedHtmlAttributes { get; set; }

    /// <summary>
    /// Gets or sets the markdown content as a string. Overrides <see cref="ChildContent"/> if set.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    /// <summary>
    /// The maximum heading depth to create anchor links for. Set to <c>0</c> to disable auto-linking.
    /// </summary>
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