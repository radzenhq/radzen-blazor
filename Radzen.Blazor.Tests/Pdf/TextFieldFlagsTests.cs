#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class TextFieldFlagsTests
{
    private const int MultilineFlag = 1 << 12;
    private const int PasswordFlag = 1 << 13;
    private const int CombFlag = 1 << 24;

    private static PortableDocument BuildDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Form", "Helvetica");
        return new DocumentRenderer().Render(document);
    }

    private static TextFieldDefinition PlainField()
        => new("Plain") { X = 100, Y = 700, Width = 250, Height = 20, Value = "hello" };

    private static string Field(PortableDocument document, string name)
        => Line(Emit(document), $"/T ({name})");

    private static int FlagsOf(string field)
        => field.Contains("/Ff ", StringComparison.Ordinal) ? NumberIn(field, "Ff") : 0;

    [Fact]
    public void MultilineFieldSetsFfBit13()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Notes")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 80,
            Value = "line",
            Multiline = true,
        });

        CarriesFlag("Notes field", Field(document, "Notes"), "Ff", MultilineFlag);
    }

    [Fact]
    public void PasswordFieldSetsFfBit14()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Secret")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 20,
            Password = true,
        });

        CarriesFlag("Secret field", Field(document, "Secret"), "Ff", PasswordFlag);
    }

    [Fact]
    public void CombFieldSetsFfBit25AndMaxLen()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Code")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 20,
            Value = "ABCDE",
            Comb = true,
            MaxLength = 5,
        });

        var field = Field(document, "Code");

        CarriesFlag("Code field", field, "Ff", CombFlag);
        Assert.Equal(5, NumberIn(field, "MaxLen"));
    }

    [Fact]
    public void CombinedFlagsAccumulateIntoOneFfInteger()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Both")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 40,
            Multiline = true,
            Password = true,
        });

        Assert.Equal(MultilineFlag | PasswordFlag, FlagsOf(Field(document, "Both")));
    }

    [Fact]
    public void MaxLengthAloneSetsMaxLenWithoutFf()
    {
        var document = BuildDocument();
        document.FormFields.Add(new TextFieldDefinition("Pin")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 20,
            MaxLength = 4,
        });

        var field = Field(document, "Pin");

        Assert.Equal(4, NumberIn(field, "MaxLen"));
        Assert.Equal(0, FlagsOf(field));
    }

    [Fact]
    public void PlainTextFieldCarriesNoFfOrMaxLen()
    {
        var document = BuildDocument();
        document.FormFields.Add(PlainField());

        var field = Field(document, "Plain");

        Lacks("Plain field", "/Ff", field);
        Lacks("Plain field", "/MaxLen", field);
    }

    [Fact]
    public void PlainTextFieldSavesByteIdentically()
    {
        byte[] Build()
        {
            var document = BuildDocument();
            document.FormFields.Add(PlainField());
            return document.ToArray();
        }

        Assert.Equal(Build(), Build());
    }
}
