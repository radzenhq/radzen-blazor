namespace Radzen.Documents.Pdf;

// /Ff form field flags, ISO 32000-1 tables 226, 228 and 230; spec bit numbers are 1-based, shifts here 0-based.
internal static class FieldFlags
{
    internal const int Required = 1 << 1;    // spec bit 2
    internal const int Radio = 1 << 15;      // spec bit 16
    internal const int PushButton = 1 << 16; // spec bit 17
    internal const int Combo = 1 << 17;      // spec bit 18
    internal const int Multiline = 1 << 12;  // spec bit 13
    internal const int Password = 1 << 13;   // spec bit 14
    internal const int Comb = 1 << 24;       // spec bit 25
}
