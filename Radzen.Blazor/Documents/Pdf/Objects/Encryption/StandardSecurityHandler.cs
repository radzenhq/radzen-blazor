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
    private readonly CryptMethod cipher;

    public StandardSecurityHandler(DictionaryObject encrypt, byte[] documentId, byte[] password)
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
            _ => Math.Max(5, GetInt(encrypt, "Length", 40) / 8),
        };

        cipher = ResolveCipher(encrypt);
        FileKey = [];
        Authenticate(password);
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

    public byte[] Decrypt(byte[] data, int objectNumber, int generation)
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

    private CryptMethod ResolveCipher(DictionaryObject encrypt)
    {
        if (version < 4)
        {
            return CryptMethod.Rc4;
        }

        if (encrypt.TryGetValue("CF", out var cf) && cf is DictionaryObject cfDict
            && cfDict.TryGetValue("StdCF", out var std) && std is DictionaryObject stdDict
            && stdDict.TryGetValue("CFM", out var cfm) && cfm is NameObject method)
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
            var userHash = Hash2B(pw, u[32..40], []);
            if (Equal(userHash, u, 32))
            {
                IsUserPassword = true;
                var intermediate = Hash2B(pw, u[40..48], []);
                FileKey = AesCbc.DecryptCbcNoPadding(intermediate, ZeroIv, userEncrypted);
                return;
            }
        }

        if (o.Length >= 48 && u.Length >= 48)
        {
            var ownerHash = Hash2B(pw, o[32..40], u[..48]);
            if (Equal(ownerHash, o, 32))
            {
                IsOwnerPassword = true;
                var intermediate = Hash2B(pw, o[40..48], u[..48]);
                FileKey = AesCbc.DecryptCbcNoPadding(intermediate, ZeroIv, ownerEncrypted);
                return;
            }
        }
    }

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
