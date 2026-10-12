using System;
using System.Collections.Generic;
using Microsoft.Extensions.AI;

namespace Radzen.Blazor;

/// <summary>
/// A source the assistant used for an answer: a web page, a document or a record a tool returned.
/// Citations come from the <see cref="CitationAnnotation"/> annotations a provider attaches to the response (OpenAI web search, Perplexity, Azure AI Search and others)
/// and from tools that return <see cref="ChatCitation"/> instances; both are collected in <see cref="ChatMessage.Citations"/> and rendered under the answer.
/// </summary>
public class ChatCitation
{
    /// <summary>
    /// Gets or sets the title of the source.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the URL of the source. Rendered as a link when set.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the part of the source the answer is based on. Sent to the model when a tool returns the citation and shown as a tooltip.
    /// </summary>
    public string? Snippet { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the file the citation refers to, for providers that cite uploaded files.
    /// </summary>
    public string? FileId { get; set; }

    /// <summary>
    /// Gets or sets the name of the tool that produced the citation.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Creates a citation from a <see cref="CitationAnnotation"/>.
    /// </summary>
    /// <param name="annotation">The annotation.</param>
    public static ChatCitation FromAnnotation(CitationAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);

        return new ChatCitation
        {
            Title = annotation.Title,
            Url = annotation.Url?.ToString(),
            Snippet = annotation.Snippet,
            FileId = annotation.FileId,
            ToolName = annotation.ToolName
        };
    }

    /// <summary>
    /// Converts the citation to the <see cref="CitationAnnotation"/> providers and <see cref="Microsoft.Extensions.AI"/> consumers understand.
    /// </summary>
    public CitationAnnotation ToAnnotation()
    {
        return new CitationAnnotation
        {
            Title = Title,
            Url = Uri.TryCreate(Url, UriKind.Absolute, out var uri) ? uri : null,
            Snippet = Snippet,
            FileId = FileId,
            ToolName = ToolName
        };
    }

    /// <summary>
    /// Returns whether this citation refers to the same source as another one: the same URL, or the same file, or the same title when neither is set.
    /// </summary>
    /// <param name="other">The other citation.</param>
    public bool IsSameSource(ChatCitation? other)
    {
        if (other == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(Url) || !string.IsNullOrEmpty(other.Url))
        {
            return string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrEmpty(FileId) || !string.IsNullOrEmpty(other.FileId))
        {
            return string.Equals(FileId, other.FileId, StringComparison.Ordinal);
        }

        return string.Equals(Title, other.Title, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Collects the citations a piece of response content carries: its <see cref="CitationAnnotation"/> annotations and, for a tool result, the <see cref="ChatCitation"/> instances the tool returned,
    /// as objects or as the JSON <see cref="AIFunctionFactory"/> produces from them (objects with only title, url, snippet, fileId and toolName properties).
    /// </summary>
    /// <param name="content">The content.</param>
    public static IEnumerable<ChatCitation> FromContent(AIContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Annotations != null)
        {
            foreach (var annotation in content.Annotations)
            {
                if (annotation is CitationAnnotation citation)
                {
                    yield return FromAnnotation(citation);
                }
            }
        }

        if (content is FunctionResultContent result)
        {
            foreach (var citation in FromResult(result.Result))
            {
                yield return citation;
            }
        }
    }

    private static readonly System.Text.RegularExpressions.Regex markers = new("\u3010(\\d+)(?:\u2020[^\u3011]*)?\u3011", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// Replaces the citation markers some models emit, such as the <c>【1†source】</c> markers of gpt-oss models, with plain <c>[1]</c> markers that render as text.
    /// </summary>
    /// <param name="text">The answer text.</param>
    public static string NormalizeMarkers(string text)
    {
        return string.IsNullOrEmpty(text) || !text.Contains('【', StringComparison.Ordinal) ? text : markers.Replace(text, "[$1]");
    }

    private static readonly string[] properties = ["title", "url", "snippet", "fileId", "toolName"];

    private static IEnumerable<ChatCitation> FromJson(System.Text.Json.JsonElement element)
    {
        switch (element.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (TryRead(item, out var citation))
                    {
                        yield return citation;
                    }
                }
                break;
            case System.Text.Json.JsonValueKind.Object:
                if (TryRead(element, out var single))
                {
                    yield return single;
                }
                break;
        }
    }

    private static bool TryRead(System.Text.Json.JsonElement element, out ChatCitation citation)
    {
        citation = new ChatCitation();

        if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind != System.Text.Json.JsonValueKind.String && property.Value.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                return false;
            }

            var value = property.Value.ValueKind == System.Text.Json.JsonValueKind.String ? property.Value.GetString() : null;

            if (property.Name.Equals(properties[0], StringComparison.OrdinalIgnoreCase))
            {
                citation.Title = value;
            }
            else if (property.Name.Equals(properties[1], StringComparison.OrdinalIgnoreCase))
            {
                citation.Url = value;
            }
            else if (property.Name.Equals(properties[2], StringComparison.OrdinalIgnoreCase))
            {
                citation.Snippet = value;
            }
            else if (property.Name.Equals(properties[3], StringComparison.OrdinalIgnoreCase))
            {
                citation.FileId = value;
            }
            else if (property.Name.Equals(properties[4], StringComparison.OrdinalIgnoreCase))
            {
                citation.ToolName = value;
            }
            else
            {
                return false;
            }
        }

        return !string.IsNullOrEmpty(citation.Title) || !string.IsNullOrEmpty(citation.Url);
    }

    private static IEnumerable<ChatCitation> FromResult(object? result)
    {
        switch (result)
        {
            case ChatCitation citation:
                yield return citation;
                break;
            case AIContent content:
                foreach (var citation in FromContent(content))
                {
                    yield return citation;
                }
                break;
            case string:
                break;
            case System.Text.Json.JsonElement element:
                foreach (var citation in FromJson(element))
                {
                    yield return citation;
                }
                break;
            case IEnumerable<ChatCitation> citations:
                foreach (var citation in citations)
                {
                    yield return citation;
                }
                break;
            case System.Collections.IEnumerable items:
                foreach (var item in items)
                {
                    if (item is ChatCitation citation)
                    {
                        yield return citation;
                    }
                }
                break;
        }
    }
}
