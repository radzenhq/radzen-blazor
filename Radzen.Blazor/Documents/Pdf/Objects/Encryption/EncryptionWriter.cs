using System;
using System.Text;
using Radzen.Documents.Crypto;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Write-side counterpart of <see cref="StandardSecurityHandler"/>. Builds the
/// <c>/Encrypt</c> dictionary for a chosen <see cref="EncryptionOptions"/> and
/// encrypts each string and stream with its per-object key (ISO 32000-1
/// algorithm 1). The active writer and current object number are passed
/// explicitly via <see cref="WriteContext"/> so <see cref="StringObject"/> and
/// <see cref="StreamObject"/> can route their bytes through it.
/// </summary>
internal sealed class EncryptionWriter(
    byte[] fileKey, EncryptionWriter.Method cipher, MaterialSequence material, bool encryptMetadata = true)
{
    private readonly byte[] fileKey = fileKey;
    private readonly Method cipher = cipher;
    private readonly MaterialSequence material = material;
    private readonly bool encryptMetadata = encryptMetadata;

    internal enum Method
    {
        Rc4,
        AesV2,
        AesV3,
    }

    public static EncryptionWriter Build(
        EncryptionOptions options, byte[] documentId, MaterialSequence material, out DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(documentId);
        ArgumentNullException.ThrowIfNull(material);

        var permissions = options.ToPermissions();
        var encryptMetadata = options.EncryptMetadata;
        switch (options.Algorithm)
        {
            case EncryptionAlgorithm.Aes256:
            {
                var fileKey = material.Next(32);
                var derived = StandardSecurityHandler.DeriveAes256(
                    options.UserPassword, options.OwnerPassword, fileKey, permissions, encryptMetadata,
                    userValidation: material.Next(8), userKeySalt: material.Next(8),
                    ownerValidation: material.Next(8), ownerKeySalt: material.Next(8),
                    permsNoise: material.Next(4));
                dictionary = BuildV5Dictionary(derived, permissions, encryptMetadata);
                return new EncryptionWriter(fileKey, Method.AesV3, material, encryptMetadata);
            }

            case EncryptionAlgorithm.Aes128:
            {
                var derived = StandardSecurityHandler.DeriveLegacy(
                    options.UserPassword, options.OwnerPassword, 4, 16, permissions, documentId, encryptMetadata);
                dictionary = BuildLegacyDictionary(derived, permissions, aes: true, encryptMetadata);
                return new EncryptionWriter(derived.FileKey, Method.AesV2, material, encryptMetadata);
            }

            default:
            {
                // RC4 (V2/R3) predates the /EncryptMetadata flag, which is meaningful
                // only for crypt-filter handlers, so the option is not surfaced here.
                var derived = StandardSecurityHandler.DeriveLegacy(
                    options.UserPassword, options.OwnerPassword, 3, 16, permissions, documentId, true);
                dictionary = BuildLegacyDictionary(derived, permissions, aes: false, encryptMetadata: true);
                return new EncryptionWriter(derived.FileKey, Method.Rc4, material);
            }
        }
    }

    public byte[] EncryptString(byte[] data, int objectNumber, int generation)
    {
        ArgumentNullException.ThrowIfNull(data);
        return Apply(data, objectNumber, generation);
    }

    public byte[] EncryptStream(ReadOnlyMemory<byte> data, int objectNumber, int generation)
        => Apply(data, objectNumber, generation);

    // A /Type /Metadata stream is left plaintext when the writer's /EncryptMetadata flag
    // is false (ISO 32000-1 7.6.3.2), mirroring the reader's DecryptStream dictionary path.
    public byte[] EncryptStream(
        ReadOnlyMemory<byte> data, int objectNumber, int generation, DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return !encryptMetadata && StandardSecurityHandler.IsMetadataStream(dictionary)
            ? data.ToArray()
            : Apply(data, objectNumber, generation);
    }

    private static DictionaryObject BuildLegacyDictionary(
        (byte[] Owner, byte[] User, byte[] FileKey) derived, int permissions, bool aes, bool encryptMetadata)
    {
        var dictionary = new DictionaryObject
        {
            ["Filter"] = new NameObject("Standard"),
            ["V"] = new NumberObject(aes ? 4 : 2),
            ["R"] = new NumberObject(aes ? 4 : 3),
            ["Length"] = new NumberObject(128),
        };

        if (aes)
        {
            AddStandardCryptFilter(dictionary, "AESV2", 16);
        }

        dictionary["O"] = FromBytes(derived.Owner);
        dictionary["U"] = FromBytes(derived.User);
        dictionary["P"] = new NumberObject(permissions);
        AddEncryptMetadata(dictionary, aes, encryptMetadata);
        return dictionary;
    }

    private static DictionaryObject BuildV5Dictionary(
        (byte[] Owner, byte[] User, byte[] OwnerEncrypted, byte[] UserEncrypted, byte[] Perms) derived,
        int permissions, bool encryptMetadata)
    {
        var dictionary = new DictionaryObject
        {
            ["Filter"] = new NameObject("Standard"),
            ["V"] = new NumberObject(5),
            ["R"] = new NumberObject(6),
            ["Length"] = new NumberObject(256),
        };

        AddStandardCryptFilter(dictionary, "AESV3", 32);
        dictionary["O"] = FromBytes(derived.Owner);
        dictionary["U"] = FromBytes(derived.User);
        dictionary["OE"] = FromBytes(derived.OwnerEncrypted);
        dictionary["UE"] = FromBytes(derived.UserEncrypted);
        dictionary["Perms"] = FromBytes(derived.Perms);
        dictionary["P"] = new NumberObject(permissions);
        AddEncryptMetadata(dictionary, aes: true, encryptMetadata);
        return dictionary;
    }

    // /EncryptMetadata is meaningful only for crypt-filter handlers (V >= 4) and its
    // default is true, so the entry is written only for a false value under AES. This
    // keeps every previously produced /Encrypt dictionary byte-for-byte unchanged.
    private static void AddEncryptMetadata(DictionaryObject dictionary, bool aes, bool encryptMetadata)
    {
        if (aes && !encryptMetadata)
        {
            dictionary["EncryptMetadata"] = new BooleanObject(false);
        }
    }

    private static void AddStandardCryptFilter(DictionaryObject dictionary, string method, int keyLength)
    {
        var standard = new DictionaryObject
        {
            ["CFM"] = new NameObject(method),
            ["AuthEvent"] = new NameObject("DocOpen"),
            ["Length"] = new NumberObject(keyLength),
        };
        dictionary["CF"] = new DictionaryObject { ["StdCF"] = standard };
        dictionary["StmF"] = new NameObject("StdCF");
        dictionary["StrF"] = new NameObject("StdCF");
    }

    private static StringObject FromBytes(byte[] bytes) => new(Encoding.Latin1.GetString(bytes));

    private byte[] Apply(ReadOnlyMemory<byte> data, int objectNumber, int generation)
    {
        // The ciphers take arrays and allocate their own output, so the input is
        // materialized only once an actual cipher runs.
        var bytes = data.ToArray();
        if (bytes.Length == 0)
        {
            return bytes;
        }

        return cipher switch
        {
            Method.Rc4 => Rc4.Transform(
                StandardSecurityHandler.ComputeObjectKey(fileKey, objectNumber, generation, aes: false), bytes),
            Method.AesV2 => AesEncrypt(
                StandardSecurityHandler.ComputeObjectKey(fileKey, objectNumber, generation, aes: true), bytes),
            _ => AesEncrypt(fileKey, bytes),
        };
    }

    // ISO 32000-1 algorithm 1.a: PKCS#7 pad, encrypt with a caller-supplied IV, prepend the IV.
    private byte[] AesEncrypt(byte[] key, byte[] data)
    {
        var iv = material.Next(16);
        var cipher = AesCbc.EncryptCbcNoPadding(key, iv, Pkcs7(data));
        var result = new byte[iv.Length + cipher.Length];
        Array.Copy(iv, result, iv.Length);
        Array.Copy(cipher, 0, result, iv.Length, cipher.Length);
        return result;
    }

    private static byte[] Pkcs7(byte[] data)
    {
        var pad = 16 - (data.Length % 16);
        var result = new byte[data.Length + pad];
        Array.Copy(data, result, data.Length);
        for (var i = data.Length; i < result.Length; i++)
        {
            result[i] = (byte)pad;
        }

        return result;
    }
}
