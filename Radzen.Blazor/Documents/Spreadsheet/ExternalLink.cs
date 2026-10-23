#nullable enable

namespace Radzen.Documents.Spreadsheet;

internal sealed record ExternalLink(byte[] Part, byte[]? Relationships);
