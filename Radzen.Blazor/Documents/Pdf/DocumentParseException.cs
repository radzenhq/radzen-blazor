using System;

namespace Radzen.Documents.Pdf;

/// <summary>
/// The exception thrown when a PDF byte stream does not conform to the
/// object or cross-reference grammar of ISO 32000-1.
/// </summary>
public class DocumentParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParseException"/> class.
    /// </summary>
    public DocumentParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParseException"/> class
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DocumentParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParseException"/> class
    /// with a specified error message and a reference to the inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public DocumentParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentParseException"/> class
    /// with a specified error message and the byte offset of the problem.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="offset">The byte offset within the source where the error was detected.</param>
    public DocumentParseException(string message, long offset)
        : base(message)
    {
        Offset = offset;
    }

    /// <summary>
    /// Gets the byte offset within the source document where the error was detected.
    /// </summary>
    public long Offset { get; }
}
