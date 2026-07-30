#nullable enable
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class DocumentChangeTrackingPublicContractTests
{
    [Fact]
    public void DeepFontMutationIsTrackedOnlyAtTheLeaf()
    {
        var document = new Document();
        var run = document.Sections.Add().Blocks.AddParagraph("text").Inlines[0];

        run.Font.Bold = true;

        Assert.True(run.Font.IsModified);
        Assert.Null(typeof(Document).GetProperty("IsModified"));
    }

    [Fact]
    public void DocumentDoesNotExposeAPublicAcceptChangesOperation()
        => Assert.Null(typeof(Document).GetMethod("AcceptChanges"));
}
