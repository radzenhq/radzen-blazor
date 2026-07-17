#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.6.5: for V>=4 the file-key length comes from the crypt-filter dictionary, not the top-level /Length.
public class EncryptionCryptFilterKeyLengthTests
{
    [Fact]
    public void AesV2WithoutTopLevelLength_DerivesSixteenByteKey()
    {
        var (encrypt, documentId) = LoadAesV2WithoutTopLevelLength();
        var handler = new StandardSecurityHandler(encrypt, documentId, string.Empty);
        Assert.True(handler.IsUserPassword);
        Assert.Equal(16, handler.FileKey.Length);
    }

    private static (DictionaryObject Encrypt, byte[] DocumentId) LoadAesV2WithoutTopLevelLength()
    {
        var bytes = PdfTestResources.ReadAllBytes("Documents/encrypted-aes-128.pdf");
        var reader = DocumentReader.Parse(bytes, "owner");
        var source = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Encrypt"]));
        var id = Assert.IsType<ArrayObject>(reader.Trailer["ID"]);
        var documentId = Encoding.Latin1.GetBytes(Assert.IsType<StringObject>(id[0]).Value);

        var encrypt = new DictionaryObject();
        foreach (var key in source.Keys)
        {
            if (key != "Length")
            {
                encrypt[key] = source[key];
            }
        }

        return (encrypt, documentId);
    }
}
