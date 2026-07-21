using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf.Fonts.Cff;

internal static class CffSubsetter
{
    private static int FirstCustomSid => CffStandardStrings.Strings.Length;

    public static byte[] Subset(CffFont font, IReadOnlyCollection<int> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        var gids = new ushort[glyphIds.Count];
        var count = 0;
        foreach (var gid in glyphIds)
        {
            if (gid < 0 || gid >= font.GlyphCount)
            {
                throw new ArgumentOutOfRangeException(nameof(glyphIds), gid,
                    $"Requested glyph id {gid} is outside the font's glyph range [0, {font.GlyphCount}).");
            }

            gids[count++] = (ushort)gid;
        }

        var closure = CompactGidMap.OrderFromMap(BuildCompactGidMap(gids));
        var glyphCount = closure.Count;

        var charStrings = new byte[glyphCount][];
        var fdSelect = new int[glyphCount];
        for (var i = 0; i < glyphCount; i++)
        {
            var gid = closure[i];

            if (font.UsesSeacEndchar(gid))
            {
                throw new NotSupportedException(
                    $"Glyph {gid} uses an endchar seac (accent composition), which the compact CFF subsetter cannot renumber. "
                    + "Re-save the font without seac composition to embed it.");
            }

            charStrings[i] = font.GetCharStringBytes(gid);
            fdSelect[i] = font.GetFd(gid);
        }

        var registry = font.Registry ?? "Adobe";
        var ordering = font.Ordering ?? "Identity";
        var registrySid = FirstCustomSid;
        var orderingSid = FirstCustomSid + 1;

        var nameBytes = Encoding.ASCII.GetBytes(font.FontName ?? "Subset");
        var nameIndex = CffIndex.Write([nameBytes]);
        var stringIndex = CffIndex.Write([Encoding.ASCII.GetBytes(registry), Encoding.ASCII.GetBytes(ordering)]);
        var globalSubrIndex = CffIndex.Write(font.GetGlobalSubrBytes());
        var charsetBytes = BuildIdentityCharset(glyphCount);
        var fdSelectBytes = BuildFdSelect(fdSelect);
        var charStringsIndex = CffIndex.Write(charStrings);

        var fdCount = font.FdCount;
        var privateBlocks = new byte[fdCount][];
        var privateSizes = new int[fdCount];
        var localSubrBlocks = new byte[fdCount][];
        for (var fd = 0; fd < fdCount; fd++)
        {
            var localSubrs = font.GetLocalSubrBytes(fd);
            localSubrBlocks[fd] = localSubrs.Length > 0 ? CffIndex.Write(localSubrs) : [];
            privateBlocks[fd] = BuildPrivateDict(font.GetDefaultWidthX(fd), font.GetNominalWidthX(fd), localSubrs.Length > 0);
            privateSizes[fd] = privateBlocks[fd].Length;
        }

        var fdMatrices = new double[]?[fdCount];
        for (var fd = 0; fd < fdCount; fd++)
        {
            fdMatrices[fd] = font.GetFdFontMatrix(fd);
        }

        var topDictLen = BuildTopDict(registrySid, orderingSid, font.Supplement, font.FontMatrix, glyphCount, 0, 0, 0, 0).Length;
        var topDictIndexLen = CffIndex.Write([new byte[topDictLen]]).Length;
        var fdArrayIndexLen = CffIndex.Write(BuildFontDicts(privateSizes, new int[fdCount], fdMatrices)).Length;

        const int headerLen = 4;
        var pos = headerLen;
        pos += nameIndex.Length;
        pos += topDictIndexLen;
        pos += stringIndex.Length;
        pos += globalSubrIndex.Length;
        var posCharset = pos;
        pos += charsetBytes.Length;
        var posFdSelect = pos;
        pos += fdSelectBytes.Length;
        var posCharStrings = pos;
        pos += charStringsIndex.Length;
        var posFdArray = pos;
        pos += fdArrayIndexLen;

        var privateOffsets = new int[fdCount];
        for (var fd = 0; fd < fdCount; fd++)
        {
            privateOffsets[fd] = pos;
            pos += privateBlocks[fd].Length + localSubrBlocks[fd].Length;
        }

        var totalLen = pos;

        var topDict = BuildTopDict(registrySid, orderingSid, font.Supplement, font.FontMatrix, glyphCount, posCharset, posCharStrings, posFdArray, posFdSelect);
        var topDictIndex = CffIndex.Write([topDict]);
        var fdArrayIndex = CffIndex.Write(BuildFontDicts(privateSizes, privateOffsets, fdMatrices));

        var result = new byte[totalLen];
        var p = 0;
        result[p++] = 1;
        result[p++] = 0;
        result[p++] = headerLen;
        result[p++] = 1;
        p = Append(result, p, nameIndex);
        p = Append(result, p, topDictIndex);
        p = Append(result, p, stringIndex);
        p = Append(result, p, globalSubrIndex);
        p = Append(result, p, charsetBytes);
        p = Append(result, p, fdSelectBytes);
        p = Append(result, p, charStringsIndex);
        p = Append(result, p, fdArrayIndex);
        for (var fd = 0; fd < fdCount; fd++)
        {
            p = Append(result, p, privateBlocks[fd]);
            p = Append(result, p, localSubrBlocks[fd]);
        }

        return result;
    }

