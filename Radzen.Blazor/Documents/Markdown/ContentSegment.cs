namespace Radzen.Documents.Markdown;

internal readonly record struct ContentSegment(int ContentStart, int ContentEnd, int SourceStart, int SourceEnd);
