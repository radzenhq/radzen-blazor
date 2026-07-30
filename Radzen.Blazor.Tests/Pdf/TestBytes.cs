#nullable enable
using System.Text;

namespace Radzen.Blazor.Pdf.Tests;

internal static class TestBytes
{
    public static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);
}
