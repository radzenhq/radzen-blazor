#nullable enable
using System;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf.Fonts.Cff;

// Rebuilds a compact CID-keyed CFF holding only the closure of the requested glyphs
// (requested original gids plus glyph 0). Charstrings are copied verbatim and the whole
// FDArray, its Private DICTs, local subrs and the global subrs are preserved, so a re-parse
// recovers identical advance widths. Offsets use the forced 5-byte integer form so every
// DICT has a layout-independent size and positions resolve in a single pass.
internal static class CffSubsetter
{
    // Registry/Ordering are appended as custom strings; standard strings occupy SIDs 0..N-1.
    private static int FirstCustomSid => CffStandardStrings.Strings.Length;

    public static byte[] Subset(CffFont font, IReadOnlyCollection<int> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        var closure = BuildClosure(font, glyphIds);
        var glyphCount = closure.Length;

        var charStrings = new byte[glyphCount][];
        var cids = new int[glyphCount];
        var fdSelect = new int[glyphCount];
        for (var i = 0; i < glyphCount; i++)
        {
            var gid = closure[i];
            charStrings[i] = font.GetCharStringBytes(gid);
            cids[i] = font.Charset[gid];
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
        var charsetBytes = BuildCharset(cids);
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

        var topDictLen = BuildTopDict(registrySid, orderingSid, font.Supplement, glyphCount, 0, 0, 0, 0).Length;
        var topDictIndexLen = CffIndex.Write([new byte[topDictLen]]).Length;
        var fdArrayIndexLen = CffIndex.Write(BuildFontDicts(privateSizes, new int[fdCount])).Length;

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

        var topDict = BuildTopDict(registrySid, orderingSid, font.Supplement, glyphCount, posCharset, posCharStrings, posFdArray, posFdSelect);
        var topDictIndex = CffIndex.Write([topDict]);
        var fdArrayIndex = CffIndex.Write(BuildFontDicts(privateSizes, privateOffsets));

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

    private static int[] BuildClosure(CffFont font, IReadOnlyCollection<int> glyphIds)
    {
        var set = new SortedSet<int> { 0 };
        foreach (var gid in glyphIds)
        {
            if (gid >= 0 && gid < font.GlyphCount)
            {
                set.Add(gid);
            }
        }

        return [.. set];
    }

    private static byte[] BuildCharset(int[] cids)
    {
        // Format 0: leading byte then a Card16 CID per glyph 1..n-1 (glyph 0 is implicit CID 0).
        var bytes = new byte[1 + ((cids.Length - 1) * 2)];
        bytes[0] = 0;
        var p = 1;
        for (var i = 1; i < cids.Length; i++)
        {
            bytes[p++] = (byte)(cids[i] >> 8);
            bytes[p++] = (byte)cids[i];
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
            // Subrs offset is relative to the Private DICT start; local subrs follow it directly,
            // so the offset equals the (forced-5-byte, hence stable) Private DICT length.
            var withoutSubrs = dict.Count;
            var subrsOffset = withoutSubrs + 6;
            CffDict.WriteOffset(dict, subrsOffset);
            CffDict.WriteOperator(dict, 19);
        }

        return [.. dict];
    }

    private static byte[][] BuildFontDicts(int[] privateSizes, int[] privateOffsets)
    {
        var result = new byte[privateSizes.Length][];
        for (var fd = 0; fd < privateSizes.Length; fd++)
        {
            var dict = new List<byte>();
            CffDict.WriteOffset(dict, privateSizes[fd]);
            CffDict.WriteOffset(dict, privateOffsets[fd]);
            CffDict.WriteOperator(dict, 18);
            result[fd] = [.. dict];
        }

        return result;
    }

    private static byte[] BuildTopDict(
        int registrySid,
        int orderingSid,
        int supplement,
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
