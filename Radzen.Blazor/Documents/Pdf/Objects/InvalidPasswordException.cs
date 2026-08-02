using System;

namespace Radzen.Documents.Pdf.Objects;

internal class InvalidPasswordException : DocumentParseException
{
    public InvalidPasswordException()
        : base("The supplied password does not open this document.")
    {
    }

    public InvalidPasswordException(string message)
        : base(message)
    {
    }

    public InvalidPasswordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
