#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests
{
    public class AsciiHexFilterTests
    {
        [Fact]
        public void Decode_Hello()
        {
            var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48656C6C6F>"));
            Assert.Equal("Hello", Encoding.ASCII.GetString(result));
        }

        [Fact]
        public void Decode_IgnoresWhitespace()
        {
            var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48 65\n6C>"));
            Assert.Equal(new byte[] { 0x48, 0x65, 0x6C }, result);
        }

        // Odd final digit is padded with a trailing 0 per the AHx spec: "7>" -> 0x70.
        [Fact]
        public void Decode_OddFinalDigit_PadsZero()
        {
            var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("7>"));
            Assert.Equal(new byte[] { 0x70 }, result);
        }

        [Fact]
        public void Decode_IgnoresDataAfterEod()
        {
            var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48>6566"));
            Assert.Equal(new byte[] { 0x48 }, result);
        }

        [Fact]
        public void Decode_ImmediateEod_ReturnsEmpty()
        {
            var result = AsciiHexFilter.Decode(Encoding.ASCII.GetBytes(">"));
            Assert.Empty(result);
        }

        [Fact]
        public void Decode_InvalidChar_Throws()
        {
            Assert.ThrowsAny<Exception>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>")));
        }

        [Fact]
        public void Encode_ProducesUppercaseHexWithEod()
        {
            var result = Encoding.ASCII.GetString(AsciiHexFilter.Encode(Encoding.ASCII.GetBytes("Hello")));
            Assert.Equal("48656C6C6F>", result);
        }

        [Fact]
        public void Encode_Decode_RoundTrip()
        {
            var data = new byte[] { 0x00, 0x7F, 0x80, 0xFF, 0x10, 0xAB };
            var decoded = AsciiHexFilter.Decode(AsciiHexFilter.Encode(data));
            Assert.Equal(data, decoded);
        }
    }

    public class Ascii85FilterTests
    {
        // Verified: python3 base64.a85encode(b'Man ') -> "9jqo^".
        [Fact]
        public void Decode_KnownVector_WithEod()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo^~>"));
            Assert.Equal("Man ", Encoding.ASCII.GetString(result));
        }

        [Fact]
        public void Encode_KnownVector()
        {
            var result = Encoding.ASCII.GetString(Ascii85Filter.Encode(Encoding.ASCII.GetBytes("Man ")));
            // Encoder emits the group followed by the ~> terminator.
            Assert.StartsWith("9jqo^", result);
            Assert.EndsWith("~>", result);
        }

        // "z" shortcut expands to four zero bytes.
        [Fact]
        public void Decode_ZShortcut_FourZeroBytes()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("z~>"));
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, result);
        }

        // Partial final groups. python3 base64.a85encode: M->"9`", Ma->"9jn", Man->"9jqo".
        [Fact]
        public void Decode_PartialGroup_OneByte()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9`~>"));
            Assert.Equal(Encoding.ASCII.GetBytes("M"), result);
        }

        [Fact]
        public void Decode_PartialGroup_TwoBytes()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jn~>"));
            Assert.Equal(Encoding.ASCII.GetBytes("Ma"), result);
        }

        [Fact]
        public void Decode_PartialGroup_ThreeBytes()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqo~>"));
            Assert.Equal(Encoding.ASCII.GetBytes("Man"), result);
        }

        [Fact]
        public void Decode_IgnoresWhitespace()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9j qo\n^~>"));
            Assert.Equal("Man ", Encoding.ASCII.GetString(result));
        }

        [Fact]
        public void Decode_ImmediateEod_ReturnsEmpty()
        {
            var result = Ascii85Filter.Decode(Encoding.ASCII.GetBytes("~>"));
            Assert.Empty(result);
        }

        [Fact]
        public void Decode_CharOutOfRange_Throws()
        {
            // 'v' (0x76) is above the valid '!'..'u' range.
            Assert.ThrowsAny<Exception>(() => Ascii85Filter.Decode(Encoding.ASCII.GetBytes("9jqv^~>")));
        }

        [Fact]
        public void Encode_Decode_RoundTrip()
        {
            var data = Encoding.ASCII.GetBytes("Hello, World!");
            var decoded = Ascii85Filter.Decode(Ascii85Filter.Encode(data));
            Assert.Equal(data, decoded);
        }

        [Fact]
        public void Encode_Decode_RoundTrip_WithZeroRun()
        {
            var data = new byte[] { 0, 0, 0, 0, 1, 2, 3, 0, 0, 0, 0 };
            var decoded = Ascii85Filter.Decode(Ascii85Filter.Encode(data));
            Assert.Equal(data, decoded);
        }
    }
}
