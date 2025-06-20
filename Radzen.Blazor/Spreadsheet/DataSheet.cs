using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a request to load data for a data sheet.
/// </summary>
public class DataSheetLoaderRequest
{
    /// <summary>
    /// Gets or sets the number of items to take from the data source.
    /// </summary>
    public int Take { get; init; }
    /// <summary>
    /// Gets or sets the number of items to skip in the data source.
    /// </summary>
    public int Skip { get; init; }
}

/// <summary>
/// Represents a mapping of a column in a data sheet to a property of an entity.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
public interface IColumnMapping<TEntity>
{
    /// <summary>
    /// Gets the name of the column.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the value of the column for a given entity.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    object? GetValue(TEntity entity);
}

/// <summary>
/// Represents a mapping of a column in a data sheet to a property of an entity with a specific value type.
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TValue"></typeparam>
public class ColumnMapping<TEntity, TValue>(string name, Expression<Func<TEntity, TValue>> accessor) : IColumnMapping<TEntity>
{
    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the compiled accessor function for the column.
    /// </summary>
    public Func<TEntity, TValue> Accessor { get; } = accessor.Compile();

    /// <inheritdoc/>
    public object? GetValue(TEntity entity) => Accessor(entity);
}

/// <summary>
/// Provides methods to create column mappings for data sheets.
/// </summary>
public static class ColumnMapping
{
    /// <summary>
    /// Creates a column mapping for a data sheet with the specified name and accessor expression.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="name"></param>
    /// <param name="accessor"></param>
    /// <returns></returns>
    public static ColumnMapping<TEntity, TValue> Create<TEntity, TValue>(string name, Expression<Func<TEntity, TValue>> accessor)
        where TEntity : class, new() => new(name, accessor);
}

/// <summary>
/// Represents a data sheet that can load data from a data source.
/// </summary>
/// <typeparam name="T"></typeparam>
public class DataSheet<T> : Sheet where T : class, new()
{
    /// <summary>
    /// Gets the collection of column mappings for the data sheet.
    /// </summary>
    public IReadOnlyList<IColumnMapping<T>> ColumnMappings { get; private set; }

    /// <summary>
    /// Gets the function that loads data for the data sheet.
    /// This function takes a <see cref="DataSheetLoaderRequest"/> and returns a task that resolves to a list of data items of type T.
    /// The function is responsible for implementing the logic to retrieve data from the underlying data source,
    /// such as a database or an API, based on the provided request parameters (like pagination).
    /// The data loader function should handle the logic for fetching the appropriate subset of data
    /// based on the skip and take parameters in the <see cref="DataSheetLoaderRequest"/>.
    /// </summary>
    public Func<DataSheetLoaderRequest, Task<List<T>>> DataLoader { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSheet{T}"/> class with the specified data and column mappings.
    /// This constructor is useful when you have an IQueryable data source and want to use it directly
    /// without needing to implement a custom data loader function.
    /// It will use a default data loader that queries the data source using Skip and Take for pagination.
    /// The data source should be an IQueryable of type T, allowing for efficient querying and pagination
    /// without loading all data into memory at once.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="columnMappings"></param>
    public DataSheet(IQueryable<T> data, IReadOnlyList<IColumnMapping<T>> columnMappings) : this(columnMappings, request => QueryableDataLoader(request, data))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DataSheet{T}"/> class with the specified column mappings and data loader function.
    /// </summary>
    /// <param name="columnMappings"></param>
    /// <param name="dataLoader"></param>
    public DataSheet(IReadOnlyList<IColumnMapping<T>> columnMappings, Func<DataSheetLoaderRequest, Task<List<T>>> dataLoader) : base(100, 100)
    {
        ColumnMappings = columnMappings;

        DataLoader = dataLoader;

        Cells = new DataCellStore<T>(this);
    }

    private static Task<List<T>> QueryableDataLoader(DataSheetLoaderRequest request, IQueryable<T> data)
    {
        return Task.FromResult(data.Skip(request.Skip).Take(request.Take).ToList());
    }
} 