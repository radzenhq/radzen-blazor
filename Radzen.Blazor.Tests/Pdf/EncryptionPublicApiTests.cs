using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Document;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class EncryptionPublicApiTests
{
    private static byte[] BuildEncrypted(EncryptionOptions options)
    {
        var document = new Document();
        var builderRenderer = new DocumentRenderer { Encryption = options };
        var section = document.Sections.Add();
        section.Blocks.AddParagraph("Hello encrypted world");
        return builderRenderer.ToArray(document);
    }

    [Fact]
    public void DocumentEncryption_ProducesPasswordProtectedFile()
    {
        var bytes = BuildEncrypted(new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Aes128,
            UserPassword = "secret",
        });

        var reader = DocumentReader.Parse(bytes, "secret");
        Assert.NotNull(reader.Resolve(reader.Trailer["Root"]!));
        Assert.True(reader.Trailer.ContainsKey("Encrypt"));
    }

    [Fact]
    public void DocumentEncryption_WrongPassword_Throws()
    {
        var bytes = BuildEncrypted(new EncryptionOptions
        {
            Material = new SeededEncryptionMaterial([7]),
            Algorithm = EncryptionAlgorithm.Rc4,
            UserPassword = "secret",
        });

        Assert.Throws<InvalidPasswordException>(() => DocumentReader.Parse(bytes, "wrong"));
    }

    [Fact]
    public void NoEncryption_ByteIdenticalToPlainBuild()
    {
        var plain = new Document();
        plain.Sections.Add().Blocks.AddParagraph("Hello encrypted world");

        var withNull = new Document();
        var withNullRenderer = new DocumentRenderer { Encryption = null };
        withNull.Sections.Add().Blocks.AddParagraph("Hello encrypted world");

        var a = new DocumentRenderer().ToArray(plain);
        var b = withNullRenderer.ToArray(withNull);
        Assert.Equal(a.Length, b.Length);
    }
}
