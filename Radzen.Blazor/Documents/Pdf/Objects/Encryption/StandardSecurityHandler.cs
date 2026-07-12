using System;
using System.Security.Cryptography;
using System.Text;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Implements the ISO 32000-1 standard security handler (revisions 2-4) and the
/// ISO 32000-2 revision 6 (AESV3) key derivation and password authentication.
/// Given the /Encrypt dictionary, the document /ID and a candidate password it
/// derives the file encryption key and decrypts strings and streams.
/// </summary>
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

        // Revision 6 passwords are UTF-8 (ISO 32000-2 7.6.4.3.3); earlier
        // revisions use the PDFDocEncoding/Latin-1 byte interpretation.
        Authenticate(revision >= 5 ? Encoding.UTF8.GetBytes(password) : Encoding.Latin1.GetBytes(password));
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

    public byte[] DecryptStream(byte[] data, int objectNumber, int generation)
        => Decrypt(streamCipher, data, objectNumber, generation);

    // A /Metadata stream stays plaintext when /EncryptMetadata is false (ISO 32000-1
    // 7.6.3.2); running it through the cipher would return corrupted XMP.
    public byte[] DecryptStream(byte[] data, int objectNumber, int generation, DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return !encryptMetadata && IsMetadataStream(dictionary)
            ? data
            : DecryptStream(data, objectNumber, generation);
    }

    private static bool IsMetadataStream(DictionaryObject dictionary)
        => dictionary.TryGetValue("Type", out var type) && type is NameObject name
            && string.Equals(name.Value, "Metadata", StringComparison.Ordinal);

    public byte[] DecryptString(byte[] data, int objectNumber, int generation)
        => Decrypt(stringCipher, data, objectNumber, generation);

    private byte[] Decrypt(CryptMethod cipher, byte[] data, int objectNumber, int generation)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            return data;
        }

        return cipher switch
        {
            CryptMethod.Identity => data,
            CryptMethod.AesV3 => AesCbc.Decrypt(FileKey, data),
            CryptMethod.AesV2 => AesCbc.Decrypt(ObjectKey(objectNumber, generation, aes: true), data),
            _ => Rc4.Transform(ObjectKey(objectNumber, generation, aes: false), data),
        };
    }

    // /StmF and /StrF name the crypt filter in /CF; /Identity means no
    // encryption for that class of data (ISO 32000-1 7.6.5).
    private CryptMethod ResolveCipher(DictionaryObject encrypt, string selector)
    {
        if (version < 4)
        {
            return CryptMethod.Rc4;
        }

        var filterName = encrypt.TryGetValue(selector, out var selected) && selected is NameObject chosen
            ? chosen.Value
            : "StdCF";
        if (string.Equals(filterName, "Identity", StringComparison.Ordinal))
        {
            return CryptMethod.Identity;
        }

        if (encrypt.TryGetValue("CF", out var cf) && cf is DictionaryObject cfDict
            && cfDict.TryGetValue(filterName, out var filter) && filter is DictionaryObject filterDict
            && filterDict.TryGetValue("CFM", out var cfm) && cfm is NameObject method)
        {
            return method.Value switch
            {
                "AESV3" => CryptMethod.AesV3,
                "AESV2" => CryptMethod.AesV2,
                "V2" => CryptMethod.Rc4,
                "Identity" => CryptMethod.Identity,
                _ => CryptMethod.Rc4,
            };
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

    // ISO 32000-1 algorithm 2.
    private byte[] ComputeFileKey(byte[] password)
    {
        var padded = Pad(password);
        var extra = revision >= 4 && !encryptMetadata ? 4 : 0;
        var buffer = new byte[padded.Length + 32 + 4 + documentId.Length + extra];
        var pos = 0;
        Array.Copy(padded, 0, buffer, pos, padded.Length);
        pos += padded.Length;
        var oBytes = ownerEntry.Length >= 32 ? ownerEntry[..32] : ownerEntry;
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

        var hash = Md5.Hash(buffer);
        if (revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = Md5.Hash(hash[..keyLength]);
            }
        }

        return hash[..keyLength];
    }

    // ISO 32000-1 algorithm 6.
    private bool UserPasswordMatches(byte[] fileKey)
    {
        if (revision == 2)
        {
            var test = Rc4.Transform(fileKey, Padding);
            return Equal(test, userEntry, 32);
        }

        var seed = new byte[Padding.Length + documentId.Length];
        Array.Copy(Padding, seed, Padding.Length);
        Array.Copy(documentId, 0, seed, Padding.Length, documentId.Length);
        var value = Rc4.Transform(fileKey, Md5.Hash(seed));
        for (var i = 1; i <= 19; i++)
        {
            value = Rc4.Transform(Xor(fileKey, i), value);
        }

        return Equal(value, userEntry, 16);
    }

    // ISO 32000-1 algorithm 7: recover the (padded) user password from /O.
    private byte[] RecoverUserPassword(byte[] password)
    {
        var padded = Pad(password);
        var hash = Md5.Hash(padded);
        if (revision >= 3)
        {
            for (var i = 0; i < 50; i++)
            {
                hash = Md5.Hash(hash[..keyLength]);
            }
        }

        var rc4Key = hash[..keyLength];
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
                FileKey = RequireAes256Key(AesCbc.DecryptCbcNoPadding(intermediate, ZeroIv, userEncrypted));
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
                FileKey = RequireAes256Key(AesCbc.DecryptCbcNoPadding(intermediate, ZeroIv, ownerEncrypted));
                return;
            }
        }
    }

    // The AESV3 file key comes straight from decrypting the attacker-supplied /UE or /OE.
    // Anything but 32 bytes (an empty /UE gives a zero-length key that divides-by-zero in
    // AES key expansion; an oversized /UE gives a huge key) is a forged dictionary.
    private static byte[] RequireAes256Key(byte[] fileKey)
        => fileKey.Length == 32
            ? fileKey
            : throw new DocumentParseException("Revision 6 file key must be exactly 32 bytes.");

    // Revision 5 (Acrobat 9 ExtensionLevel 3) hashes with a single SHA-256 pass;
    // the iterated algorithm 2.B loop applies to revision 6 only.
    private byte[] HashPassword(byte[] password, byte[] salt, byte[] userData)
        => revision == 5
            ? SHA256.HashData(Concat(password, salt, userData))
            : Hash2B(password, salt, userData);

    // ISO 32000-2 algorithm 2.B.
    private static byte[] Hash2B(byte[] password, byte[] salt, byte[] userData)
    {
        var input = Concat(password, salt, userData);
        var k = SHA256.HashData(input);
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
                0 => SHA256.HashData(e),
                1 => SHA384.HashData(e),
                _ => SHA512.HashData(e),
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
    {
        var extra = aes ? AesSalt.Length : 0;
        var buffer = new byte[FileKey.Length + 5 + extra];
        Array.Copy(FileKey, buffer, FileKey.Length);
        var pos = FileKey.Length;
        buffer[pos++] = (byte)objectNumber;
        buffer[pos++] = (byte)(objectNumber >> 8);
        buffer[pos++] = (byte)(objectNumber >> 16);
        buffer[pos++] = (byte)generation;
        buffer[pos++] = (byte)(generation >> 8);
        if (aes)
        {
            Array.Copy(AesSalt, 0, buffer, pos, AesSalt.Length);
        }

        var hash = Md5.Hash(buffer);
        var length = Math.Min(FileKey.Length + 5, 16);
        return hash[..length];
    }

    // For V>=4 the file-key length comes from the resolved crypt-filter dictionary,
    // not the top-level /Length (ISO 32000-1 7.6.5). AESV2/AESV3 fix the size at
    // 16/32 bytes regardless of a wrong or absent /Length.
    private int DeriveCryptFilterKeyLength(DictionaryObject encrypt)
    {
        var filter = ResolveCryptFilterDictionary(encrypt);
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
            // The crypt-filter /Length is bytes (ISO 32000-1 Table 25); some producers
            // still write bits, so treat anything above 16 as a bit count.
            var value = number.IntValue;
            return DeriveMd5KeyLength(value > 16 ? value : value * 8);
        }

        return DeriveMd5KeyLength(GetInt(encrypt, "Length", 40));
    }

    private static DictionaryObject? ResolveCryptFilterDictionary(DictionaryObject encrypt)
    {
        var filterName = encrypt.TryGetValue("StmF", out var selected) && selected is NameObject chosen
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

    // The MD5-derived V1/V2/V4 file key is sliced out of a 16-byte hash; a hostile
    // /Length (e.g. 1000000000) would otherwise slice past the hash. RC4/AES key sizes
    // are 5..16 bytes (ISO 32000-1 7.6.3.3), so anything else is a malformed dictionary.
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
    {
        if (left.Length < count || right.Length < count)
        {
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (left[i] != right[i])
            {
                return false;
            }
        }

        return true;
    }

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
}
