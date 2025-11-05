using System.Linq;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// RadzenPivotAggreateContext.
/// </summary>
public class RadzenPivotAggreateContext<T>
{
    /// <summary>
    /// Gets the query.
    /// </summary>
    public IQueryable<T> View { get; internal set; }

    /// <summary>
    /// Gets the aggregate.
    /// </summary>
    public RadzenPivotAggregate<T> Aggregate { get; internal set; }

    /// <summary>
    /// Gets the aggregate value.
    /// </summary>
    public object Value { get; internal set; }

    /// <summary>
    /// Gets the row index.
    /// </summary>
    public int? Index { get; internal set; }
}

