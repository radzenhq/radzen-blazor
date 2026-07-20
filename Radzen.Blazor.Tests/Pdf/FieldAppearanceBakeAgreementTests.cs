#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

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
        var builder = new DocumentBuilder();
        BuildTestSupport.AddText(builder.Sections.Add(), "Body", "Helvetica");
        var document = builder.Build();
        document.FormFields.Add(definition);
        return DocumentReader.Parse(document.ToArray());
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
                break;
        }

        var reader = Reload(definition);

        Assert.False(FormTestSupport.Field(reader, "f").ContainsKey("AP"));
        Assert.True(NeedAppearances(reader));
    }
}
