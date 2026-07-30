using System.Text;

namespace Radzen.Documents.Pdf.Write;

internal static class XmpPacketFraming
{
    public const string PacketId = "W5M0MpCehiHzreSzNTczkc9d";

    public const string BeginInstruction = "begin=\"﻿\" id=\"" + PacketId + "\"";

    public const string EndInstruction = "end=\"w\"";

    private const string PaddingLine =
        "                                                                                \n";

    private const int PaddingLineCount = 24;

    public static string Padding
    {
        get
        {
            var builder = new StringBuilder(PaddingLine.Length * PaddingLineCount);
            AppendPadding(builder);
            return builder.ToString();
        }
    }

    public static void AppendPadding(StringBuilder builder)
    {
        for (var i = 0; i < PaddingLineCount; i++)
        {
            builder.Append(PaddingLine);
        }
    }
}
