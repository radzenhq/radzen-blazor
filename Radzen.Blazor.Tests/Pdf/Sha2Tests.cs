#nullable enable
using System;
using Radzen.Documents;
using Radzen.Documents.Pdf.Crypto;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class Sha2Tests
{
    [Fact]
    public void Sha256Hasher_FinishTwice_Throws()
    {
        var hasher = new Sha256Hasher();
        hasher.Append(TestBytes.Ascii("abc"));
        hasher.Finish();

        Assert.Throws<InvalidOperationException>(() => hasher.Finish());
    }

    [Fact]
    public void Sha256Hasher_AppendAfterFinish_Throws()
    {
        var hasher = new Sha256Hasher();
        hasher.Finish();

        Assert.Throws<InvalidOperationException>(() => hasher.Append(TestBytes.Ascii("abc")));
        Assert.Throws<InvalidOperationException>(() => hasher.Append((byte)0));
    }
}
