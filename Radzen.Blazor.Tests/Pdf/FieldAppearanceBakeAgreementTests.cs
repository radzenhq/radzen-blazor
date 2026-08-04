#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string Emission(TextFieldDefinition definition)
    {
        var document = new Document();
        BuildTestSupport.AddText(document.Sections.Add(), "Body", "Helvetica");
        var pdf = new DocumentRenderer().Render(document);
        pdf.FormFields.Add(definition);
        return Emit(pdf);
    }

    [Fact]
    public void PlainCreatedTextField_GetsABakedAppearance()
    {
        var emission = Emission(Field());

        Carries("text field", "/AP ", Line(emission, "/T (f)"));
        Lacks("AcroForm", "/NeedAppearances true", Line(emission, "/Fields ["));
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

        var emission = Emission(definition);

        Lacks("text field", "/AP ", Line(emission, "/T (f)"));
        Carries("AcroForm", "/NeedAppearances true", Line(emission, "/Fields ["));
    }
}
