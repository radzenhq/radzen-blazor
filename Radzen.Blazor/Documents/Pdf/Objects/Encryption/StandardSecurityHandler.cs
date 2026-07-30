using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Radzen.Documents.Crypto;

namespace Radzen.Documents.Pdf.Objects.Encryption;

internal sealed class StandardSecurityHandler
{
    private static readonly byte[] Padding =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ];

    private static readonly byte[] AesSalt = [0x73, 0x41, 0x6C, 0x54];
    private static readonly byte[] ZeroIv = new byte[16];

    private readonly int revision;
    private readonly int version;
    private readonly byte[] ownerEntry;
    private readonly byte[] userEntry;
    private readonly byte[] ownerEncrypted;
    private readonly byte[] userEncrypted;
    private readonly byte[] permsEntry;
    private readonly bool hasPermsEntry;
    private readonly byte[] documentId;
    private readonly int permissions;
    private readonly int keyLength;
    private readonly bool encryptMetadata;
    private readonly CryptMethod streamCipher;
    private readonly CryptMethod stringCipher;

    public StandardSecurityHandler(DictionaryObject encrypt, byte[] documentId, byte[] password)
        : this(encrypt, documentId, Encoding.Latin1.GetString(password ?? throw new ArgumentNullException(nameof(password))))
    {
    }

    public StandardSecurityHandler(DictionaryObject encrypt, byte[] documentId, string password)
    {
        ArgumentNullException.ThrowIfNull(encrypt);
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(password);

        this.documentId = documentId;
        version = GetInt(encrypt, "V", 0);
        revision = GetInt(encrypt, "R", 0);
        permissions = GetInt(encrypt, "P", 0);
        ownerEntry = GetStringBytes(encrypt, "O") ?? [];
        userEntry = GetStringBytes(encrypt, "U") ?? [];
        ownerEncrypted = GetStringBytes(encrypt, "OE") ?? [];
        userEncrypted = GetStringBytes(encrypt, "UE") ?? [];
        hasPermsEntry = encrypt.TryGetValue("Perms", out _);
        permsEntry = GetStringBytes(encrypt, "Perms") ?? [];
        encryptMetadata = !(encrypt.TryGetValue("EncryptMetadata", out var meta)
            && meta is BooleanObject flag && !flag.Value);

        keyLength = version switch
        {
            1 => 5,
            5 => 32,
            >= 4 => DeriveCryptFilterKeyLength(encrypt),
            _ => DeriveMd5KeyLength(GetInt(encrypt, "Length", 40)),
        };

        streamCipher = ResolveCipher(encrypt, "StmF");
        stringCipher = ResolveCipher(encrypt, "StrF");
        FileKey = [];

        // ISO 32000-2 7.6.4.3.3: revision 6 passwords are SASLprep-normalized UTF-8.
        Authenticate(revision switch
        {
            5 => Encoding.UTF8.GetBytes(password),
            >= 6 => EncodeR6Password(password),
            _ => Encoding.Latin1.GetBytes(password),
        });
    }

    private enum CryptMethod
    {
        Identity,
        Rc4,
        AesV2,
        AesV3,
    }

    public byte[] FileKey { get; private set; }

    public bool IsUserPassword { get; private set; }

    public bool IsOwnerPassword { get; private set; }

    public ReadOnlyMemory<byte> DecryptStream(ReadOnlyMemory<byte> data, int objectNumber, int generation)
        => Decrypt(streamCipher, data, objectNumber, generation);

    // ISO 32000-1 7.6.3.2: a /Metadata stream stays plaintext when /EncryptMetadata is false.
    // ISO 32000-1 7.4.10: a /Crypt-first chain is decrypted by that filter, not /StmF.
    public ReadOnlyMemory<byte> DecryptStream(
        ReadOnlyMemory<byte> data, int objectNumber, int generation, DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return (!encryptMetadata && IsMetadataStream(dictionary)) || HasCryptFilter(dictionary)
            ? data
            : DecryptStream(data, objectNumber, generation);
    }

    internal static bool IsMetadataStream(DictionaryObject dictionary)
        => dictionary.TryGetValue("Type", out var type) && type is NameObject name
            && string.Equals(name.Value, "Metadata", StringComparison.Ordinal);

    // ISO 32000-1 7.4.10: /Crypt shall be the first filter in the chain.
    private static bool HasCryptFilter(DictionaryObject dictionary)
    {
        if (!dictionary.TryGetValue("Filter", out var filter))
        {
            return false;
        }

        var first = filter is ArrayObject array ? (array.Count > 0 ? array[0] : null) : filter;
        return first is NameObject name && string.Equals(name.Value, "Crypt", StringComparison.Ordinal);
    }

    public ReadOnlyMemory<byte> DecryptString(byte[] data, int objectNumber, int generation)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Decrypt(stringCipher, data, objectNumber, generation);
    }

    private ReadOnlyMemory<byte> Decrypt(
        CryptMethod cipher, ReadOnlyMemory<byte> data, int objectNumber, int generation)
    {
        if (data.Length == 0 || cipher == CryptMethod.Identity)
        {
            return data;
        }

        var bytes = data.ToArray();
        return cipher switch
        {
            CryptMethod.AesV3 => ParseAes.Decrypt(FileKey, bytes),
            CryptMethod.AesV2 => ParseAes.Decrypt(ObjectKey(objectNumber, generation, aes: true), bytes),
            _ => Rc4.Transform(ObjectKey(objectNumber, generation, aes: false), bytes),
        };
    }

    // ISO 32000-1 7.6.5: /StmF and /StrF name the crypt filter in /CF; /Identity means no encryption.
    private CryptMethod ResolveCipher(DictionaryObject encrypt, string selector)
    {
        if (version < 4)
        {
            return CryptMethod.Rc4;
        }

        var filter = ResolveCryptFilter(encrypt, selector, out var filterName);
        if (string.Equals(filterName, "Identity", StringComparison.Ordinal))
        {
            return CryptMethod.Identity;
        }

        if (encrypt.TryGetValue("CF", out var cf) && cf is DictionaryObject)
        {
            if (filter is not null && filter.TryGetValue("CFM", out var cfm) && cfm is NameObject method)
            {
                return method.Value switch
                {
                    "AESV3" => CryptMethod.AesV3,
                    "AESV2" => CryptMethod.AesV2,
                    "V2" => CryptMethod.Rc4,
                    "Identity" => CryptMethod.Identity,
                    _ => throw new DocumentParseException(
                        $"Unsupported crypt filter method /CFM /{method.Value}."),
                };
            }

            throw new DocumentParseException($"Crypt filter '{filterName}' is not defined in /CF.");
        }

        return version == 5 ? CryptMethod.AesV3 : CryptMethod.Rc4;
    }

    private void Authenticate(byte[] password)
    {
        if (revision >= 5)
        {
            AuthenticateR6(password);
            return;
        }

        var userKey = ComputeFileKey(password);
        if (UserPasswordMatches(userKey))
        {
            FileKey = userKey;
            IsUserPassword = true;
            return;
        }

        var recovered = RecoverUserPassword(password);
        var ownerKey = ComputeFileKey(recovered);
        if (UserPasswordMatches(ownerKey))
        {
            FileKey = ownerKey;
            IsOwnerPassword = true;
            return;
        }

        FileKey = userKey;
    }

    private byte[] ComputeFileKey(byte[] password)
        => ComputeFileKey(password, ownerEntry, permissions, documentId, revision, keyLength, encryptMetadata);

    // ISO 32000-1 algorithm 6. R2 /U is the full 32-byte RC4 block; for R >= 3 only the leading 16 bytes are defined.
    private bool UserPasswordMatches(byte[] fileKey)
        => Equal(ComputeUserEntry(fileKey, documentId, revision), userEntry, revision == 2 ? 32 : 16);

    // ISO 32000-1 algorithm 7: recover the (padded) user password from /O.
    private byte[] RecoverUserPassword(byte[] password)
    {
        var rc4Key = DeriveKey(Pad(password), revision, keyLength);
        var oBytes = ownerEntry.Length >= 32 ? ownerEntry[..32] : ownerEntry;
        if (revision == 2)
        {
            return Rc4.Transform(rc4Key, oBytes);
        }

        var value = oBytes;
        for (var i = 19; i >= 0; i--)
        {
            value = Rc4.Transform(Xor(rc4Key, i), value);
        }

        return value;
    }

    // ISO 32000-2 algorithm 2.A.
    private void AuthenticateR6(byte[] password)
    {
        var pw = password.Length > 127 ? password[..127] : password;
        var u = userEntry;
        var o = ownerEntry;

        if (u.Length >= 48)
        {
            var userHash = HashPassword(pw, u[32..40], []);
            if (Equal(userHash, u, 32))
            {
                IsUserPassword = true;
                var intermediate = HashPassword(pw, u[40..48], []);
                FileKey = RequireAes256Key(ParseAes.DecryptCbcNoPadding(intermediate, ZeroIv, userEncrypted));
                VerifyPerms();
                return;
            }
        }

        if (o.Length >= 48 && u.Length >= 48)
        {
            var ownerHash = HashPassword(pw, o[32..40], u[..48]);
            if (Equal(ownerHash, o, 32))
            {
                IsOwnerPassword = true;
                var intermediate = HashPassword(pw, o[40..48], u[..48]);
                FileKey = RequireAes256Key(ParseAes.DecryptCbcNoPadding(intermediate, ZeroIv, ownerEncrypted));
                VerifyPerms();
                return;
            }
        }
    }

    // ISO 32000-2 algorithm 13: decrypt /Perms with the file key and validate its permission binding.
    private void VerifyPerms()
    {
        if (!hasPermsEntry)
        {
            return;
        }

        if (permsEntry.Length != 16)
        {
            throw new DocumentParseException("Encryption /Perms must be exactly 16 bytes.");
        }

        var decoded = ParseAes.DecryptCbcNoPadding(FileKey, ZeroIv, permsEntry);
        if (decoded[9] != (byte)'a' || decoded[10] != (byte)'d' || decoded[11] != (byte)'b')
        {
            throw new DocumentParseException("Encryption /Perms block failed its integrity check.");
        }

        var embedded = decoded[0] | (decoded[1] << 8) | (decoded[2] << 16) | (decoded[3] << 24);
        if (embedded != permissions)
        {
            throw new DocumentParseException("Encryption /Perms permissions do not match /P.");
        }

        if (decoded[4] != 0xFF || decoded[5] != 0xFF || decoded[6] != 0xFF || decoded[7] != 0xFF)
        {
            throw new DocumentParseException("Encryption /Perms reserved bytes are invalid.");
        }

        var metadataFlag = encryptMetadata ? (byte)'T' : (byte)'F';
        if (decoded[8] != metadataFlag)
        {
            throw new DocumentParseException("Encryption /Perms metadata flag does not match /EncryptMetadata.");
        }
    }

    // ISO 32000-2 7.6.4.3.3: R6 passwords are SASLprep-prepared (RFC 4013) before UTF-8.
    private static byte[] EncodeR6Password(string password)
    {
        var mapped = MapForSaslprep(password);
        var normalized = mapped.Normalize(NormalizationForm.FormKC);
        RejectProhibited(normalized);
        RejectBidiViolation(normalized);
        return Encoding.UTF8.GetBytes(normalized);
    }

    private static string MapForSaslprep(string password)
    {
        var builder = new StringBuilder(password.Length);
        foreach (var rune in password.EnumerateRunes())
        {
            if (InAnyRange(rune.Value, SaslprepMappedToNothing))
            {
                continue;
            }

            builder.Append(InAnyRange(rune.Value, SaslprepNonAsciiSpace) ? " " : rune.ToString());
        }

        return builder.ToString();
    }

    private static void RejectProhibited(string value)
    {
        foreach (var rune in value.EnumerateRunes())
        {
            if (InAnyRange(rune.Value, SaslprepProhibited))
            {
                throw new DocumentParseException(
                    "Revision 6 password contains a code point prohibited by the SASLprep profile.");
            }
        }
    }

    // RFC 3454 6: a RandALCat string contains no LCat character and begins and ends with RandALCat.
    private static void RejectBidiViolation(string value)
    {
        Rune? first = null;
        var last = default(Rune);
        var hasRandAl = false;
        var hasL = false;
        foreach (var rune in value.EnumerateRunes())
        {
            first ??= rune;
            last = rune;
            hasRandAl |= InAnyRange(rune.Value, SaslprepRandAlCat);
            hasL |= IsLCat(rune);
        }

        if (hasRandAl && (hasL || first is { } start
            && !(InAnyRange(start.Value, SaslprepRandAlCat) && InAnyRange(last.Value, SaslprepRandAlCat))))
        {
            throw new DocumentParseException(
                "Revision 6 password violates the SASLprep bidirectional rule.");
        }
    }

    private static bool IsLCat(Rune rune) => InAnyRange(rune.Value, SaslprepLCat);

    private static bool InAnyRange(int codePoint, int[] ranges)
    {
        for (var i = 0; i < ranges.Length; i += 2)
        {
            if (codePoint >= ranges[i] && codePoint <= ranges[i + 1])
            {
                return true;
            }
        }

        return false;
    }

    // RFC 3454 table B.1 (commonly mapped to nothing).
    private static readonly int[] SaslprepMappedToNothing =
    [
        0x00AD, 0x00AD, 0x034F, 0x034F, 0x1806, 0x1806, 0x180B, 0x180D,
        0x200B, 0x200D, 0x2060, 0x2060, 0xFE00, 0xFE0F, 0xFEFF, 0xFEFF,
    ];

    // RFC 3454 table C.1.2 (non-ASCII space characters).
    private static readonly int[] SaslprepNonAsciiSpace =
    [
        0x00A0, 0x00A0, 0x1680, 0x1680, 0x2000, 0x200A,
        0x202F, 0x202F, 0x205F, 0x205F, 0x3000, 0x3000,
    ];

    // RFC 3454 tables C.2.1, C.2.2, C.3, C.4, C.5, C.6, C.7, C.8, C.9 (prohibited output).
    private static readonly int[] SaslprepProhibited =
    [
        0x0000, 0x001F, 0x007F, 0x009F,
        0x0340, 0x0341, 0x06DD, 0x06DD, 0x070F, 0x070F, 0x180E, 0x180E,
        0x200C, 0x200F, 0x2028, 0x2029, 0x202A, 0x202E, 0x2060, 0x2063, 0x206A, 0x206F,
        0x2FF0, 0x2FFB, 0xD800, 0xDFFF, 0xE000, 0xF8FF,
        0xFDD0, 0xFDEF, 0xFEFF, 0xFEFF, 0xFFF9, 0xFFFF,
        0x1D173, 0x1D17A,
        0x1FFFE, 0x1FFFF, 0x2FFFE, 0x2FFFF, 0x3FFFE, 0x3FFFF, 0x4FFFE, 0x4FFFF,
        0x5FFFE, 0x5FFFF, 0x6FFFE, 0x6FFFF, 0x7FFFE, 0x7FFFF, 0x8FFFE, 0x8FFFF,
        0x9FFFE, 0x9FFFF, 0xAFFFE, 0xAFFFF, 0xBFFFE, 0xBFFFF, 0xCFFFE, 0xCFFFF,
        0xDFFFE, 0xDFFFF, 0xEFFFE, 0xEFFFF, 0xE0001, 0xE0001, 0xE0020, 0xE007F,
        0xF0000, 0xFFFFD, 0xFFFFE, 0xFFFFF, 0x100000, 0x10FFFD, 0x10FFFE, 0x10FFFF,
    ];

    // RFC 3454 table D.2 (bidirectional property L).
    private static readonly int[] SaslprepLCat =
    [
        0x0041, 0x005A, 0x0061, 0x007A, 0x00AA, 0x00AA, 0x00B5, 0x00B5,
        0x00BA, 0x00BA, 0x00C0, 0x00D6, 0x00D8, 0x00F6, 0x00F8, 0x0220,
        0x0222, 0x0233, 0x0250, 0x02AD, 0x02B0, 0x02B8, 0x02BB, 0x02C1,
        0x02D0, 0x02D1, 0x02E0, 0x02E4, 0x02EE, 0x02EE, 0x037A, 0x037A,
        0x0386, 0x0386, 0x0388, 0x038A, 0x038C, 0x038C, 0x038E, 0x03A1,
        0x03A3, 0x03CE, 0x03D0, 0x03F5, 0x0400, 0x0482, 0x048A, 0x04CE,
        0x04D0, 0x04F5, 0x04F8, 0x04F9, 0x0500, 0x050F, 0x0531, 0x0556,
        0x0559, 0x055F, 0x0561, 0x0587, 0x0589, 0x0589, 0x0903, 0x0903,
        0x0905, 0x0939, 0x093D, 0x0940, 0x0949, 0x094C, 0x0950, 0x0950,
        0x0958, 0x0961, 0x0964, 0x0970, 0x0982, 0x0983, 0x0985, 0x098C,
        0x098F, 0x0990, 0x0993, 0x09A8, 0x09AA, 0x09B0, 0x09B2, 0x09B2,
        0x09B6, 0x09B9, 0x09BE, 0x09C0, 0x09C7, 0x09C8, 0x09CB, 0x09CC,
        0x09D7, 0x09D7, 0x09DC, 0x09DD, 0x09DF, 0x09E1, 0x09E6, 0x09F1,
        0x09F4, 0x09FA, 0x0A05, 0x0A0A, 0x0A0F, 0x0A10, 0x0A13, 0x0A28,
        0x0A2A, 0x0A30, 0x0A32, 0x0A33, 0x0A35, 0x0A36, 0x0A38, 0x0A39,
        0x0A3E, 0x0A40, 0x0A59, 0x0A5C, 0x0A5E, 0x0A5E, 0x0A66, 0x0A6F,
        0x0A72, 0x0A74, 0x0A83, 0x0A83, 0x0A85, 0x0A8B, 0x0A8D, 0x0A8D,
        0x0A8F, 0x0A91, 0x0A93, 0x0AA8, 0x0AAA, 0x0AB0, 0x0AB2, 0x0AB3,
        0x0AB5, 0x0AB9, 0x0ABD, 0x0AC0, 0x0AC9, 0x0AC9, 0x0ACB, 0x0ACC,
        0x0AD0, 0x0AD0, 0x0AE0, 0x0AE0, 0x0AE6, 0x0AEF, 0x0B02, 0x0B03,
        0x0B05, 0x0B0C, 0x0B0F, 0x0B10, 0x0B13, 0x0B28, 0x0B2A, 0x0B30,
        0x0B32, 0x0B33, 0x0B36, 0x0B39, 0x0B3D, 0x0B3E, 0x0B40, 0x0B40,
        0x0B47, 0x0B48, 0x0B4B, 0x0B4C, 0x0B57, 0x0B57, 0x0B5C, 0x0B5D,
        0x0B5F, 0x0B61, 0x0B66, 0x0B70, 0x0B83, 0x0B83, 0x0B85, 0x0B8A,
        0x0B8E, 0x0B90, 0x0B92, 0x0B95, 0x0B99, 0x0B9A, 0x0B9C, 0x0B9C,
        0x0B9E, 0x0B9F, 0x0BA3, 0x0BA4, 0x0BA8, 0x0BAA, 0x0BAE, 0x0BB5,
        0x0BB7, 0x0BB9, 0x0BBE, 0x0BBF, 0x0BC1, 0x0BC2, 0x0BC6, 0x0BC8,
        0x0BCA, 0x0BCC, 0x0BD7, 0x0BD7, 0x0BE7, 0x0BF2, 0x0C01, 0x0C03,
        0x0C05, 0x0C0C, 0x0C0E, 0x0C10, 0x0C12, 0x0C28, 0x0C2A, 0x0C33,
        0x0C35, 0x0C39, 0x0C41, 0x0C44, 0x0C60, 0x0C61, 0x0C66, 0x0C6F,
        0x0C82, 0x0C83, 0x0C85, 0x0C8C, 0x0C8E, 0x0C90, 0x0C92, 0x0CA8,
        0x0CAA, 0x0CB3, 0x0CB5, 0x0CB9, 0x0CBE, 0x0CBE, 0x0CC0, 0x0CC4,
        0x0CC7, 0x0CC8, 0x0CCA, 0x0CCB, 0x0CD5, 0x0CD6, 0x0CDE, 0x0CDE,
        0x0CE0, 0x0CE1, 0x0CE6, 0x0CEF, 0x0D02, 0x0D03, 0x0D05, 0x0D0C,
        0x0D0E, 0x0D10, 0x0D12, 0x0D28, 0x0D2A, 0x0D39, 0x0D3E, 0x0D40,
        0x0D46, 0x0D48, 0x0D4A, 0x0D4C, 0x0D57, 0x0D57, 0x0D60, 0x0D61,
        0x0D66, 0x0D6F, 0x0D82, 0x0D83, 0x0D85, 0x0D96, 0x0D9A, 0x0DB1,
        0x0DB3, 0x0DBB, 0x0DBD, 0x0DBD, 0x0DC0, 0x0DC6, 0x0DCF, 0x0DD1,
        0x0DD8, 0x0DDF, 0x0DF2, 0x0DF4, 0x0E01, 0x0E30, 0x0E32, 0x0E33,
        0x0E40, 0x0E46, 0x0E4F, 0x0E5B, 0x0E81, 0x0E82, 0x0E84, 0x0E84,
        0x0E87, 0x0E88, 0x0E8A, 0x0E8A, 0x0E8D, 0x0E8D, 0x0E94, 0x0E97,
        0x0E99, 0x0E9F, 0x0EA1, 0x0EA3, 0x0EA5, 0x0EA5, 0x0EA7, 0x0EA7,
        0x0EAA, 0x0EAB, 0x0EAD, 0x0EB0, 0x0EB2, 0x0EB3, 0x0EBD, 0x0EBD,
        0x0EC0, 0x0EC4, 0x0EC6, 0x0EC6, 0x0ED0, 0x0ED9, 0x0EDC, 0x0EDD,
        0x0F00, 0x0F17, 0x0F1A, 0x0F34, 0x0F36, 0x0F36, 0x0F38, 0x0F38,
        0x0F3E, 0x0F47, 0x0F49, 0x0F6A, 0x0F7F, 0x0F7F, 0x0F85, 0x0F85,
        0x0F88, 0x0F8B, 0x0FBE, 0x0FC5, 0x0FC7, 0x0FCC, 0x0FCF, 0x0FCF,
        0x1000, 0x1021, 0x1023, 0x1027, 0x1029, 0x102A, 0x102C, 0x102C,
        0x1031, 0x1031, 0x1038, 0x1038, 0x1040, 0x1057, 0x10A0, 0x10C5,
        0x10D0, 0x10F8, 0x10FB, 0x10FB, 0x1100, 0x1159, 0x115F, 0x11A2,
        0x11A8, 0x11F9, 0x1200, 0x1206, 0x1208, 0x1246, 0x1248, 0x1248,
        0x124A, 0x124D, 0x1250, 0x1256, 0x1258, 0x1258, 0x125A, 0x125D,
        0x1260, 0x1286, 0x1288, 0x1288, 0x128A, 0x128D, 0x1290, 0x12AE,
        0x12B0, 0x12B0, 0x12B2, 0x12B5, 0x12B8, 0x12BE, 0x12C0, 0x12C0,
        0x12C2, 0x12C5, 0x12C8, 0x12CE, 0x12D0, 0x12D6, 0x12D8, 0x12EE,
        0x12F0, 0x130E, 0x1310, 0x1310, 0x1312, 0x1315, 0x1318, 0x131E,
        0x1320, 0x1346, 0x1348, 0x135A, 0x1361, 0x137C, 0x13A0, 0x13F4,
        0x1401, 0x1676, 0x1681, 0x169A, 0x16A0, 0x16F0, 0x1700, 0x170C,
        0x170E, 0x1711, 0x1720, 0x1731, 0x1735, 0x1736, 0x1740, 0x1751,
        0x1760, 0x176C, 0x176E, 0x1770, 0x1780, 0x17B6, 0x17BE, 0x17C5,
        0x17C7, 0x17C8, 0x17D4, 0x17DA, 0x17DC, 0x17DC, 0x17E0, 0x17E9,
        0x1810, 0x1819, 0x1820, 0x1877, 0x1880, 0x18A8, 0x1E00, 0x1E9B,
        0x1EA0, 0x1EF9, 0x1F00, 0x1F15, 0x1F18, 0x1F1D, 0x1F20, 0x1F45,
        0x1F48, 0x1F4D, 0x1F50, 0x1F57, 0x1F59, 0x1F59, 0x1F5B, 0x1F5B,
        0x1F5D, 0x1F5D, 0x1F5F, 0x1F7D, 0x1F80, 0x1FB4, 0x1FB6, 0x1FBC,
        0x1FBE, 0x1FBE, 0x1FC2, 0x1FC4, 0x1FC6, 0x1FCC, 0x1FD0, 0x1FD3,
        0x1FD6, 0x1FDB, 0x1FE0, 0x1FEC, 0x1FF2, 0x1FF4, 0x1FF6, 0x1FFC,
        0x200E, 0x200E, 0x2071, 0x2071, 0x207F, 0x207F, 0x2102, 0x2102,
        0x2107, 0x2107, 0x210A, 0x2113, 0x2115, 0x2115, 0x2119, 0x211D,
        0x2124, 0x2124, 0x2126, 0x2126, 0x2128, 0x2128, 0x212A, 0x212D,
        0x212F, 0x2131, 0x2133, 0x2139, 0x213D, 0x213F, 0x2145, 0x2149,
        0x2160, 0x2183, 0x2336, 0x237A, 0x2395, 0x2395, 0x249C, 0x24E9,
        0x3005, 0x3007, 0x3021, 0x3029, 0x3031, 0x3035, 0x3038, 0x303C,
        0x3041, 0x3096, 0x309D, 0x309F, 0x30A1, 0x30FA, 0x30FC, 0x30FF,
        0x3105, 0x312C, 0x3131, 0x318E, 0x3190, 0x31B7, 0x31F0, 0x321C,
        0x3220, 0x3243, 0x3260, 0x327B, 0x327F, 0x32B0, 0x32C0, 0x32CB,
        0x32D0, 0x32FE, 0x3300, 0x3376, 0x337B, 0x33DD, 0x33E0, 0x33FE,
        0x3400, 0x4DB5, 0x4E00, 0x9FA5, 0xA000, 0xA48C, 0xAC00, 0xD7A3,
        0xD800, 0xFA2D, 0xFA30, 0xFA6A, 0xFB00, 0xFB06, 0xFB13, 0xFB17,
        0xFF21, 0xFF3A, 0xFF41, 0xFF5A, 0xFF66, 0xFFBE, 0xFFC2, 0xFFC7,
        0xFFCA, 0xFFCF, 0xFFD2, 0xFFD7, 0xFFDA, 0xFFDC, 0x10300, 0x1031E,
        0x10320, 0x10323, 0x10330, 0x1034A, 0x10400, 0x10425, 0x10428, 0x1044D,
        0x1D000, 0x1D0F5, 0x1D100, 0x1D126, 0x1D12A, 0x1D166, 0x1D16A, 0x1D172,
        0x1D183, 0x1D184, 0x1D18C, 0x1D1A9, 0x1D1AE, 0x1D1DD, 0x1D400, 0x1D454,
        0x1D456, 0x1D49C, 0x1D49E, 0x1D49F, 0x1D4A2, 0x1D4A2, 0x1D4A5, 0x1D4A6,
        0x1D4A9, 0x1D4AC, 0x1D4AE, 0x1D4B9, 0x1D4BB, 0x1D4BB, 0x1D4BD, 0x1D4C0,
        0x1D4C2, 0x1D4C3, 0x1D4C5, 0x1D505, 0x1D507, 0x1D50A, 0x1D50D, 0x1D514,
        0x1D516, 0x1D51C, 0x1D51E, 0x1D539, 0x1D53B, 0x1D53E, 0x1D540, 0x1D544,
        0x1D546, 0x1D546, 0x1D54A, 0x1D550, 0x1D552, 0x1D6A3, 0x1D6A8, 0x1D7C9,
        0x20000, 0x2A6D6, 0x2F800, 0x2FA1D, 0xF0000, 0xFFFFD, 0x100000, 0x10FFFD,
    ];

    // RFC 3454 table D.1 (bidirectional property R or AL).
    private static readonly int[] SaslprepRandAlCat =
    [
        0x05BE, 0x05BE, 0x05C0, 0x05C0, 0x05C3, 0x05C3, 0x05D0, 0x05EA,
        0x05F0, 0x05F4, 0x061B, 0x061B, 0x061F, 0x061F, 0x0621, 0x063A,
        0x0640, 0x064A, 0x066D, 0x066F, 0x0671, 0x06D5, 0x06DD, 0x06DD,
        0x06E5, 0x06E6, 0x06FA, 0x06FE, 0x0700, 0x070D, 0x0710, 0x0710,
        0x0712, 0x072C, 0x0780, 0x07A5, 0x07B1, 0x07B1, 0x200F, 0x200F,
        0xFB1D, 0xFB1D, 0xFB1F, 0xFB28, 0xFB2A, 0xFB36, 0xFB38, 0xFB3C,
        0xFB3E, 0xFB3E, 0xFB40, 0xFB41, 0xFB43, 0xFB44, 0xFB46, 0xFBB1,
        0xFBD3, 0xFD3D, 0xFD50, 0xFD8F, 0xFD92, 0xFDC7, 0xFDF0, 0xFDFC,
        0xFE70, 0xFE74, 0xFE76, 0xFEFC,
    ];

    private static byte[] RequireAes256Key(byte[] fileKey)
        => fileKey.Length == 32
            ? fileKey
            : throw new DocumentParseException("Revision 6 file key must be exactly 32 bytes.");

    // ISO 32000-2 algorithm 2.B: iterated for revision 6; revision 5 uses a single SHA-256 pass.
    private byte[] HashPassword(byte[] password, byte[] salt, byte[] userData)
        => revision == 5
            ? Sha2.ComputeHash256(Concat(password, salt, userData))
            : Hash2B(password, salt, userData);

    // ISO 32000-2 algorithm 2.B.
    private static byte[] Hash2B(byte[] password, byte[] salt, byte[] userData)
    {
        var input = Concat(password, salt, userData);
        var k = Sha2.ComputeHash256(input);
        var round = 0;
        while (true)
        {
            var block = Concat(password, k, userData);
            var k1 = new byte[block.Length * 64];
            for (var i = 0; i < 64; i++)
            {
                Array.Copy(block, 0, k1, i * block.Length, block.Length);
            }

            var e = AesCbc.EncryptCbcNoPadding(k[..16], k[16..32], k1);
            var mod = 0;
            for (var i = 0; i < 16; i++)
            {
                mod = (mod + e[i]) % 3;
            }

            k = mod switch
            {
                0 => Sha2.ComputeHash256(e),
                1 => Sha2.ComputeHash384(e),
                _ => Sha2.ComputeHash512(e),
            };

            round++;
            if (round >= 64 && (e[^1] & 0xFF) <= round - 32)
            {
                break;
            }
        }

        return k[..32];
    }

    private byte[] ObjectKey(int objectNumber, int generation, bool aes)
        => ComputeObjectKey(FileKey, objectNumber, generation, aes);

    // ISO 32000-1 algorithm 1: per-object key from the file key, object number and generation (plus "sAlT" for AESV2).
    internal static byte[] ComputeObjectKey(byte[] fileKey, int objectNumber, int generation, bool aes)
    {
        var extra = aes ? AesSalt.Length : 0;
        var buffer = new byte[fileKey.Length + 5 + extra];
        Array.Copy(fileKey, buffer, fileKey.Length);
        var pos = fileKey.Length;
        buffer[pos++] = (byte)objectNumber;
        buffer[pos++] = (byte)(objectNumber >> 8);
        buffer[pos++] = (byte)(objectNumber >> 16);
        buffer[pos++] = (byte)generation;
        buffer[pos++] = (byte)(generation >> 8);
        if (aes)
        {
            Array.Copy(AesSalt, 0, buffer, pos, AesSalt.Length);
        }

        var hash = Md5.ComputeHash(buffer);
        var length = Math.Min(fileKey.Length + 5, 16);
        return hash[..length];
    }

    internal static (byte[] Owner, byte[] User, byte[] FileKey) DeriveLegacy(
        string userPassword, string ownerPassword, int revision, int keyLength,
        int permissions, byte[] documentId, bool encryptMetadata)
    {
        var userBytes = Encoding.Latin1.GetBytes(userPassword);
        var ownerBytes = Encoding.Latin1.GetBytes(ownerPassword.Length > 0 ? ownerPassword : userPassword);
        var owner = ComputeOwnerEntry(ownerBytes, userBytes, revision, keyLength);
        var fileKey = ComputeFileKey(userBytes, owner, permissions, documentId, revision, keyLength, encryptMetadata);
        var user = ComputeUserEntry(fileKey, documentId, revision);
        return (owner, user, fileKey);
    }

    // ISO 32000-2 algorithms 8-10: derive /O, /U, /OE, /UE and /Perms for the AESV3 (R6) handler.
    internal static (byte[] Owner, byte[] User, byte[] OwnerEncrypted, byte[] UserEncrypted, byte[] Perms) DeriveAes256(
        string userPassword, string ownerPassword, byte[] fileKey, int permissions, bool encryptMetadata,
        byte[] userValidation, byte[] userKeySalt, byte[] ownerValidation, byte[] ownerKeySalt, byte[] permsNoise)
    {
        var userPw = TruncateUtf8(userPassword);
        var ownerPw = TruncateUtf8(ownerPassword.Length > 0 ? ownerPassword : userPassword);

        var user = Concat(Hash2B(userPw, userValidation, []), userValidation, userKeySalt);
        var userEncrypted = AesCbc.EncryptCbcNoPadding(Hash2B(userPw, userKeySalt, []), ZeroIv, fileKey);

        var owner = Concat(Hash2B(ownerPw, ownerValidation, user), ownerValidation, ownerKeySalt);
        var ownerEncrypted = AesCbc.EncryptCbcNoPadding(Hash2B(ownerPw, ownerKeySalt, user), ZeroIv, fileKey);

        return (owner, user, ownerEncrypted, userEncrypted, ComputePerms(permissions, encryptMetadata, fileKey, permsNoise));
    }

    private static byte[] TruncateUtf8(string password)
    {
        var bytes = EncodeR6Password(password);
        return bytes.Length > 127 ? bytes[..127] : bytes;
    }

    private static byte[] DeriveKey(byte[] seed, int revision, int keyLength)
    {
        var hash = Md5.ComputeHash(seed);
        if (revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = Md5.ComputeHash(hash[..keyLength]);
            }
        }

        return hash[..keyLength];
    }

    // ISO 32000-1 algorithm 3.
    private static byte[] ComputeOwnerEntry(byte[] ownerPassword, byte[] userPassword, int revision, int keyLength)
    {
        var rc4Key = DeriveKey(Pad(ownerPassword), revision, keyLength);
        var value = Rc4.Transform(rc4Key, Pad(userPassword));
        if (revision >= 3)
        {
            for (var i = 1; i <= 19; i++)
            {
                value = Rc4.Transform(Xor(rc4Key, i), value);
            }
        }

        return value;
    }

    // ISO 32000-1 algorithm 2. A short /O is zero-filled to the 32 bytes the layout reserves for it.
    private static byte[] ComputeFileKey(
        byte[] userPassword, byte[] owner, int permissions, byte[] documentId,
        int revision, int keyLength, bool encryptMetadata)
    {
        var padded = Pad(userPassword);
        var extra = revision >= 4 && !encryptMetadata ? 4 : 0;
        var buffer = new byte[padded.Length + 32 + 4 + documentId.Length + extra];
        var pos = 0;
        Array.Copy(padded, 0, buffer, pos, padded.Length);
        pos += padded.Length;
        var oBytes = owner.Length >= 32 ? owner[..32] : owner;
        Array.Copy(oBytes, 0, buffer, pos, oBytes.Length);
        pos += 32;
        buffer[pos++] = (byte)permissions;
        buffer[pos++] = (byte)(permissions >> 8);
        buffer[pos++] = (byte)(permissions >> 16);
        buffer[pos++] = (byte)(permissions >> 24);
        Array.Copy(documentId, 0, buffer, pos, documentId.Length);
        pos += documentId.Length;
        for (var i = 0; i < extra; i++)
        {
            buffer[pos + i] = 0xFF;
        }

        return DeriveKey(buffer, revision, keyLength);
    }

    // ISO 32000-1 algorithm 4 (R2) and algorithm 5 (R >= 3, padded to 32 bytes).
    private static byte[] ComputeUserEntry(byte[] fileKey, byte[] documentId, int revision)
    {
        if (revision == 2)
        {
            return Rc4.Transform(fileKey, Padding);
        }

        var seed = new byte[Padding.Length + documentId.Length];
        Array.Copy(Padding, seed, Padding.Length);
        Array.Copy(documentId, 0, seed, Padding.Length, documentId.Length);
        var value = Rc4.Transform(fileKey, Md5.ComputeHash(seed));
        for (var i = 1; i <= 19; i++)
        {
            value = Rc4.Transform(Xor(fileKey, i), value);
        }

        var result = new byte[32];
        Array.Copy(value, result, 16);
        return result;
    }

    // ISO 32000-2 algorithm 10: the /Perms block is a single AES-256 ECB block (CBC with a zero IV).
    private static byte[] ComputePerms(int permissions, bool encryptMetadata, byte[] fileKey, byte[] noise)
    {
        var perms = new byte[16];
        perms[0] = (byte)permissions;
        perms[1] = (byte)(permissions >> 8);
        perms[2] = (byte)(permissions >> 16);
        perms[3] = (byte)(permissions >> 24);
        perms[4] = 0xFF;
        perms[5] = 0xFF;
        perms[6] = 0xFF;
        perms[7] = 0xFF;
        perms[8] = (byte)(encryptMetadata ? 'T' : 'F');
        perms[9] = (byte)'a';
        perms[10] = (byte)'d';
        perms[11] = (byte)'b';
        Array.Copy(noise, 0, perms, 12, 4);
        return AesCbc.EncryptCbcNoPadding(fileKey, ZeroIv, perms);
    }

    // ISO 32000-1 7.6.5: for V>=4 the file-key length comes from the crypt-filter dictionary. AESV2/AESV3 fix it at 16/32 bytes.
    private int DeriveCryptFilterKeyLength(DictionaryObject encrypt)
    {
        var filter = ResolveCryptFilter(encrypt, "StmF", out _);
        if (filter is not null && filter.TryGetValue("CFM", out var cfm) && cfm is NameObject method)
        {
            switch (method.Value)
            {
                case "AESV3":
                    return 32;
                case "AESV2":
                    return 16;
            }
        }

        if (filter is not null && filter.TryGetValue("Length", out var length) && length is NumberObject number)
        {
            // ISO 32000-1 Table 25: the crypt-filter /Length is in bytes; treat anything above 16 as a bit count.
            var value = number.IntValue;
            return DeriveMd5KeyLength(value > 16 ? value : value * 8);
        }

        return DeriveMd5KeyLength(GetInt(encrypt, "Length", 40));
    }

    private static DictionaryObject? ResolveCryptFilter(DictionaryObject encrypt, string selector, out string filterName)
    {
        filterName = encrypt.TryGetValue(selector, out var selected) && selected is NameObject chosen
            ? chosen.Value
            : "StdCF";
        if (string.Equals(filterName, "Identity", StringComparison.Ordinal))
        {
            return null;
        }

        return encrypt.TryGetValue("CF", out var cf) && cf is DictionaryObject cfDict
            && cfDict.TryGetValue(filterName, out var filter) && filter is DictionaryObject filterDict
            ? filterDict
            : null;
    }

    // ISO 32000-1 7.6.3.3: RC4/AES key sizes are 5..16 bytes; anything else is a malformed dictionary.
    private static int DeriveMd5KeyLength(int lengthBits)
    {
        var bytes = lengthBits / 8;
        if (bytes is < 5 or > 16)
        {
            throw new DocumentParseException("Encryption /Length is out of the permitted 40..128 bit range.");
        }

        return bytes;
    }

    private static byte[] Pad(byte[] password)
    {
        var result = new byte[32];
        var count = Math.Min(password.Length, 32);
        Array.Copy(password, result, count);
        Array.Copy(Padding, 0, result, count, 32 - count);
        return result;
    }

    private static byte[] Xor(byte[] key, int value)
    {
        var result = new byte[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            result[i] = (byte)(key[i] ^ value);
        }

        return result;
    }

    private static bool Equal(byte[] left, byte[] right, int count)
        => left.Length >= count
            && right.Length >= count
