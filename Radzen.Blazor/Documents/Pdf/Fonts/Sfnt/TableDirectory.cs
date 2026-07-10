#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

internal readonly struct TableRecord(uint offset, uint length)
{
    public uint Offset { get; } = offset;

    public uint Length { get; } = length;
}

// Parses the sfnt table directory at a given offset in the buffer.
internal sealed class TableDirectory
{
    private const uint VersionTrueType = 0x00010000;
    private const uint VersionOtto = 0x4F54544F;      // 'OTTO'
    private const uint VersionTrue = 0x74727565;      // 'true'

    private readonly Dictionary<string, TableRecord> tables = new(StringComparer.Ordinal);

    public TableDirectory(byte[] data, int offset)
    {
        var reader = new SfntReader(data, offset);
        var version = reader.ReadUInt32();
        if (version != VersionTrueType && version != VersionOtto && version != VersionTrue)
        {
            throw new InvalidDataException($"Unsupported sfnt version 0x{version:X8}.");
        }

        var numTables = reader.ReadUInt16();
        reader.ReadUInt16(); // searchRange
        reader.ReadUInt16(); // entrySelector
        reader.ReadUInt16(); // rangeShift

        for (var i = 0; i < numTables; i++)
        {
            var tag = reader.ReadTag();
            reader.ReadUInt32(); // checksum
            var tableOffset = reader.ReadUInt32();
            var tableLength = reader.ReadUInt32();
            tables[tag] = new TableRecord(tableOffset, tableLength);
        }
    }

    public bool TryGet(string tag, out TableRecord record) => tables.TryGetValue(tag, out record);

    public bool Contains(string tag) => tables.ContainsKey(tag);
}
