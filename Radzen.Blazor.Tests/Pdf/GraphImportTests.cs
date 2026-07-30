#nullable enable
using System.IO;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class GraphImportTests
{
    private static byte[] Wrap(FixturePdf pdf, int count)
    {
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static DocumentReader SourceWithIndirectScalars()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Field 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /V 3 0 R /MaxLen 4 0 R /DA 5 0 R /Flag 6 0 R >>\nendobj\n")
            .Object(3, "3 0 obj\n(hello)\nendobj\n")
            .Object(4, "4 0 obj\n42\nendobj\n")
            .Object(5, "5 0 obj\n/Helv\nendobj\n")
            .Object(6, "6 0 obj\ntrue\nendobj\n");
        return DocumentReader.Parse(Wrap(pdf, 7));
    }

    private static DocumentReader ImportAndReparse(DocumentReader reader, DocumentObject root)
    {
        using var stream = new MemoryStream();
        var writer = new DocumentWriter(stream);
        var importer = new GraphImporter(reader, writer);
        writer.Trailer["Root"] = importer.ImportInstance(root);
        writer.Close();
        return DocumentReader.Parse(stream.ToArray());
    }

    private static DictionaryObject Field(DocumentReader reader)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));
        return Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Field"]));
    }

    [Fact]
    public void ImportInstance_IndirectString_PreservesValue()
    {
        var source = SourceWithIndirectScalars();
        var catalog = Assert.IsType<DictionaryObject>(source.GetObject(1));

        var round = ImportAndReparse(source, catalog);

        var value = round.Resolve(Field(round)["V"]);
        Assert.Equal("hello", Assert.IsType<StringObject>(value).Value);
    }

    [Fact]
    public void ImportInstance_IndirectNumberNameBoolean_PreserveValues()
    {
        var source = SourceWithIndirectScalars();
        var catalog = Assert.IsType<DictionaryObject>(source.GetObject(1));

        var round = ImportAndReparse(source, catalog);
        var field = Field(round);

        Assert.Equal(42, Assert.IsType<NumberObject>(round.Resolve(field["MaxLen"])).IntValue);
        Assert.Equal("Helv", Assert.IsType<NameObject>(round.Resolve(field["DA"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(round.Resolve(field["Flag"])).Value);
    }

    [Fact]
    public void ImportInstance_IndirectScalar_NotImportedAsEmptyDictionary()
    {
        var source = SourceWithIndirectScalars();
        var catalog = Assert.IsType<DictionaryObject>(source.GetObject(1));

        var round = ImportAndReparse(source, catalog);

        Assert.IsNotType<DictionaryObject>(round.Resolve(Field(round)["V"]));
    }
}
