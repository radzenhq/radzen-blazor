#nullable enable
using System;
using System.Text;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Revision 6 (AESV3) read-path integrity and password handling:
//   #73 /Perms is decrypted and checked against /P (ISO 32000-2 algorithm 13).
//   #72 R6 passwords are SASLprep/NFKC-normalized, so composed and decomposed
//        Unicode forms of the same password authenticate identically.
// Built through the library's own DeriveAes256 write side so the fixture is
// self-consistent (no external PDF required).
public class R6PermsAndPasswordTests
{
    private static readonly byte[] FileKey = Fixed(32, 7);
    private static readonly byte[] DocumentId = Fixed(16, 3);
    private const int Permissions = -44;

    [Fact]
    public void ValidPerms_UserPasswordAuthenticates()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);
        var handler = new StandardSecurityHandler(encrypt, DocumentId, "secret");
        Assert.True(handler.IsUserPassword);
        Assert.Equal(FileKey, handler.FileKey);
    }

    // A /P edited to grant extra permissions no longer matches the encrypted /Perms.
    [Fact]
    public void TamperedPermissions_Throws()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);
        encrypt["P"] = new NumberObject(-4); // claim more permissions than /Perms encodes

        Assert.Throws<DocumentParseException>(
            () => new StandardSecurityHandler(encrypt, DocumentId, "secret"));
    }

    [Fact]
    public void MissingPerms_UserPasswordAuthenticates()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions, includePerms: false);

        Assert.True(new StandardSecurityHandler(encrypt, DocumentId, "secret").IsUserPassword);
    }

    [Fact]
    public void ShortPerms_Throws()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);
        encrypt["Perms"] = Str(new byte[15]);

        Assert.Throws<DocumentParseException>(
            () => new StandardSecurityHandler(encrypt, DocumentId, "secret"));
    }

    [Fact]
    public void PermsWrongFixedBytes_Throws()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);
        RewritePerms(encrypt, decoded => decoded[4] = 0);

        Assert.Throws<DocumentParseException>(
            () => new StandardSecurityHandler(encrypt, DocumentId, "secret"));
    }

    [Fact]
    public void PermsMetadataFlagMismatch_Throws()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);
        RewritePerms(encrypt, decoded => decoded[8] = (byte)'F');

        Assert.Throws<DocumentParseException>(
            () => new StandardSecurityHandler(encrypt, DocumentId, "secret"));
    }

    // #72: "cafe"+acute as a single composed codepoint (U+00E9) and as "e" + combining
    // acute (U+0301) are the same password after NFKC; either form must open a file
    // whose key was derived from the other.
    [Fact]
    public void ComposedAndDecomposedPassword_AuthenticateEquivalently()
    {
        var composed = "caf\u00e9";   // U+00E9 e-with-acute, composed
        var decomposed = "cafe\u0301"; // e + U+0301 combining acute, decomposed
        var encrypt = BuildEncrypt(composed, "owner", Permissions);

        var handler = new StandardSecurityHandler(encrypt, DocumentId, decomposed);
        Assert.True(handler.IsUserPassword);
        Assert.Equal(FileKey, handler.FileKey);
    }

    // SASLprep B.1 deletes U+00AD (SOFT HYPHEN); NFKC keeps it. A file keyed on the clean
    // password must open with the soft-hyphen form and vice versa. Fails on the NFKC-only
    // base (the two forms derive different keys).
    [Fact]
    public void SoftHyphenPassword_AuthenticatesAsIfRemoved()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);

        var handler = new StandardSecurityHandler(encrypt, DocumentId, "sec\u00adret");
        Assert.True(handler.IsUserPassword);
        Assert.Equal(FileKey, handler.FileKey);
    }

    // SASLprep B.1 deletes U+200B (ZERO WIDTH SPACE); NFKC keeps it.
    [Fact]
    public void ZeroWidthSpacePassword_AuthenticatesAsIfRemoved()
    {
        var encrypt = BuildEncrypt("secret", "owner", Permissions);

        Assert.True(new StandardSecurityHandler(encrypt, DocumentId, "sec\u200bret").IsUserPassword);
    }

    // SASLprep C.1.2 maps U+1680 (OGHAM SPACE MARK) to U+0020; NFKC leaves it unchanged.
    [Fact]
    public void OghamSpacePassword_AuthenticatesAsRegularSpace()
    {
        var encrypt = BuildEncrypt("open sesame", "owner", Permissions);

        var handler = new StandardSecurityHandler(encrypt, DocumentId, "open\u1680sesame");
        Assert.True(handler.IsUserPassword);
        Assert.Equal(FileKey, handler.FileKey);
    }

    // Printable ASCII is outside every SASLprep table, so the derived key is unchanged.
    [Fact]
    public void PrintableAsciiPassword_Authenticates()
    {
        var encrypt = BuildEncrypt("P@ssw0rd!~ ", "owner", Permissions);

        var handler = new StandardSecurityHandler(encrypt, DocumentId, "P@ssw0rd!~ ");
        Assert.True(handler.IsUserPassword);
        Assert.Equal(FileKey, handler.FileKey);
    }

    // A prohibited code point (U+202E RIGHT-TO-LEFT OVERRIDE, RFC 3454 C.8) is rejected
    // loudly instead of being passed through NFKC-mangled.
    [Fact]
    public void ProhibitedCodePointPassword_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => BuildEncrypt("bad\u202epassword", "owner", Permissions));
    }

    // A RandALCat character (Hebrew Alef) whose string does not begin and end with a
    // RandALCat violates the bidi rule (RFC 3454 6) and is rejected.
    [Fact]
    public void BidiFirstLastViolationPassword_Throws()
    {
        Assert.Throws<DocumentParseException>(
            () => BuildEncrypt("abc\u05d0", "owner", Permissions));
    }

    // A well-formed right-to-left password (first and last RandALCat) is accepted.
    [Fact]
    public void RightToLeftPassword_Authenticates()
    {
        var encrypt = BuildEncrypt("\u05d0\u05d1\u05d2", "owner", Permissions);

        Assert.True(new StandardSecurityHandler(encrypt, DocumentId, "\u05d0\u05d1\u05d2").IsUserPassword);
    }

    private static DictionaryObject BuildEncrypt(string userPassword, string ownerPassword, int permissions, bool includePerms = true)
    {
        var (owner, user, ownerEncrypted, userEncrypted, perms) = StandardSecurityHandler.DeriveAes256(
            userPassword, ownerPassword, FileKey, permissions, encryptMetadata: true,
            userValidation: Fixed(8, 1), userKeySalt: Fixed(8, 2),
            ownerValidation: Fixed(8, 4), ownerKeySalt: Fixed(8, 5), permsNoise: Fixed(4, 6));

        var encrypt = new DictionaryObject
        {
            ["V"] = new NumberObject(5),
            ["R"] = new NumberObject(6),
            ["P"] = new NumberObject(permissions),
            ["O"] = Str(owner),
            ["U"] = Str(user),
            ["OE"] = Str(ownerEncrypted),
            ["UE"] = Str(userEncrypted),
        };

        if (includePerms)
        {
            encrypt["Perms"] = Str(perms);
        }

        return encrypt;
    }

    private static StringObject Str(byte[] bytes) => new(Encoding.Latin1.GetString(bytes));

    private static void RewritePerms(DictionaryObject encrypt, Action<byte[]> rewrite)
    {
        var encrypted = Encoding.Latin1.GetBytes(Assert.IsType<StringObject>(encrypt["Perms"]).Value);
        var decoded = AesCbc.DecryptCbcNoPadding(FileKey, new byte[16], encrypted);
        rewrite(decoded);
        encrypt["Perms"] = Str(AesCbc.EncryptCbcNoPadding(FileKey, new byte[16], decoded));
    }

    private static byte[] Fixed(int length, int seed)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = (byte)((i * 31 + seed) & 0xFF);
        }

        return result;
    }
}
