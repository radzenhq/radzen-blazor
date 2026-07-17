#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TextFieldFlagsTests
{
    private const int MultilineFlag = 1 << 12;
    private const int PasswordFlag = 1 << 13;
    private const int CombFlag = 1 << 24;

    private static Document BuildDocument()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Form", "Helvetica");
        return builder.Build();
    }

    private static TextFieldDefinition PlainField()
        => new("Plain") { X = 100, Y = 700, Width = 250, Height = 20, Value = "hello" };

    private static DocumentReader Reload(Document document)
        => DocumentReader.Parse(document.ToArray());

    private static int FlagsOf(DocumentReader reader, DictionaryObject field)
        => field.TryGetValue("Ff", out var ff) && reader.Resolve(ff!) is NumberObject flags ? flags.IntValue : 0;

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

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Notes");

        Assert.Equal(MultilineFlag, FlagsOf(reader, field) & MultilineFlag);
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

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Secret");

        Assert.Equal(PasswordFlag, FlagsOf(reader, field) & PasswordFlag);
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

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Code");

        Assert.Equal(CombFlag, FlagsOf(reader, field) & CombFlag);
        var maxLen = Assert.IsType<NumberObject>(reader.Resolve(field["MaxLen"]));
        Assert.Equal(5, maxLen.IntValue);
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

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Both");

        Assert.Equal(MultilineFlag | PasswordFlag, FlagsOf(reader, field));
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

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Pin");

        Assert.Equal(4, Assert.IsType<NumberObject>(reader.Resolve(field["MaxLen"])).IntValue);
        Assert.Equal(0, FlagsOf(reader, field));
    }

    [Fact]
    public void PlainTextFieldCarriesNoFfOrMaxLen()
    {
        var document = BuildDocument();
        document.FormFields.Add(PlainField());

        var reader = Reload(document);
        var field = FormTestSupport.Field(reader, "Plain");

        Assert.False(field.ContainsKey("Ff"));
        Assert.False(field.ContainsKey("MaxLen"));
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
