#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class EncryptionMetadataTests
{
    [Fact]
    public void EncryptMetadataFalse_LeavesMetadataPlaintext()
    {
        var bytes = EncryptedDocument(encryptMetadata: false);
        Assert.Contains("xpacket", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptMetadataTrue_EncryptsMetadataStream()
    {
        var bytes = EncryptedDocument(encryptMetadata: true);
        Assert.DoesNotContain("xpacket", Encoding.Latin1.GetString(bytes), StringComparison.Ordinal);
    }

    private static byte[] EncryptedDocument(bool encryptMetadata)
    {
        var document = new Document();
        document.Info.Producer = "Radzen metadata test producer";
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (secret) Tj ET"));
        document.Encryption = new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            OwnerPassword = "owner",
            EncryptMetadata = encryptMetadata,
        };
        return document.ToArray();
    }
}
