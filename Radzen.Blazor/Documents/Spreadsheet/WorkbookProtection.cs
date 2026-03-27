namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Represents workbook-level protection settings matching the XLSX workbookProtection element.
/// </summary>
public class WorkbookProtection
{
    /// <summary>
    /// Gets or sets whether the workbook structure is locked (prevents adding, deleting, renaming, or reordering sheets).
    /// </summary>
    public bool LockStructure { get; set; }

    /// <summary>Gets or sets the legacy 16-bit password hash (4 hex characters).</summary>
    public string? PasswordHash { get; set; }

    /// <summary>Gets or sets the hash algorithm name (e.g. "SHA-512").</summary>
    public string? AlgorithmName { get; set; }

    /// <summary>Gets or sets the base64-encoded password hash value.</summary>
    public string? HashValue { get; set; }

    /// <summary>Gets or sets the base64-encoded salt value.</summary>
    public string? SaltValue { get; set; }

    /// <summary>Gets or sets the number of hash iterations.</summary>
    public int? SpinCount { get; set; }
}
