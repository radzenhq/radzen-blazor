using System;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Thrown when an encrypted PDF document cannot be opened because the supplied
/// password matches neither the user nor the owner password.
/// </summary>
public class InvalidPasswordException : DocumentParseException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordException"/> class.
    /// </summary>
    public InvalidPasswordException()
        : base("The supplied password does not open this document.")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public InvalidPasswordException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPasswordException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public InvalidPasswordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