#pragma warning disable RS0030
            && CryptographicOperations.FixedTimeEquals(left.AsSpan(0, count), right.AsSpan(0, count));
#pragma warning restore RS0030

    private static byte[] Concat(byte[] a, byte[] b, byte[] c)
    {
        var result = new byte[a.Length + b.Length + c.Length];
        Array.Copy(a, 0, result, 0, a.Length);
        Array.Copy(b, 0, result, a.Length, b.Length);
        Array.Copy(c, 0, result, a.Length + b.Length, c.Length);
        return result;
    }

    private static int GetInt(DictionaryObject dictionary, string key, int fallback)
        => dictionary.TryGetValue(key, out var value) && value is NumberObject number ? number.IntValue : fallback;

    private static byte[]? GetStringBytes(DictionaryObject dictionary, string key)
        => dictionary.TryGetValue(key, out var value) && value is StringObject text
            ? Encoding.Latin1.GetBytes(text.Value)
            : null;

    private static class ParseAes
    {
        // ISO 32000-1 7.6.2: AESV2 and AESV3 strings and streams begin with the 16-byte initialization vector.
        public static byte[] Decrypt(byte[] key, byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            if (data.Length < 16)
            {
                throw new DocumentParseException("AES data is shorter than the required 16-byte IV.");
            }

            return Guard(() => AesCbc.Decrypt(key, data[..16], data[16..]));
        }

        public static byte[] DecryptCbcNoPadding(byte[] key, byte[] iv, byte[] data)
            => Guard(() => AesCbc.DecryptCbcNoPadding(key, iv, data));

        private static byte[] Guard(Func<byte[]> decrypt)
        {
            try
            {
                return decrypt();
            }
            catch (InvalidDataException exception)
            {
                throw new DocumentParseException(exception.Message);
            }
        }
    }
}
