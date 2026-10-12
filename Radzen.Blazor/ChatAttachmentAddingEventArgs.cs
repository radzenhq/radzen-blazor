using System;

namespace Radzen.Blazor;

/// <summary>
/// The arguments of <see cref="RadzenAIChat.AttachmentAdding"/>: the file the user picked, before it is attached to the next message.
/// </summary>
public class ChatAttachmentAddingEventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChatAttachmentAddingEventArgs"/> class.
    /// </summary>
    /// <param name="attachment">The attachment read from the picked file.</param>
    public ChatAttachmentAddingEventArgs(ChatAttachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        Attachment = attachment;
    }

    /// <summary>
    /// Gets or sets the attachment. Replace it to attach something else instead, for example the same image scaled down.
    /// </summary>
    public ChatAttachment Attachment { get; set; }

    /// <summary>
    /// Gets or sets whether the file is not attached.
    /// </summary>
    public bool Cancel { get; set; }
}
