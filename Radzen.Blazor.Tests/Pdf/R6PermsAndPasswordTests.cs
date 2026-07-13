#nullable enable
using System.Text;
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

    private static DictionaryObject BuildEncrypt(string userPassword, string ownerPassword, int permissions)
    {
        var (owner, user, ownerEncrypted, userEncrypted, perms) = StandardSecurityHandler.DeriveAes256(
            userPassword, ownerPassword, FileKey, permissions, encryptMetadata: true,
            userValidation: Fixed(8, 1), userKeySalt: Fixed(8, 2),
            ownerValidation: Fixed(8, 4), ownerKeySalt: Fixed(8, 5), permsNoise: Fixed(4, 6));

        return new DictionaryObject
        {
            ["V"] = new NumberObject(5),
            ["R"] = new NumberObject(6),
            ["P"] = new NumberObject(permissions),
            ["O"] = Str(owner),
            ["U"] = Str(user),
            ["OE"] = Str(ownerEncrypted),
            ["UE"] = Str(userEncrypted),
            ["Perms"] = Str(perms),
        };
    }

    private static StringObject Str(byte[] bytes) => new(Encoding.Latin1.GetString(bytes));

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
