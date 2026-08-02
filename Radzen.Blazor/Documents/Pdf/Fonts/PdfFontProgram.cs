using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Fonts;

internal interface IPdfFontProgramSource
{
    SfntFont Program { get; }
}

internal static class PdfFontProgram
{
    public static SfntFont Of<TSource>(in TSource source)
        where TSource : struct, IPdfFontProgramSource
        => source.Program;
}