    public static Dictionary<ushort, ushort> BuildCompactGidMap(IReadOnlyCollection<ushort> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(glyphIds);

        var closure = new SortedSet<ushort> { 0 };
        foreach (var gid in glyphIds)
        {
            closure.Add(gid);
        }

        var map = new Dictionary<ushort, ushort>(closure.Count);
        ushort next = 0;
        foreach (var gid in closure)
        {
            map[gid] = next++;
        }

        return map;
    }


    private static byte[] BuildIdentityCharset(int glyphCount)
    {
        var bytes = new byte[1 + ((glyphCount - 1) * 2)];
        bytes[0] = 0;
        var p = 1;
        for (var i = 1; i < glyphCount; i++)
        {
            bytes[p++] = (byte)(i >> 8);
            bytes[p++] = (byte)i;
        }

        return bytes;
    }

    private static byte[] BuildFdSelect(int[] fdSelect)
    {
        var bytes = new byte[1 + fdSelect.Length];
        bytes[0] = 0;
        for (var i = 0; i < fdSelect.Length; i++)
        {
            bytes[1 + i] = (byte)fdSelect[i];
        }

        return bytes;
    }

    private static byte[] BuildPrivateDict(double defaultWidthX, double nominalWidthX, bool hasLocalSubrs)
    {
        var dict = new List<byte>();
        CffDict.WriteNumber(dict, defaultWidthX);
        CffDict.WriteOperator(dict, 20);
        CffDict.WriteNumber(dict, nominalWidthX);
        CffDict.WriteOperator(dict, 21);

        if (hasLocalSubrs)
        {
            var withoutSubrs = dict.Count;
            var subrsOffset = withoutSubrs + 6;
            CffDict.WriteOffset(dict, subrsOffset);
            CffDict.WriteOperator(dict, 19);
        }

        return [.. dict];
    }

    private static byte[][] BuildFontDicts(int[] privateSizes, int[] privateOffsets, double[]?[] fontMatrices)
    {
        var result = new byte[privateSizes.Length][];
        for (var fd = 0; fd < privateSizes.Length; fd++)
        {
            var dict = new List<byte>();
            WriteFontMatrix(dict, fontMatrices[fd]);
            CffDict.WriteOffset(dict, privateSizes[fd]);
            CffDict.WriteOffset(dict, privateOffsets[fd]);
            CffDict.WriteOperator(dict, 18);
            result[fd] = [.. dict];
        }

        return result;
    }

    private static void WriteFontMatrix(List<byte> dict, double[]? matrix)
    {
        if (matrix is null)
        {
            return;
        }

        foreach (var value in matrix)
        {
            CffDict.WriteNumber(dict, value);
        }

        CffDict.WriteOperator(dict, 1207);
    }

    private static byte[] BuildTopDict(
        int registrySid,
        int orderingSid,
        int supplement,
        double[]? fontMatrix,
        int glyphCount,
        int charsetOffset,
        int charStringsOffset,
        int fdArrayOffset,
        int fdSelectOffset)
    {
        var dict = new List<byte>();

        CffDict.WriteInteger(dict, registrySid);
        CffDict.WriteInteger(dict, orderingSid);
        CffDict.WriteInteger(dict, supplement);
        CffDict.WriteOperator(dict, 1230);

        WriteFontMatrix(dict, fontMatrix);

        CffDict.WriteInteger(dict, glyphCount);
        CffDict.WriteOperator(dict, 1234);

        CffDict.WriteOffset(dict, charsetOffset);
        CffDict.WriteOperator(dict, 15);

        CffDict.WriteOffset(dict, charStringsOffset);
        CffDict.WriteOperator(dict, 17);

        CffDict.WriteOffset(dict, fdArrayOffset);
        CffDict.WriteOperator(dict, 1236);

        CffDict.WriteOffset(dict, fdSelectOffset);
        CffDict.WriteOperator(dict, 1237);

        return [.. dict];
    }

    private static int Append(byte[] dst, int pos, byte[] src)
    {
        Array.Copy(src, 0, dst, pos, src.Length);
        return pos + src.Length;
    }
}
