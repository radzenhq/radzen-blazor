using System;
using Microsoft.Extensions.AI;

namespace Radzen.Blazor;

/// <summary>
/// A file attached to a <see cref="ChatMessage"/>, for example an image the user sends to a multimodal model.
/// </summary>
public class ChatAttachment
{
    private string? url;

    /// <summary>
    /// Gets or sets the file name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the media type, for example <c>image/png</c>.
    /// </summary>
    public string MediaType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Gets or sets the file content.
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; set; }

    /// <summary>
    /// Gets whether the attachment is an image.
    /// </summary>
    public bool IsImage => MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the content as a data URI, usable as an image source.
    /// </summary>
    public string Url => url ??= $"data:{MediaType};base64,{Convert.ToBase64String(Data.Span)}";

    /// <summary>
    /// Converts the attachment to the <see cref="DataContent"/> sent to the model.
    /// </summary>
    public DataContent ToDataContent() => new(Data, MediaType) { Name = Name };

    /// <summary>
    /// Gets the media type for a file name from its extension, or <c>application/octet-stream</c> when unknown.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    public static string GetMediaType(string fileName)
    {
        var extension = System.IO.Path.GetExtension(fileName ?? string.Empty).ToLowerInvariant();

        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            ".txt" or ".log" => "text/plain",
            ".md" or ".markdown" => "text/markdown",
            ".csv" => "text/csv",
            ".tsv" => "text/tab-separated-values",
            ".html" or ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "text/javascript",
            ".ts" => "text/typescript",
            ".cs" or ".razor" or ".py" or ".java" or ".sql" or ".ps1" or ".sh" or ".ini" or ".cfg" => "text/plain",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".yaml" or ".yml" => "application/yaml",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// Creates an attachment from the <see cref="DataContent"/> of a message.
    /// </summary>
    /// <param name="content">The content.</param>
    public static ChatAttachment FromDataContent(DataContent content)
    {
        ArgumentNullException.ThrowIfNull(content);

        return new ChatAttachment { Name = content.Name ?? string.Empty, MediaType = content.MediaType, Data = content.Data };
    }
}
