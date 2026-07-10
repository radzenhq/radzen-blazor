#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for the sRGB /OutputIntent dictionary (PDF/A GTS_PDFA1).
public class OutputIntentTests
{
    private static DictionaryObject Build() => OutputIntentBuilder.BuildSrgb("sRGB");

    [Fact]
    public void Type_IsOutputIntent()
    {
        Assert.Equal("OutputIntent", ((NameObject)Build()["Type"]).Value);
    }

    [Fact]
    public void S_IsGtsPdfa1()
    {
        Assert.Equal("GTS_PDFA1", ((NameObject)Build()["S"]).Value);
    }

    [Fact]
    public void OutputConditionIdentifier_IsString()
    {
        Assert.Equal("sRGB", ((StringObject)Build()["OutputConditionIdentifier"]).Value);
    }

    [Fact]
    public void OutputConditionIdentifier_HonoursArgument()
    {
        Assert.Equal("Custom", ((StringObject)OutputIntentBuilder.BuildSrgb("Custom")["OutputConditionIdentifier"]).Value);
    }

    [Fact]
    public void DestOutputProfile_IsStream()
    {
        Assert.IsType<StreamObject>(Build()["DestOutputProfile"]);
    }

    [Fact]
    public void DestOutputProfile_NIsThree()
    {
        var profile = (StreamObject)Build()["DestOutputProfile"];
        Assert.Equal(3, ((NumberObject)profile.Dictionary["N"]).IntValue);
    }

    [Fact]
    public void DestOutputProfile_BytesEqualSrgbProfile()
    {
        var profile = (StreamObject)Build()["DestOutputProfile"];
        Assert.Equal(SrgbIccProfile.GetBytes(), profile.Data);
    }

    [Fact]
    public void DestOutputProfile_HasNoFilter()
    {
        var profile = (StreamObject)Build()["DestOutputProfile"];
        Assert.False(profile.Dictionary.ContainsKey("Filter"));
    }
}
