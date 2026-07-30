#nullable enable
using System.IO;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class ColorChannelAgreementTests
{
    private static byte[] FileWithSameColorEverywhere(string component)
    {
        var content = $"{component} {component} {component} rg 10 10 100 50 re f\n";
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Outlines 5 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R /Annots [7 0 R] >>\nendobj\n");
        pdf.Object(4, $"4 0 obj\n<< /Length {content.Length} >>\nstream\n{content}endstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Outlines /First 6 0 R /Last 6 0 R /Count 1 >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Title (Item) /Parent 5 0 R /Dest [3 0 R /Fit] "
            + $"/C [{component} {component} {component}] >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Square /Rect [10 160 110 210] "
            + $"/C [{component} {component} {component}] >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i <= 7; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    [Theory]
    [InlineData("0.3")]
    [InlineData("0.7")]
    [InlineData("0.00196078431372549")]
    public void Load_SameComponentEverywhere_AllReadersAgree(string component)
    {
        var document = Load(FileWithSameColorEverywhere(component));

        var outline = document.Outline[0].Color!.Value;
        var annotation = Assert.IsType<SquareAnnotation>(document.Pages[0].Annotations[0]).Color;
        var fill = document.Pages[0].Content.OfType<PathContent>().Single().FillColor;

        Assert.Equal(outline.R, annotation.R);
        Assert.Equal(outline.R, fill.R);
        Assert.Equal(outline, annotation);
        Assert.Equal(outline, fill);
    }
}
