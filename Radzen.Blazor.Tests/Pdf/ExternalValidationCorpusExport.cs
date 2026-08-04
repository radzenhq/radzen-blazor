#nullable enable
using System;
using System.IO;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ExternalValidationCorpusExport
{
    [Fact]
    public void Corpus_IsWrittenToTheRequestedDirectory()
    {
        var directory = Environment.GetEnvironmentVariable("RADZEN_EXTERNAL_CORPUS");

        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);

        foreach (var row in DeterminismManifestTests.Corpus())
        {
            var name = (string)row[0];
            var build = (Func<byte[]>)row[1];
            File.WriteAllBytes(Path.Combine(directory, name + ".pdf"), build());
        }
    }

    [Fact]
    public void ForeignResaves_AreWrittenToTheRequestedDirectory()
    {
        var directory = Environment.GetEnvironmentVariable("RADZEN_EXTERNAL_CORPUS");

        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        var foreign = Path.Combine(directory, "foreign");
        Directory.CreateDirectory(foreign);

        foreach (var name in ForeignProducerCorpusTests.Producers)
        {
            var document = ForeignProducerCorpusTests.Load(ForeignProducerCorpusTests.Source(name));
            File.WriteAllBytes(Path.Combine(foreign, name + "-resaved.pdf"), ForeignProducerCorpusTests.Save(document));
        }
    }
}
