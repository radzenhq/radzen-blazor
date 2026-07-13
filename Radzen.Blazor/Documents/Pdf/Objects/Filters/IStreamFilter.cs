namespace Radzen.Documents.Pdf.Objects.Filters;

/// <summary>
/// A named PDF stream filter that decodes a single link of a <c>/Filter</c> chain.
/// Predictor post-processing (when applicable) is applied inside <see cref="Decode"/>
/// so the pipeline stays a uniform name-to-filter lookup.
/// </summary>
internal interface IStreamFilter
{
    string Name { get; }

    byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput);
}
