using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A named style definition. Inheritance is resolved at layout time; this type only stores the structure.
/// </summary>
/// <remarks>
/// Every formatting value a style carries is resolved with the same precedence: the direct local value
/// set on the element itself wins; otherwise the nearest style in the element's named style chain that
/// sets the value wins, walking from the applied style through its <see cref="BaseStyle"/> ancestors up
/// to <c>Normal</c>; otherwise the built-in default applies. A style name that does not exist, and a
/// <see cref="BaseStyle"/> chain that loops back on itself, both fail at layout with the offending name
/// and the full reference chain.
/// </remarks>
public sealed class Style
{
    internal Style(string name, string? baseStyle)
    {
        Name = name;
        BaseStyle = baseStyle;
    }

    /// <summary>Gets the unique style name.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the name of the style this one inherits from, or <see langword="null"/> for none.</summary>
    public string? BaseStyle { get; set; }

    /// <summary>Gets the style font.</summary>
    public Font Font { get; } = new();

    /// <summary>
    /// Gets or sets the horizontal alignment this style sets itself, or <see langword="null"/> when it sets
    /// none and the value is inherited from its base style chain. Setting <see langword="null"/> resets it.
    /// </summary>
    public HorizontalAlignment? Alignment { get; set; }

    /// <summary>
    /// Gets or sets the spacing before a paragraph this style sets itself, or <see langword="null"/> when it
    /// sets none and the value is inherited from its base style chain. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? SpacingBefore
    {
        get => spacingBefore;
        set => spacingBefore = AuthoredNumber.Absolute(value, "Style.SpacingBefore");
    }

    /// <summary>
    /// Gets or sets the spacing after a paragraph this style sets itself, or <see langword="null"/> when it
    /// sets none and the value is inherited from its base style chain. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? SpacingAfter
    {
        get => spacingAfter;
        set => spacingAfter = AuthoredNumber.Absolute(value, "Style.SpacingAfter");
    }

    /// <summary>
    /// Gets or sets the left indent this style sets itself, or <see langword="null"/> when it sets none and
    /// the value is inherited from its base style chain. Setting <see langword="null"/> resets it.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit? LeftIndent
    {
        get => leftIndent;
        set => leftIndent = AuthoredNumber.Absolute(value, "Style.LeftIndent");
    }

    /// <summary>
    /// Gets or sets the keep-together flag this style sets itself, or <see langword="null"/> when it sets
    /// none and the value is inherited from its base style chain. Setting <see langword="null"/> resets it.
    /// </summary>
    public bool? KeepTogether { get; set; }

    private int? headingLevel;
    private Unit? spacingBefore;
    private Unit? spacingAfter;
    private Unit? leftIndent;

    /// <summary>
    /// Gets or sets the heading level a paragraph in this style carries, from 1 to 6, or
    /// <see langword="null"/> when it sets none and the value is inherited from its base style chain.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the range 1 to 6.</exception>
    public int? HeadingLevel
    {
        get => headingLevel;
        set
        {
            if (value is { } level and (< 1 or > 6))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value), level, "Style.HeadingLevel must be between 1 and 6, or null for a style that is not a heading.");
            }

            headingLevel = value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether a paragraph is kept with the next block this style sets
    /// itself, or <see langword="null"/> when it sets none and the value is inherited from its base style
    /// chain. Setting <see langword="null"/> resets it.
    /// </summary>
    public bool? KeepWithNext { get; set; }

    private string? role;

    /// <summary>
    /// Gets or sets the semantic role a paragraph in this style carries for assistive technology and
    /// structured output, or <see langword="null"/> when it sets none and the value is inherited from
    /// its base style chain. A style without a role falls back to its <see cref="Name"/>. A renderer
    /// decides how to express the role and which role names it accepts; a name outside the
    /// renderer's role vocabulary is rejected when the document is rendered.
    /// <see cref="HeadingLevel"/> takes precedence over the role when both are set.
    /// </summary>
    /// <exception cref="ArgumentException">The value is empty.</exception>
    public string? Role
    {
        get => role;
        set
        {
            if (value is not null && value.Length == 0)
            {
                throw new ArgumentException(
                    "Style.Role must be non-empty, or null for a style that declares no structure role.",
                    nameof(value));
            }

            role = value;
        }
    }
}
