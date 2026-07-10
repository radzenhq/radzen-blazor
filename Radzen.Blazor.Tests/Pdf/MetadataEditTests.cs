#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class MetadataEditTests
{
    [Fact]
    public void SetTitleRoundTripsThroughInfoDictionary()
    {
        var document = FormTestSupport.LoadFixture();
        document.Info.Title = "Signed Agreement";

        var reader = FormTestSupport.Reload(document);

        Assert.True(reader.Trailer.TryGetValue("Info", out var infoObject));
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(infoObject!));
        Assert.True(info.TryGetValue("Title", out var title));
        Assert.Equal("Signed Agreement", Assert.IsType<StringObject>(reader.Resolve(title!)).Value);
    }

    [Fact]
    public void SetTitleRoundTripsThroughPublicApi()
    {
        var document = FormTestSupport.LoadFixture();
        document.Info.Title = "Signed Agreement";
        document.Info.Author = "Radzen Ltd";

        var reloaded = Document.LoadFromStream(new System.IO.MemoryStream(FormTestSupport.Save(document)));

        Assert.Equal("Signed Agreement", reloaded.Info.Title);
        Assert.Equal("Radzen Ltd", reloaded.Info.Author);
    }

    [Fact]
    public void EditingMetadataDoesNotDropTheForm()
    {
        var document = FormTestSupport.LoadFixture();
        document.Info.Title = "Signed Agreement";

        var reader = FormTestSupport.Reload(document);
        var catalog = FormTestSupport.Catalog(reader);

        Assert.True(catalog.TryGetValue("AcroForm", out _));
    }

    [Fact]
    public void XmpMetadataStaysConsistentWithInfoWhenPresent()
    {
        var document = FormTestSupport.LoadFixture();
        document.Info.Title = "Signed Agreement";

        var reader = FormTestSupport.Reload(document);
        var catalog = FormTestSupport.Catalog(reader);

        if (catalog.TryGetValue("Metadata", out var metadataObject)
            && reader.Resolve(metadataObject!) is StreamObject xmp)
        {
            Assert.Contains("Signed Agreement", FormTestSupport.Decode(xmp));
        }
    }
}
