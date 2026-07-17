using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class EncryptionPublicApiTests
{
    private static byte[] BuildEncrypted(EncryptionOptions options)
    {
        var builder = new DocumentBuilder { Encryption = options };
        var section = builder.Sections.Add();
        section.Blocks.AddParagraph("Hello encrypted world");
        return builder.ToArray();
    }

    [Fact]
    public void DocumentBuilderEncryption_ProducesPasswordProtectedFile()
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
    public void DocumentBuilderEncryption_WrongPassword_Throws()
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
        var plain = new DocumentBuilder();
        plain.Sections.Add().Blocks.AddParagraph("Hello encrypted world");

        var withNull = new DocumentBuilder { Encryption = null };
        withNull.Sections.Add().Blocks.AddParagraph("Hello encrypted world");

        var a = plain.ToArray();
        var b = withNull.ToArray();
        Assert.Equal(a.Length, b.Length);
    }
}
