using System;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Hand-rolled RC4 stream cipher. The transform is symmetric: the same call
/// both encrypts and decrypts (ISO 32000-1 uses it for the V1/V2 handlers).
/// </summary>
internal static class Rc4
{
    public static byte[] Transform(byte[] key, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);

        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var result = new byte[data.Length];
        int x = 0, y = 0;
        for (var k = 0; k < data.Length; k++)
        {
            x = (x + 1) & 0xFF;
            y = (y + s[x]) & 0xFF;
            (s[x], s[y]) = (s[y], s[x]);
            result[k] = (byte)(data[k] ^ s[(s[x] + s[y]) & 0xFF]);
        }

        return result;
    }
}
