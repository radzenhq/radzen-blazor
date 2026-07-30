#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class AttachmentPayloadImmutabilityTests
{
    [Fact]
    public void MutatingTheCallerArrayDoesNotChangeTheStoredPayload()
    {
        var document = new Document();
        var data = Encoding.UTF8.GetBytes("payload");
        var builderRenderer = new DocumentRenderer();
        var attachment = builderRenderer.Attachments.Add("data.bin", data, AttachmentRelationship.Supplement, "application/octet-stream");

        data[0] = (byte)'X';

        Assert.Equal("payload", Encoding.UTF8.GetString(attachment.GetBytes()));
    }

    [Fact]
    public void MutatingTheCallerArrayDoesNotFlipIsModified()
    {
        var document = new PortableDocument();
        var data = Encoding.UTF8.GetBytes("payload");
        var attachment = document.Attachments.Add("data.bin", data, AttachmentRelationship.Supplement, "application/octet-stream");
        document.Attachments.AcceptChanges();

        data[0] = (byte)'X';

        Assert.False(attachment.IsModified);
        Assert.Equal("payload", Encoding.UTF8.GetString(attachment.GetBytes()));
    }
}
