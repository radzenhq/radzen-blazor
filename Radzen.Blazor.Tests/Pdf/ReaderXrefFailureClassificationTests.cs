#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ReaderXrefFailureClassificationTests
{
    private static byte[] ValidDocument()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (body) Tj ET"));
        return document.ToArray();
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(InvalidCastException))]
    [InlineData(typeof(IndexOutOfRangeException))]
    public void Load_LetsFrameworkLogicErrorsFromTheXrefPathPropagate(Type exceptionType)
    {
        var data = ValidDocument();
        var failure = (Exception)Activator.CreateInstance(exceptionType)!;

        var thrown = Record.Exception(() =>
            DocumentReader.ParseWithXrefLoad(data, () => throw failure));

        Assert.Same(failure, thrown);
    }

    [Theory]
    [InlineData(typeof(DocumentParseException))]
    [InlineData(typeof(EndOfStreamException))]
    [InlineData(typeof(InvalidDataException))]
    [InlineData(typeof(FormatException))]
    public void Load_RepairsWhenTheXrefPathReportsMalformedInput(Type exceptionType)
    {
        var data = ValidDocument();
        var failure = exceptionType == typeof(DocumentParseException)
            ? new DocumentParseException("malformed", -1)
            : (Exception)Activator.CreateInstance(exceptionType)!;

        var reader = DocumentReader.ParseWithXrefLoad(data, () => throw failure);

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }
}
