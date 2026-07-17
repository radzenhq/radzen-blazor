using Radzen.Documents.Pdf.Objects.Encryption;

namespace Radzen.Documents.Pdf.Objects;

internal readonly struct WriteContext(EncryptionWriter? encryptor, int objectNumber, int generation)
{
    public static readonly WriteContext None;

    public EncryptionWriter? Encryptor { get; } = encryptor;

    public int ObjectNumber { get; } = objectNumber;

    public int Generation { get; } = generation;
}
