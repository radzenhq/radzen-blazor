#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldAppearanceBakeAgreementTests
{
    private static TextFieldDefinition Field() => new("f")
    {
        X = 100,
        Y = 700,
        Width = 200,
        Height = 20,
        Value = "secret",
    };

    private static DocumentReader Reload(TextFieldDefinition definition)
    {
        var document = new Document();
        BuildTestSupport.AddText(document.Sections.Add(), "Body", "Helvetica");
        var pdf = new DocumentRenderer().Render(document);
        pdf.FormFields.Add(definition);
        return DocumentReader.Parse(pdf.ToArray());
    }

    private static bool NeedAppearances(DocumentReader reader)
        => FormTestSupport.AcroForm(reader).TryGetValue("NeedAppearances", out var need)
            && reader.Resolve(need!) is BooleanObject flag && flag.Value;

    [Fact]
    public void PlainCreatedTextField_GetsABakedAppearance()
    {
        var reader = Reload(Field());

        Assert.True(FormTestSupport.Field(reader, "f").ContainsKey("AP"));
        Assert.False(NeedAppearances(reader));
    }

    [Theory]
    [InlineData("password")]
    [InlineData("multiline")]
    [InlineData("comb")]
    public void NonSingleLineCreatedField_DoesNotBakeASingleLineAppearance(string flag)
    {
        var definition = Field();
        switch (flag)
        {
            case "password":
                definition.Password = true;
                break;
            case "multiline":
                definition.Multiline = true;
                break;
            case "comb":
                definition.Comb = true;
                definition.MaxLength = 6;
                break;
        }

        var reader = Reload(definition);

        Assert.False(FormTestSupport.Field(reader, "f").ContainsKey("AP"));
        Assert.True(NeedAppearances(reader));
    }
}
