using System;
using System.Text;
using Microsoft.AspNetCore.Components;

namespace Radzen;

/// <summary>
/// A base class of row/col components.
/// </summary>
public class RadzenFlexComponent : RadzenComponentWithChildren
{
    /// <summary>
    /// Gets or sets the content justify.
    /// </summary>
    /// <value>The content justify.</value>
    [Parameter]
    public JustifyContent JustifyContent { get; set; } = JustifyContent.Normal;

    /// <summary>
    /// Gets or sets the items alignment.
    /// </summary>
    /// <value>The items alignment.</value>
    [Parameter]
    public AlignItems AlignItems { get; set; } = AlignItems.Normal;

    internal string GetFlexCSSClass<T>(Enum v)
    {
        var value = ToDashCase(Enum.GetName(typeof(T), v));
        return value == "start" || value == "end" ? $"flex-{value}" : value;
    }

    internal string ToDashCase(string value)
    {
        var sb = new StringBuilder();

        foreach (var ch in value)
        {
            if ((char.IsUpper(ch) && sb.Length > 0) || char.IsSeparator(ch))
            {
                sb.Append('-');
            }

            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
            }
        }

        return sb.ToString();
    }
}

