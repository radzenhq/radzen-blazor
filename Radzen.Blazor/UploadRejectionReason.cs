namespace Radzen;

/// <summary>Identifies why a file was rejected during selection.</summary>
public enum UploadRejectionReason
{
    /// <summary>The file exceeds the maximum size in bytes.</summary>
    FileTooLarge,
    /// <summary>The filename does not have an allowed suffix.</summary>
    ExtensionNotAllowed
}
