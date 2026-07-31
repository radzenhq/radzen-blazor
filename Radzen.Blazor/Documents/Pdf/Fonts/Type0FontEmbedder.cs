using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Fonts;

// Type0/CID font object graph per ISO 32000-1 9.7.
internal static class Type0FontEmbedder
{
    private const int StemV = 80;
    private const int DefaultWidth = 1000;

    public static ReferenceObject Embed(DocumentWriter writer, OutputFontProgram program)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(program);

        var isCff = program.Kind == OutputFontFileKind.Cff;
        var descriptor = new DictionaryObject
        {
            ["Type"] = new NameObject("FontDescriptor"),
            ["FontName"] = new NameObject(program.BaseName),
            ["Flags"] = new NumberObject(program.Flags),
            ["FontBBox"] = NumberArray(program.BoundingBox),
            ["ItalicAngle"] = new NumberObject(program.ItalicAngle),
            ["Ascent"] = new NumberObject(program.Ascent),
            ["Descent"] = new NumberObject(program.Descent),
            ["CapHeight"] = new NumberObject(program.CapHeight),
            ["StemV"] = new NumberObject(StemV),
        };

        descriptor[isCff ? "FontFile3" : "FontFile2"] = writer.Add(FontFile(program, isCff));
        descriptor["CIDSet"] = writer.Add(FlateFilter.EncodeStream(program.CidSet.Span));
        var descriptorRef = writer.Add(descriptor);

        var descendant = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject(isCff ? "CIDFontType0" : "CIDFontType2"),
            ["BaseFont"] = new NameObject(program.BaseName),
            ["CIDSystemInfo"] = new DictionaryObject
            {
                ["Registry"] = new StringObject("Adobe"),
                ["Ordering"] = new StringObject("Identity"),
                ["Supplement"] = new NumberObject(0),
            },
            ["FontDescriptor"] = descriptorRef,
            ["DW"] = new NumberObject(DefaultWidth),
            ["W"] = Widths(program.Widths),
        };

        if (!isCff)
        {
            descendant["CIDToGIDMap"] = new NameObject("Identity");
        }

        var descendantRef = writer.Add(descendant);
        var toUnicodeRef = writer.Add(FlateFilter.EncodeStream(program.ToUnicode.Span));

        var top = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type0"),
            ["BaseFont"] = new NameObject(program.BaseName),
            ["Encoding"] = new NameObject("Identity-H"),
            ["DescendantFonts"] = new ArrayObject { descendantRef },
            ["ToUnicode"] = toUnicodeRef,
        };

        return writer.Add(top);
    }

    private static StreamObject FontFile(OutputFontProgram program, bool isCff)
    {
        var stream = FlateFilter.EncodeStream(program.File.Span);
        if (isCff)
        {
            stream.Dictionary["Subtype"] = new NameObject("CIDFontType0C");
        }
        else
        {
            stream.Dictionary["Length1"] = new NumberObject(program.File.Length);
        }

        return stream;
    }

    private static ArrayObject NumberArray(System.Collections.Immutable.ImmutableArray<int> values)
    {
        var array = new ArrayObject();
        foreach (var value in values)
        {
            array.Add(new NumberObject(value));
        }

        return array;
    }

    private static ArrayObject Widths(System.Collections.Immutable.ImmutableArray<OutputWidthRun> runs)
    {
        var w = new ArrayObject();
        foreach (var run in runs)
        {
            w.Add(new NumberObject(run.Cid));
            var widths = new ArrayObject();
            foreach (var width in run.Widths)
            {
                widths.Add(new NumberObject(width));
            }

            w.Add(widths);
        }

        return w;
    }

    public static Dictionary<ushort, int> RemapToCompactGids(
        IReadOnlyDictionary<ushort, int> gidToUnicode,
        IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var remapped = new Dictionary<ushort, int>(gidToUnicode.Count);
        foreach (var (gid, codepoint) in gidToUnicode)
        {
            remapped[gidMap[gid]] = codepoint;
        }

        return remapped;
    }
}
