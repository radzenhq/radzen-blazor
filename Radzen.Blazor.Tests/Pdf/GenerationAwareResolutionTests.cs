#nullable enable
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.3.10: an indirect reference identifies an object by number and generation.
public class GenerationAwareResolutionTests
{
    private static byte[] FileWithReusedObject()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 5 >>\nstream\nBT ET\nendstream\nendobj\n");
        pdf.Object(5, "5 1 obj\n<< /Type /Reused >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(5), 1))
            .Append("trailer\n<< /Size 6 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Resolve_StaleGeneration_YieldsNull()
    {
        var reader = DocumentReader.Parse(FileWithReusedObject());
        Assert.IsType<NullObject>(reader.Resolve(new ReferenceObject(5, 0)));
    }

    [Fact]
    public void Resolve_MatchingGeneration_ResolvesObject()
    {
        var reader = DocumentReader.Parse(FileWithReusedObject());
        var dictionary = Assert.IsType<DictionaryObject>(reader.Resolve(new ReferenceObject(5, 1)));
        Assert.Equal("Reused", Assert.IsType<NameObject>(dictionary["Type"]).Value);
    }
}
