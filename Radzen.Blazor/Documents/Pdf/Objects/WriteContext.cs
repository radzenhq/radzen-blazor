using Radzen.Documents.Pdf.Objects.Encryption;

namespace Radzen.Documents.Pdf.Objects;

// Explicit write-side context threaded through DocumentObject.Write in place of
// the former thread-static encryption ambient. Carries the enclosing indirect
// object's number/generation and the optional encryptor so StringObject and
// StreamObject can encrypt their bytes without reaching for a global.
internal readonly struct WriteContext(EncryptionWriter? encryptor, int objectNumber, int generation)
{
    // Default context for unencrypted serialization: no encryptor, object 0.
    public static readonly WriteContext None;

    public EncryptionWriter? Encryptor { get; } = encryptor;

    public int ObjectNumber { get; } = objectNumber;

    public int Generation { get; } = generation;
}
