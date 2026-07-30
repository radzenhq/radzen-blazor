using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// Declares how non-standard structure roles map to standard ISO 32000-1 structure
/// types for Tagged PDF (PDF/UA and PDF/A Level A). A paragraph whose
/// <see cref="Paragraph.StyleName"/> matches a declared role is tagged with that role,
/// and the produced document carries a <c>/StructTreeRoot /RoleMap</c> mapping the role
/// to its standard type so conforming readers can interpret it. Empty by default, in
/// which case no <c>/RoleMap</c> is written.
/// </summary>
public sealed class RoleMap
{
    // ISO 32000-1:2008 Table 333 (standard structure types) and 14.8.4.
    private static readonly string[] StandardTypeNames =
    [
        "Annot", "Art", "BibEntry", "BlockQuote", "Caption", "Code", "Div", "Document",
        "Figure", "Form", "Formula", "H", "H1", "H2", "H3", "H4", "H5", "H6", "Index", "L",
        "LBody", "Lbl", "LI", "Link", "NonStruct", "Note", "P", "Part", "Private", "Quote",
        "RB", "Reference", "RP", "RT", "Ruby", "Sect", "Span", "Table", "TBody", "TD",
        "TFoot", "TH", "THead", "TOC", "TOCI", "TR", "Warichu", "WP", "WT",
    ];

    private static readonly HashSet<string> StandardTypes = new(StandardTypeNames, StringComparer.Ordinal);

    private readonly SortedDictionary<string, string> map = new(StringComparer.Ordinal);

    /// <summary>Gets the number of declared role mappings.</summary>
    public int Count => map.Count;

    /// <summary>
    /// Declares that the custom structure role <paramref name="role"/> maps to the
    /// standard ISO 32000-1 structure type <paramref name="structureType"/> (for
    /// example <c>P</c>, <c>Div</c> or <c>Sect</c>). Declaring the same role again
    /// replaces the mapping.
    /// </summary>
    /// <param name="role">The custom role name used as the structure element type.</param>
    /// <param name="structureType">The standard structure type it maps to.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="role"/> is empty, or <paramref name="structureType"/> is not one of the
    /// standard ISO 32000-1 structure types.
    /// </exception>
    public void Add(string role, string structureType)
    {
        if (string.IsNullOrEmpty(role))
        {
            throw new ArgumentException("The custom role name must be non-empty.", nameof(role));
        }

        if (string.IsNullOrEmpty(structureType))
        {
            throw new ArgumentException("The standard structure type must be non-empty.", nameof(structureType));
        }

        if (!StandardTypes.Contains(structureType))
        {
            throw new ArgumentException(
                $"'{structureType}' is not a standard ISO 32000-1 structure type, so a reader could not "
                + $"interpret the role '{role}' through it. Map the role to one of: "
                + string.Join(", ", StandardTypeNames) + ".",
                nameof(structureType));
        }

        map[role] = structureType;
    }

    /// <summary>Returns whether <paramref name="role"/> has a declared mapping.</summary>
    /// <param name="role">The role name to test.</param>
    /// <returns><see langword="true"/> when the role is declared.</returns>
    public bool Contains(string role) => map.ContainsKey(role);

    internal static bool IsStandardType(string type) => StandardTypes.Contains(type);

    internal IEnumerable<KeyValuePair<string, string>> Entries => map;

}
