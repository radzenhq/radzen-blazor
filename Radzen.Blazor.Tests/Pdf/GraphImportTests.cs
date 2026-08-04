#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class GraphImportTests
{
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
        return DocumentReader.Parse(FixturePdf.Wrap(pdf, 7));
    }

    private static string ImportedCatalogEmission()
    {
        var source = SourceWithIndirectScalars();
        var root = Assert.IsType<DictionaryObject>(source.GetObject(1));

        using var stream = new MemoryStream();
        var writer = new DocumentWriter(stream);
        var importer = new GraphImporter(source, writer);
        writer.Trailer["Root"] = importer.ImportInstance(root);
        writer.Close();

        return Encoding.Latin1.GetString(stream.ToArray());
    }

    private static string ImportedField(string emission)
    {
        var catalog = IndirectObject(
            emission,
            Shaped("trailer", @"/Root (\d+) 0 R", emission).Groups[1].Value);

        return IndirectObject(
            emission,
            Shaped("imported catalog", @"/Field (\d+) 0 R", catalog).Groups[1].Value);
    }

    private static string Entry(string emission, string field, string key)
        => IndirectObject(
            emission,
            Shaped($"imported field /{key}", $@"/{key} (\d+) 0 R", field).Groups[1].Value);

    [Fact]
    public void ImportInstance_IndirectString_PreservesValue()
    {
        var emission = ImportedCatalogEmission();

        Shaped("imported /V", @"^\(hello\)$", Entry(emission, ImportedField(emission), "V"));
    }

    [Fact]
    public void ImportInstance_IndirectNumberNameBoolean_PreserveValues()
    {
        var emission = ImportedCatalogEmission();
        var field = ImportedField(emission);

        Shaped("imported /MaxLen", @"^42$", Entry(emission, field, "MaxLen"));
        Shaped("imported /DA", @"^/Helv$", Entry(emission, field, "DA"));
        Shaped("imported /Flag", @"^true$", Entry(emission, field, "Flag"));
    }

    [Fact]
    public void ImportInstance_IndirectScalar_NotImportedAsEmptyDictionary()
    {
        var emission = ImportedCatalogEmission();

        Lacks("imported /V", "<<", Entry(emission, ImportedField(emission), "V"));
    }
}
