#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class AcroFormFieldTreeImportTests
{
    private static byte[] NestedFormSource()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [6 0 R 7 0 R 9 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R 9 0 R] /NeedAppearances true /DA (/Helv 0 Tf 0 g) /DR << /Font << /Helv 8 0 R >> >> >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /T (address) /FT /Tx /Kids [6 0 R 7 0 R] >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Widget /T (city) /Parent 5 0 R /V (Sofia) /P 3 0 R /Rect [100 700 350 720] >>\nendobj\n")
            .Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Widget /T (zip) /Parent 5 0 R /V (1000) /P 3 0 R /Rect [100 660 350 680] >>\nendobj\n")
            .Object(8, "8 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Name /Helv >>\nendobj\n")
            .Object(9, "9 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V (Anna) /P 3 0 R /Rect [100 600 350 620] >>\nendobj\n");
        return FixturePdf.Wrap(pdf, 10);
    }

    private static byte[] Merge(bool destinationHasNameField, bool destinationHasDa)
    {
        var reader = DocumentReader.Parse(NestedFormSource());
        using var stream = new MemoryStream();
        var writer = new DocumentWriter(stream);

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var catalogRef = writer.Add(catalog);
        var pagesNode = new DictionaryObject { ["Type"] = new NameObject("Pages") };
        var pagesRef = writer.Add(pagesNode);
        var pageNode = new DictionaryObject
        {
            ["Type"] = new NameObject("Page"),
            ["Parent"] = pagesRef,
            ["MediaBox"] = new ArrayObject
            {
                new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
            },
        };
        var pageRef = writer.Add(pageNode);
        pagesNode["Kids"] = new ArrayObject { pageRef };
        pagesNode["Count"] = new NumberObject(1);
        catalog["Pages"] = pagesRef;

        var form = new DictionaryObject
        {
            ["DR"] = new DictionaryObject
            {
                ["Font"] = new DictionaryObject
                {
                    ["DestF"] = new DictionaryObject
                    {
                        ["Type"] = new NameObject("Font"),
                        ["Subtype"] = new NameObject("Type1"),
                        ["BaseFont"] = new NameObject("Courier"),
                    },
                },
            },
        };
        if (destinationHasDa)
        {
            form["DA"] = new StringObject("/DestF 0 Tf 0 g");
        }

        var fields = new ArrayObject();
        var usedNames = new HashSet<string>(StringComparer.Ordinal);
        var annotations = new ArrayObject();

        if (destinationHasNameField)
        {
            var existing = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Widget"),
                ["FT"] = new NameObject("Tx"),
                ["T"] = new StringObject("Name"),
                ["V"] = new StringObject("existing"),
                ["P"] = pageRef,
                ["Rect"] = new ArrayObject
                {
                    new NumberObject(100), new NumberObject(560), new NumberObject(350), new NumberObject(580),
                },
            };
            GraphImporter.DisambiguateFieldName(existing, "Name", usedNames);
            var existingRef = writer.Add(existing);
            fields.Add(existingRef);
            annotations.Add(existingRef);
        }

        var sourceCatalog = (DictionaryObject)reader.Resolve(reader.Trailer["Root"]!);
        var sourcePages = (DictionaryObject)reader.Resolve(sourceCatalog["Pages"]);
        var sourcePage = (DictionaryObject)reader.Resolve(((ArrayObject)reader.Resolve(sourcePages["Kids"]))[0]);
        var sourceForm = (DictionaryObject)reader.Resolve(sourceCatalog["AcroForm"]);
        var sourceAnnotations = (ArrayObject)reader.Resolve(sourcePage["Annots"]);

        var importer = new GraphImporter(reader, writer);
        importer.Seed(sourcePage, pageRef);

        foreach (var annotation in sourceAnnotations)
        {
            annotations.Add(importer.ImportValue(annotation));
        }

        pageNode["Annots"] = annotations;

        foreach (var annotation in sourceAnnotations)
        {
            if (reader.Resolve(annotation) is DictionaryObject dict
                && importer.TryImportFieldRoot(dict, out var reference, out var field, out var name))
            {
                GraphImporter.DisambiguateFieldName(field!, name, usedNames);
                fields.Add(reference);
            }
        }

        importer.MergeFormDefaults(form, sourceForm);
        form["Fields"] = fields;
        catalog["AcroForm"] = writer.Add(form);
        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return stream.ToArray();
    }

    private static ArrayObject Fields(DocumentReader reader)
        => (ArrayObject)reader.Resolve(FormTestSupport.AcroForm(reader)["Fields"]);

    private static string Title(DocumentReader reader, DictionaryObject field)
        => Assert.IsType<StringObject>(reader.Resolve(field["T"])).Value;

    [Fact]
    public void NestedTreeRootIsListedOnceWithBothKids()
    {
        var reader = DocumentReader.Parse(Merge(destinationHasNameField: false, destinationHasDa: true));
        var fields = Fields(reader);

        Assert.Equal(2, fields.Count);
        var root = (DictionaryObject)reader.Resolve(fields[0]);
        Assert.Equal("address", Title(reader, root));

        var kids = (ArrayObject)reader.Resolve(root["Kids"]);
        Assert.Equal(2, kids.Count);
        Assert.Equal(
            ["city", "zip"],
            kids.Select(kid => Title(reader, (DictionaryObject)reader.Resolve(kid))).ToArray());

        var rootNumber = Assert.IsType<ReferenceObject>(fields[0]).ObjectNumber;
        foreach (var kid in kids)
        {
            var parent = ((DictionaryObject)reader.Resolve(kid))["Parent"];
            Assert.Equal(rootNumber, Assert.IsType<ReferenceObject>(parent).ObjectNumber);
        }
    }

    [Fact]
    public void KidWidgetsStayOnThePageAndAreTheKidObjects()
    {
        var reader = DocumentReader.Parse(Merge(destinationHasNameField: false, destinationHasDa: true));
        var fields = Fields(reader);
        var root = (DictionaryObject)reader.Resolve(fields[0]);
        var kids = (ArrayObject)reader.Resolve(root["Kids"]);

        var annotations = ((ArrayObject)reader.Resolve(FormTestSupport.FirstPage(reader)["Annots"]))
            .Select(entry => Assert.IsType<ReferenceObject>(entry).ObjectNumber)
            .ToArray();
        foreach (var kid in kids)
        {
            Assert.Contains(Assert.IsType<ReferenceObject>(kid).ObjectNumber, annotations);
        }
    }

    [Fact]
    public void QualifiedNamesResolveAndNoFieldIsDropped()
    {
        var document = PortableDocument.LoadFromStream(
            new MemoryStream(Merge(destinationHasNameField: false, destinationHasDa: true)));

        Assert.NotNull(document.AcroForm);
        Assert.Equal(3, document.AcroForm!.FieldNames.Count);
        Assert.Contains("address.city", document.AcroForm.FieldNames);
        Assert.Contains("address.zip", document.AcroForm.FieldNames);
        Assert.Contains("Name", document.AcroForm.FieldNames);
        Assert.DoesNotContain("address", document.AcroForm.FieldNames);
    }

    [Fact]
    public void DefaultResourceFontsAreUnionedAndDestinationDaWins()
    {
        var reader = DocumentReader.Parse(Merge(destinationHasNameField: false, destinationHasDa: true));
        var form = FormTestSupport.AcroForm(reader);

        var resources = (DictionaryObject)reader.Resolve(form["DR"]);
        var fonts = (DictionaryObject)reader.Resolve(resources["Font"]);
        Assert.True(fonts.ContainsKey("DestF"));
        Assert.True(fonts.ContainsKey("Helv"));
        var helv = (DictionaryObject)reader.Resolve(fonts["Helv"]);
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(reader.Resolve(helv["BaseFont"])).Value);

        Assert.Equal("/DestF 0 Tf 0 g", Assert.IsType<StringObject>(reader.Resolve(form["DA"])).Value);
        Assert.True(Assert.IsType<BooleanObject>(reader.Resolve(form["NeedAppearances"])).Value);
    }

    [Fact]
    public void SourceDaIsAdoptedWhenDestinationHasNone()
    {
        var reader = DocumentReader.Parse(Merge(destinationHasNameField: false, destinationHasDa: false));
        var form = FormTestSupport.AcroForm(reader);

        Assert.Equal("/Helv 0 Tf 0 g", Assert.IsType<StringObject>(reader.Resolve(form["DA"])).Value);
    }

    [Fact]
    public void CollidingTopLevelNameIsSuffixedAndBothFieldsSurvive()
    {
        var bytes = Merge(destinationHasNameField: true, destinationHasDa: true);
        var reader = DocumentReader.Parse(bytes);
        var fields = Fields(reader);

        Assert.Equal(3, fields.Count);
        var titles = fields
            .Select(entry => Title(reader, (DictionaryObject)reader.Resolve(entry)))
            .ToArray();
        Assert.Equal(["Name", "address", "Name_2"], titles);

        var document = PortableDocument.LoadFromStream(new MemoryStream(bytes));
        Assert.Contains("Name", document.AcroForm!.FieldNames);
        Assert.Contains("Name_2", document.AcroForm.FieldNames);
        Assert.Equal(4, document.AcroForm.FieldNames.Count);
    }

    [Fact]
    public void DisambiguationPicksTheSmallestUnusedSuffix()
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { "Name", "Name_2" };
        var field = new DictionaryObject { ["T"] = new StringObject("Name") };

        GraphImporter.DisambiguateFieldName(field, "Name", used);

        Assert.Equal("Name_3", Assert.IsType<StringObject>(field["T"]).Value);
        Assert.Contains("Name_3", used);
    }
}
