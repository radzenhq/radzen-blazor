using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Fonts;

internal interface IPdfFontProgramSource
{
    SfntFont Program { get; }
}

internal interface IPdfEmbeddedFace : IPdfFontProgramSource
{
    Radzen.Documents.Fonts.FontSourceData ProgramData { get; }
}

internal static class PdfFontProgram
{
    public static SfntFont Of<TSource>(in TSource source)
        where TSource : struct, IPdfFontProgramSource
        => source.Program;

    public static Radzen.Documents.Fonts.FontSourceData DataOf<TSource>(in TSource source)
        where TSource : struct, IPdfEmbeddedFace
        => source.ProgramData;
}
