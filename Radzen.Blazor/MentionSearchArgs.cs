using System;
using System.Collections.Generic;

namespace Radzen.Blazor;

/// <summary>
/// Supplies information about a mention search event in the RadzenChat component.
/// </summary>
public class MentionSearchArgs
{
    /// <summary>
    /// Gets or sets the search filter text (text typed after the mention character).
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets how many items to skip. Related to pagination. Usually used with the <see cref="System.Linq.Enumerable.Skip{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
    /// </summary>
    public int? Skip { get; set; }

    /// <summary>
    /// Gets or sets how many items to take. Related to pagination and page size. Usually used with the <see cref="System.Linq.Enumerable.Take{TSource}(IEnumerable{TSource}, int)"/> LINQ method.
    /// </summary>
    public int? Top { get; set; }
}
