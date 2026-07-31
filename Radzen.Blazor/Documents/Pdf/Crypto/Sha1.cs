using System;
using System.Security.Cryptography;

namespace Radzen.Documents.Pdf.Crypto;

internal static class Sha1
{
    public static byte[] ComputeHash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // ISO 32000-2 12.8.4.3 fixes SHA-1 as the /VRI key digest, so CA5350 cannot be honoured here.
#pragma warning disable RS0030, CA5350
        return SHA1.HashData(data);
#pragma warning restore RS0030, CA5350
    }

    public static string ComputeHashHex(byte[] data)
        => HexCodec.EncodeToString(ComputeHash(data), HexCase.Upper);
}
