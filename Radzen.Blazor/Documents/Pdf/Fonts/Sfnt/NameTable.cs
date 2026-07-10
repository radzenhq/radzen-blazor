#nullable enable
using System.Text;

namespace Radzen.Documents.Pdf.Fonts.Sfnt
{
    internal sealed class NameTable
    {
        private const int FamilyNameId = 1;
        private const int SubfamilyNameId = 2;
        private const int PostScriptNameId = 6;
        private const int EnglishUnitedStates = 0x0409;

        public string FamilyName { get; private set; } = string.Empty;

        public string SubfamilyName { get; private set; } = string.Empty;

        public string PostScriptName { get; private set; } = string.Empty;

        public static NameTable Parse(byte[] data, int offset)
        {
            var table = new NameTable();
            var reader = new SfntReader(data, offset);

            reader.ReadUInt16(); // format
            var count = reader.ReadUInt16();
            var stringOffset = reader.ReadUInt16();
            var storageBase = offset + stringOffset;

            string? familyName = null, familyFallback = null;
            string? subfamily = null, subfamilyFallback = null;
            string? postScript = null, postScriptFallback = null;

            for (var i = 0; i < count; i++)
            {
                var platformId = reader.ReadUInt16();
                var encodingId = reader.ReadUInt16();
                var languageId = reader.ReadUInt16();
                var nameId = reader.ReadUInt16();
                var length = reader.ReadUInt16();
                var recordOffset = reader.ReadUInt16();
                var next = reader.Position;

                var preferred = platformId == 3
                    && (encodingId == 1 || encodingId == 10)
                    && languageId == EnglishUnitedStates;
                var acceptableFallback = platformId == 1 || platformId == 3;

                if (!preferred && !acceptableFallback)
                {
                    reader.Position = next;
                    continue;
                }

                var value = Decode(data, storageBase + recordOffset, length, platformId, encodingId);

                switch (nameId)
                {
                    case FamilyNameId:
                        Assign(preferred, value, ref familyName, ref familyFallback);
                        break;
                    case SubfamilyNameId:
                        Assign(preferred, value, ref subfamily, ref subfamilyFallback);
                        break;
                    case PostScriptNameId:
                        Assign(preferred, value, ref postScript, ref postScriptFallback);
                        break;
                }

                reader.Position = next;
            }

            table.FamilyName = familyName ?? familyFallback ?? string.Empty;
            table.SubfamilyName = subfamily ?? subfamilyFallback ?? string.Empty;
            table.PostScriptName = postScript ?? postScriptFallback ?? string.Empty;
            return table;
        }

        private static void Assign(bool preferred, string value, ref string? primary, ref string? fallback)
        {
            if (preferred)
            {
                primary ??= value;
            }
            else
            {
                fallback ??= value;
            }
        }

        private static string Decode(byte[] data, int start, int length, int platformId, int encodingId)
        {
            if (start < 0 || start + length > data.Length)
            {
                return string.Empty;
            }

            var isUtf16 = platformId == 3 || (platformId == 0);
            if (isUtf16 && encodingId != 0 && platformId == 3 && encodingId != 1 && encodingId != 10)
            {
                isUtf16 = false;
            }

            if (isUtf16)
            {
                return Encoding.BigEndianUnicode.GetString(data, start, length);
            }

            // Platform 1 (Mac Roman): treat as Latin-1 for the ASCII range used by names.
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = (char)data[start + i];
            }

            return new string(chars);
        }
    }
}
