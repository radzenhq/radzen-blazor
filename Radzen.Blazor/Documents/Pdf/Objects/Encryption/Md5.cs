namespace Radzen.Documents.Pdf.Objects.Encryption;

// Delegates to the shared managed MD5 (Radzen.MD5) that also backs Gravatar; fully
// qualified because Radzen cannot be imported here (Colors would clash).
internal static class Md5
{
    public static byte[] Hash(byte[] data) => Radzen.MD5.ComputeHash(data);
}
