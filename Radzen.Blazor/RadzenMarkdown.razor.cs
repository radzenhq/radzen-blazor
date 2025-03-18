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
    /// Gets or sets the markdown content as a string. Overrides <see cref="ChildContent"/> if set.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

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
            
            var visitor = new BlazorMarkdownRenderer(builder, Empty);

            document.Accept(visitor);
        }
    }

    private static void Empty(RenderTreeBuilder builder, int marker) 
    { 
    }

    private static void ProcessFramesWithMarkers(RenderTreeBuilder builder, ArrayRange<RenderTreeFrame> frames)
    {
        var markdownBuilder = new StringBuilder();
        var componentFrames = new Dictionary<int, (int startIndex, int endIndex)>();
        var markerId = 0;
        var index = 0;

        while (index < frames.Count)
        {
            var frame = frames.Array[index];

            if (frame.FrameType == RenderTreeFrameType.Text || frame.FrameType == RenderTreeFrameType.Markup)
            {
                var content = frame.FrameType == RenderTreeFrameType.Text ? frame.TextContent : frame.MarkupContent;
                markdownBuilder.Append(content);
                index++;
            }
            else if (frame.FrameType == RenderTreeFrameType.Component || frame.FrameType == RenderTreeFrameType.Element)
            {
                // Insert a marker for this component
                var marker = string.Format(BlazorMarkdownRenderer.Outlet, markerId);
                markdownBuilder.Append(marker);

                // Store the component information for later
                var subtreeLength = GetSubtreeLength(frame);
                componentFrames.Add(markerId, (index, index + subtreeLength));

                // Increment marker ID and skip past this component
                markerId++;
                index += subtreeLength;
            }
            else
            {
                // Skip other frame types
                index++;
            }
        }

        var document = MarkdownParser.Parse(markdownBuilder.ToString());

        void RenderOutlet(RenderTreeBuilder outletBuilder, int markerId)
        {
            if (componentFrames.TryGetValue(markerId, out var componentFrame))
            {
                CopyFrames(outletBuilder, frames, componentFrame.startIndex, componentFrame.endIndex);
            }
        }

        var visitor = new BlazorMarkdownRenderer(builder, RenderOutlet);

        document.Accept(visitor);
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

                    startIndex += frame.ElementSubtreeLength;
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

    private static int GetSubtreeLength(RenderTreeFrame frame)
    {
        return frame.FrameType switch
        {
            RenderTreeFrameType.Element => frame.ElementSubtreeLength,
            RenderTreeFrameType.Component => frame.ComponentSubtreeLength,
            RenderTreeFrameType.Region => frame.RegionSubtreeLength,
            _ => 1,
        };
    }
}
#pragma warning restore BL0006 // Do not use RenderTree types
#pragma warning restore ASP0006 // Do not use non-literal sequence numbers