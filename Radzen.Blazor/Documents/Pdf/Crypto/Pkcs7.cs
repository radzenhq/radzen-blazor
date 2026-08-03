using System;
using System.IO;

namespace Radzen.Documents.Pdf.Crypto;

internal static class Pkcs7
{
    public static byte[] Pad(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var pad = 16 - (data.Length % 16);
        var result = new byte[data.Length + pad];
        Array.Copy(data, result, data.Length);
        for (var i = data.Length; i < result.Length; i++)
        {
            result[i] = (byte)pad;
        }

        return result;
    }

    public static byte[] Strip(byte[] plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        if (plaintext.Length == 0)
        {
            throw new InvalidDataException("AES plaintext is empty.");
        }

        var pad = plaintext[^1];
        if (pad < 1 || pad > 16 || pad > plaintext.Length)
        {
            throw new InvalidDataException("Invalid PKCS#7 padding.");
        }

        for (var i = plaintext.Length - pad; i < plaintext.Length; i++)
        {
            if (plaintext[i] != pad)
            {
                throw new InvalidDataException("Invalid PKCS#7 padding.");
            }
        }

        return plaintext[..^pad];
    }
}
