using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class ExternalLinkXlsxRoundTripTests
{
    private static readonly XNamespace Main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private static readonly XNamespace Rels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace ContentTypes = "http://schemas.openxmlformats.org/package/2006/content-types";

    private const string ExternalLinkXml = """<externalLink xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><externalBook r:id="rId1"><sheetNames><sheetName val="Misc Breakdown"/></sheetNames><sheetDataSet><sheetData sheetId="0"><row r="1"><cell r="A1"><v>42</v></cell></row></sheetData></sheetDataSet></externalBook></externalLink>""";

    private const string ExternalLinkRelsXml = """<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLinkPath" Target="file:///C:/Estimates/Breakdown.xlsx" TargetMode="External"/></Relationships>""";

    private static MemoryStream CreatePackageWithExternalLink()
    {
        var ms = new MemoryStream();

        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(archive, "xl/workbook.xml", """
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Sheet1" sheetId="1" r:id="rId1" /></sheets>
                  <externalReferences><externalReference r:id="rId7" /></externalReferences>
                  <definedNames><definedName name="Piping">'[1]Misc Breakdown'!$A$1:$G$51</definedName></definedNames>
                </workbook>
                """);
            Add(archive, "xl/_rels/workbook.xml.rels", """
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml" />
                  <Relationship Id="rId7" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink" Target="links/book.xml" />
                </Relationships>
                """);
            Add(archive, "xl/worksheets/sheet1.xml", """
                <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <sheetData><row r="1"><c r="A1"><f>'[1]Misc Breakdown'!A1+1</f><v>43</v></c></row></sheetData>
                </worksheet>
                """);
            Add(archive, "xl/links/book.xml", ExternalLinkXml);
            Add(archive, "xl/links/_rels/book.xml.rels", ExternalLinkRelsXml);
        }

        ms.Position = 0;
        return ms;
    }

    private static void Add(ZipArchive archive, string name, string content)
    {
        using var stream = archive.CreateEntry(name).Open();
        stream.Write(Encoding.UTF8.GetBytes(content));
    }

    private static ZipArchive Resave(MemoryStream source)
    {
        var workbook = Workbook.LoadFromStream(source);
        var saved = new MemoryStream();
        workbook.SaveToStream(saved);
        saved.Position = 0;

        return new ZipArchive(saved, ZipArchiveMode.Read);
    }

    private static string ReadText(ZipArchive archive, string path)
    {
        using var reader = new StreamReader(archive.GetEntry(path)!.Open());
        return reader.ReadToEnd();
    }

    private static XDocument ReadXml(ZipArchive archive, string path)
    {
        using var stream = archive.GetEntry(path)!.Open();
        return XDocument.Load(stream);
    }

    [Fact]
    public void ExternalLinkPartAndItsRelationshipsSurviveRoundTrip()
    {
        using var archive = Resave(CreatePackageWithExternalLink());

        Assert.Equal(ExternalLinkXml, ReadText(archive, "xl/externalLinks/externalLink1.xml"));
        Assert.Equal(ExternalLinkRelsXml, ReadText(archive, "xl/externalLinks/_rels/externalLink1.xml.rels"));
    }

    [Fact]
    public void WorkbookReferencesTheExternalLinkBeforeDefinedNames()
    {
        using var archive = Resave(CreatePackageWithExternalLink());

        var workbook = ReadXml(archive, "xl/workbook.xml").Root!;
        var children = workbook.Elements().Select(element => element.Name.LocalName).ToList();
        var reference = Assert.Single(workbook.Descendants(Main + "externalReference"));
        var relationship = ReadXml(archive, "xl/_rels/workbook.xml.rels").Descendants(Rels + "Relationship")
            .Single(element => element.Attribute("Id")?.Value == reference.Attribute(R + "id")?.Value);

        Assert.Equal(children.IndexOf("sheets") + 1, children.IndexOf("externalReferences"));
        Assert.Equal(children.IndexOf("externalReferences") + 1, children.IndexOf("definedNames"));
        Assert.Equal("http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink", relationship.Attribute("Type")?.Value);
        Assert.Equal("externalLinks/externalLink1.xml", relationship.Attribute("Target")?.Value);
    }

    [Fact]
    public void ExternalLinkPartHasItsContentType()
    {
        using var archive = Resave(CreatePackageWithExternalLink());

        var contentType = ReadXml(archive, "[Content_Types].xml").Descendants(ContentTypes + "Override")
            .Single(element => element.Attribute("PartName")?.Value == "/xl/externalLinks/externalLink1.xml");

        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.externalLink+xml", contentType.Attribute("ContentType")?.Value);
    }

    [Fact]
    public void NamesAndFormulasReferringToTheExternalWorkbookSurviveRoundTrip()
    {
        using var archive = Resave(CreatePackageWithExternalLink());

        var definedName = Assert.Single(ReadXml(archive, "xl/workbook.xml").Descendants(Main + "definedName"));
        var formula = Assert.Single(ReadXml(archive, "xl/worksheets/sheet1.xml").Descendants(Main + "f"));

        Assert.Equal("'[1]Misc Breakdown'!$A$1:$G$51", definedName.Value);
        Assert.Equal("'[1]Misc Breakdown'!A1+1", formula.Value);
    }

    [Fact]
    public void WorkbookWithoutExternalLinksWritesNoExternalReferences()
    {
        var workbook = new Workbook();
        workbook.AddSheet("Sheet1", 5, 5);
        using var saved = new MemoryStream();
        workbook.SaveToStream(saved);
        saved.Position = 0;
        using var archive = new ZipArchive(saved, ZipArchiveMode.Read);

        Assert.Empty(ReadXml(archive, "xl/workbook.xml").Descendants(Main + "externalReferences"));
        Assert.DoesNotContain(archive.Entries, entry => entry.FullName.StartsWith("xl/externalLinks/", System.StringComparison.Ordinal));
    }
}
