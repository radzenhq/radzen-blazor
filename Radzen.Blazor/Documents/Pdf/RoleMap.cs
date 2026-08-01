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
    /// Declares that the custom structure role <paramref name="role"/> maps to
    /// <paramref name="structureType"/>. The target is normally a standard ISO 32000-1
    /// structure type (for example <c>P</c>, <c>Div</c> or <c>Sect</c>), but may itself be
    /// another custom role, in which case the chain must terminate at a standard type; that
    /// is checked when the document is saved under PDF/UA or PDF/A Level A. Declaring the
    /// same role again replaces the mapping.
    /// </summary>
    /// <param name="role">The custom role name used as the structure element type.</param>
    /// <param name="structureType">The structure type it maps to.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="role"/> or <paramref name="structureType"/> is empty,
    /// <paramref name="role"/> is itself a standard ISO 32000-1 structure type, or the
    /// mapping would close a cycle.
    /// </exception>
    public void Add(string role, string structureType)
    {
        if (string.IsNullOrEmpty(role))
        {
            throw new ArgumentException("The custom role name must be non-empty.", nameof(role));
        }

        if (string.IsNullOrEmpty(structureType))
        {
            throw new ArgumentException("The structure type must be non-empty.", nameof(structureType));
        }

        // ISO 14289-1:2014 7.1: standard structure types shall not be remapped. Matterhorn Protocol 1.1
        // checkpoint 02-001 fails a file in which one or more standard types are remapped.
        if (StandardTypes.Contains(role))
        {
            throw new ArgumentException(
                $"'{role}' is a standard ISO 32000-1 structure type and standard types shall not be remapped "
                + "(ISO 14289-1 7.1). Give the role a name that is not one of: "
                + string.Join(", ", StandardTypeNames) + ".",
                nameof(role));
        }

        // ISO 14289-1:2014 7.1 and Matterhorn Protocol 1.1 checkpoint 02-003: a circular role mapping
        // never reaches a standard type, so a reader cannot interpret the tag.
        if (Reaches(structureType, role))
        {
            throw new ArgumentException(
                $"Mapping '{role}' to '{structureType}' closes a role mapping cycle {Describe(role, structureType)}, "
                + "so the chain never terminates at a standard ISO 32000-1 structure type.",
                nameof(structureType));
        }

        map[role] = structureType;
    }

    private bool Reaches(string from, string target)
    {
        var current = from;

        while (true)
        {
            if (string.Equals(current, target, StringComparison.Ordinal))
            {
                return true;
            }

            if (!map.TryGetValue(current, out var next))
            {
                return false;
            }

            current = next;
        }
    }

    private string Describe(string role, string structureType)
    {
        var chain = new List<string> { role, structureType };
        var current = structureType;

        while (!string.Equals(current, role, StringComparison.Ordinal) && map.TryGetValue(current, out var next))
        {
            chain.Add(next);
            current = next;
        }

        return string.Join(" -> ", chain);
    }

    /// <summary>Returns whether <paramref name="role"/> has a declared mapping.</summary>
    /// <param name="role">The role name to test.</param>
    /// <returns><see langword="true"/> when the role is declared.</returns>
    public bool Contains(string role) => map.ContainsKey(role);

    internal static bool IsStandardType(string type) => StandardTypes.Contains(type);

    internal IEnumerable<KeyValuePair<string, string>> Entries => map;

    internal RoleMap Snapshot()
    {
        var copy = new RoleMap();
        foreach (var entry in map)
        {
            copy.map[entry.Key] = entry.Value;
        }

        return copy;
    }

}
