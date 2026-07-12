using System;
using System.Security.Cryptography;
using System.Text;
using Radzen.Documents.Crypto;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Write-side counterpart of <see cref="StandardSecurityHandler"/>. Builds the
/// <c>/Encrypt</c> dictionary for a chosen <see cref="EncryptionOptions"/> and
/// encrypts each string and stream with its per-object key (ISO 32000-1
/// algorithm 1). While an object is being serialized the active writer is held
/// in a thread-static ambient so <see cref="StringObject"/> and
/// <see cref="StreamObject"/> can route their bytes through it.
/// </summary>
internal sealed class EncryptionWriter(byte[] fileKey, EncryptionWriter.Method cipher, bool encryptMetadata = true)
{
    [ThreadStatic]
    private static EncryptionWriter? current;

    [ThreadStatic]
    private static int currentObjectNumber;

    private readonly byte[] fileKey = fileKey;
    private readonly Method cipher = cipher;
    private readonly bool encryptMetadata = encryptMetadata;

    internal enum Method
    {
        Rc4,
        AesV2,
        AesV3,
    }

    public static EncryptionWriter? Current => current;

    public static int CurrentObjectNumber => currentObjectNumber;

    public static EncryptionWriter Build(EncryptionOptions options, byte[] documentId, out DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(documentId);

        var permissions = options.ToPermissions();
        var encryptMetadata = options.EncryptMetadata;
        switch (options.Algorithm)
        {
            case EncryptionAlgorithm.Aes256:
            {
                var fileKey = RandomNumberGenerator.GetBytes(32);
                var derived = StandardSecurityHandler.DeriveAes256(
                    options.UserPassword, options.OwnerPassword, fileKey, permissions, encryptMetadata);
                dictionary = BuildV5Dictionary(derived, permissions, encryptMetadata);
                return new EncryptionWriter(fileKey, Method.AesV3, encryptMetadata);
            }

            case EncryptionAlgorithm.Aes128:
            {
                var derived = StandardSecurityHandler.DeriveLegacy(
                    options.UserPassword, options.OwnerPassword, 4, 16, permissions, documentId, encryptMetadata);
                dictionary = BuildLegacyDictionary(derived, permissions, aes: true, encryptMetadata);
                return new EncryptionWriter(derived.FileKey, Method.AesV2, encryptMetadata);
            }

            default:
            {
                // RC4 (V2/R3) predates the /EncryptMetadata flag, which is meaningful
                // only for crypt-filter handlers, so the option is not surfaced here.
                var derived = StandardSecurityHandler.DeriveLegacy(
                    options.UserPassword, options.OwnerPassword, 3, 16, permissions, documentId, true);
                dictionary = BuildLegacyDictionary(derived, permissions, aes: false, encryptMetadata: true);
                return new EncryptionWriter(derived.FileKey, Method.Rc4);
            }
        }
    }

    // Establishes the ambient writer for the duration of one indirect object.
    public ObjectScope BeginObject(int objectNumber)
    {
        current = this;
        currentObjectNumber = objectNumber;
        return default;
    }

    public byte[] EncryptString(byte[] data, int objectNumber, int generation)
        => Apply(data, objectNumber, generation);

    public byte[] EncryptStream(byte[] data, int objectNumber, int generation)
        => Apply(data, objectNumber, generation);

    // A /Type /Metadata stream is left plaintext when the writer's /EncryptMetadata flag
    // is false (ISO 32000-1 7.6.3.2), mirroring the reader's DecryptStream dictionary path.
    public byte[] EncryptStream(byte[] data, int objectNumber, int generation, DictionaryObject dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return !encryptMetadata && IsMetadataStream(dictionary)
            ? data
            : Apply(data, objectNumber, generation);
    }

    private static bool IsMetadataStream(DictionaryObject dictionary)
        => dictionary.TryGetValue("Type", out var type) && type is NameObject name
            && string.Equals(name.Value, "Metadata", StringComparison.Ordinal);

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

    private byte[] Apply(byte[] data, int objectNumber, int generation)
    {
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length == 0)
        {
            return data;
        }

        return cipher switch
        {
            Method.Rc4 => Rc4.Transform(
                StandardSecurityHandler.ComputeObjectKey(fileKey, objectNumber, generation, aes: false), data),
            Method.AesV2 => AesEncrypt(
                StandardSecurityHandler.ComputeObjectKey(fileKey, objectNumber, generation, aes: true), data),
            _ => AesEncrypt(fileKey, data),
        };
    }

    // ISO 32000-1 algorithm 1.a: PKCS#7 pad, encrypt with a random IV, prepend the IV.
    private static byte[] AesEncrypt(byte[] key, byte[] data)
    {
        var iv = RandomNumberGenerator.GetBytes(16);
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

    // Clears the ambient writer once an indirect object has been serialized.
    internal readonly struct ObjectScope : IDisposable
    {
        public void Dispose() => current = null;
    }
}
