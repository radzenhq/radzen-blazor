using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

#nullable enable

public class EncryptionPublicApiTests
{
    private static byte[] BuildEncrypted(EncryptionOptions options)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Paragraph("Hello encrypted world"));
        var rendered = new DocumentRenderer().Render(document);
        rendered.Encryption = options;
        return rendered.ToArray();
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

    private static byte[] BuildWithPasswordAndPermissionsOnly()
        => BuildEncrypted(new EncryptionOptions
        {
            UserPassword = "secret",
            OwnerPassword = "owner",
            AllowPrinting = false,
            AllowContentCopy = false,
        });

    [Fact]
    public void DocumentEncryption_WithoutMaterial_ProducesReadableEncryptedFile()
    {
        var bytes = BuildWithPasswordAndPermissionsOnly();
        var encryption = Line(Encoding.Latin1.GetString(bytes), "/Filter /Standard");

        LacksFlag("encryption dictionary", encryption, "P", 0x004);
        LacksFlag("encryption dictionary", encryption, "P", 0x010);

        var reader = DocumentReader.Parse(bytes, "secret");
        Assert.NotNull(reader.Resolve(reader.Trailer["Root"]!));
        Assert.Throws<InvalidPasswordException>(() => DocumentReader.Parse(bytes, "wrong"));
    }

    [Fact]
    public void DocumentEncryption_WithoutMaterial_DiffersBetweenDocuments()
    {
        var first = BuildWithPasswordAndPermissionsOnly();
        var second = BuildWithPasswordAndPermissionsOnly();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void NoEncryption_ByteIdenticalToPlainBuild()
    {
        var plain = new Document();
        plain.Sections.Add().Blocks.Add(new Paragraph("Hello encrypted world"));

        var withNull = new Document();
        withNull.Sections.Add().Blocks.Add(new Paragraph("Hello encrypted world"));
        var withNullRendered = new DocumentRenderer().Render(withNull);
        withNullRendered.Encryption = null;

        var a = new DocumentRenderer().ToArray(plain);
        var b = withNullRendered.ToArray();
        Assert.Equal(a, b);
    }
}
