using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Globalization;
using System.Linq.Expressions;

namespace Radzen
{
    /// <summary>
    /// Class ODataEnumerable.
    /// Implements the <see cref="IEnumerable{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IEnumerable{T}" />
    public class ODataEnumerable<T> : IEnumerable<T>
    {
        /// <summary>
        /// The source
        /// </summary>
        IEnumerable<T> source;
        /// <summary>
        /// Initializes a new instance of the <see cref="ODataEnumerable{T}"/> class.
        /// </summary>
        /// <param name="source">The source.</param>
        public ODataEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>IEnumerator&lt;T&gt;.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return source.GetEnumerator();
        }

        /// <summary>
        /// Gets the enumerator.
        /// </summary>
        /// <returns>IEnumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return source.GetEnumerator();
        }
    }

    /// <summary>
    /// Class ODataServiceResult.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ODataServiceResult<T>
    {
        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>The count.</value>
        [JsonPropertyName("@odata.count")]
        public int Count { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        public IEnumerable<T>? Value { get; set; }
    }

    /// <summary>
    /// Class ODataJsonSerializer.
    /// </summary>
    public static class ODataJsonSerializer
    {
        /// <summary>
        /// Determines whether the specified type is complex.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is complex; otherwise, <c>false</c>.</returns>
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2070, Justification = TrimMessages.ODataTypePreserved)]
        static bool IsComplex(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            var underlyingType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(type) : type;

            var baseType = underlyingType != null && underlyingType.IsGenericType ? underlyingType.GetGenericArguments().FirstOrDefault() ?? underlyingType : underlyingType;

            if (baseType == null)
            {
                return false;
            }

            return !baseType.IsPrimitive && !typeof(IEnumerable<>).IsAssignableFrom(baseType) && 
                type != typeof(string) && type != typeof(decimal) && type.IsClass;
        }

        /// <summary>
        /// Determines whether the specified type is enumerable.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is enumerable; otherwise, <c>false</c>.</returns>
        static bool IsEnumerable(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return !typeof(string).IsAssignableFrom(type) && (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type));
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.ODataTypePreserved)]
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2067, Justification = TrimMessages.ODataTypePreserved)]
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2070, Justification = TrimMessages.ODataTypePreserved)]
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2072, Justification = TrimMessages.ODataTypePreserved)]
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2087, Justification = TrimMessages.ODataTypePreserved)]
        [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2091, Justification = TrimMessages.ODataTypePreserved)]
        public static string Serialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(TValue value, JsonSerializerOptions? options = null)
        {
            if (options == null)
            {
                options = new JsonSerializerOptions();
            }
           
            var complexProperties = typeof(TValue).GetProperties().Where(p => IsComplex(p.PropertyType) || IsEnumerable(p.PropertyType));
            var dateProperties = typeof(TValue).GetProperties().Where(p => p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?));
            if (complexProperties.Any() || dateProperties.Any())
            {
                options.Converters.Add(new ComplexPropertiesConverter<TValue>(complexProperties.Select(p => p.Name)));
            }

            return JsonSerializer.Serialize<TValue>(value, options);
        }
    }

    /// <summary>
    /// Class ComplexPropertiesConverter.
    /// Implements the <see cref="JsonConverter{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="JsonConverter{T}" />
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.ODataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2067, Justification = TrimMessages.ODataTypePreserved)]
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2091, Justification = TrimMessages.ODataTypePreserved)]
    public class ComplexPropertiesConverter<T> : JsonConverter<T>
    {
        /// <summary>
        /// The complex properties
        /// </summary>
        IEnumerable<string> complexProperties;
        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexPropertiesConverter{T}"/> class.
        /// </summary>
        /// <param name="complexProperties">The complex properties.</param>
        public ComplexPropertiesConverter(IEnumerable<string> complexProperties)
        {
            this.complexProperties = complexProperties;
        }

        /// <summary>
        /// Reads the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The options.</param>
        /// <returns>T.</returns>
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options)!;
        }

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStartObject();

            var valueOptions = new JsonSerializerOptions();
            valueOptions.Converters.Add(new DateTimeConverterUsingDateTimeParse());

            using (JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(value, valueOptions)))
            {
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    if (!complexProperties.Contains(property.Name))
                    {
                        property.WriteTo(writer);
                    }
                }
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Class DateTimeConverterUsingDateTimeParse.
    /// Implements the <see cref="JsonConverter{DateTime}" />
    /// </summary>
    /// <seealso cref="JsonConverter{DateTime}" />
    public class DateTimeConverterUsingDateTimeParse : JsonConverter<DateTime>
    {
        /// <summary>
        /// Reads the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="typeToConvert">The type to convert.</param>
        /// <param name="options">The options.</param>
        /// <returns>DateTime.</returns>
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString() ?? string.Empty, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Class ODataExtensions.
    /// </summary>
    public static class ODataExtensions
    {
        /// <summary>
        /// Gets the o data URI.
        /// </summary>
        /// <param name="uri">The URI.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="top">The top.</param>
        /// <param name="skip">The skip.</param>
        /// <param name="orderby">The orderby.</param>
        /// <param name="expand">The expand.</param>
        /// <param name="select">The select.</param>
        /// <param name="apply">The apply.</param>
        /// <param name="count">if set to <c>true</c> [count].</param>
        /// <returns>Uri.</returns>
        public static Uri GetODataUri(this Uri uri, string? filter = null, int? top = null, int? skip = null, string? orderby = null, string? expand = null, string? select = null, string? apply = null, bool? count = null)
        {
            var uriBuilder = new UriBuilder(uri);
            var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (!string.IsNullOrEmpty(filter))
            {
                queryString["$filter"] = $"{filter.Replace("\"", "'", StringComparison.Ordinal)}";
            }

            if (top != null)
            {
                queryString["$top"] = $"{top}";
            }

            if (skip != null)
            {
                queryString["$skip"] = $"{skip}";
            }

            if (!string.IsNullOrEmpty(orderby))
            {
                queryString["$orderby"] = $"{orderby}";
            }

            if (!string.IsNullOrEmpty(expand))
            {
                queryString["$expand"] = $"{expand}";
            }

            if (!string.IsNullOrEmpty(select))
            {
                queryString["$select"] = $"{select}";
            }

            if (!string.IsNullOrEmpty(apply))
            {
                queryString["$apply"] = $"{apply}";
            }

            if (count != null)
            {
                queryString["$count"] = $"{count}".ToLower(CultureInfo.InvariantCulture);
            }

            uriBuilder.Query = queryString.ToString();

            return uriBuilder.Uri;
        }

        /// <summary>
        /// Returns <paramref name="uri" /> with the query options of <paramref name="query" /> and, when given, of a RadzenDataGrid's
        /// <paramref name="args" />. Each option value is escaped once with <see cref="Uri.EscapeDataString(string)" />; an option the query sets
        /// replaces the option of the same name in <paramref name="uri" />, whose other query parameters are kept.
        /// The grid's arguments apply last: <c>$filter</c> is <c>(args.Filter) and (query filter)</c> without an empty part, a non-empty
        /// <c>args.OrderBy</c> replaces the query's ordering, <c>args.Skip</c> and <c>args.Top</c> set <c>$skip</c> and <c>$top</c>, and <c>$count=true</c> is requested.
        /// <c>args.Filter</c> must be OData: the grid writes OData filters when its Data is an <see cref="ODataEnumerable{T}" />, e.g. the result of
        /// <see cref="QueryableExtension.AsODataEnumerable{T}(System.Collections.Generic.IEnumerable{T})" />.
        /// </summary>
        /// <typeparam name="T">The entity type of the entity set.</typeparam>
        /// <param name="uri">The URI of the entity set.</param>
        /// <param name="query">The query.</param>
        /// <param name="args">The arguments of a RadzenDataGrid LoadData event, or null.</param>
        /// <returns>The URI with the query options.</returns>
        public static Uri GetODataUri<T>(this Uri uri, ODataQuery<T> query, LoadDataArgs? args = null)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNull(query);
            return WithOptions(uri, query.State.Options(args));
        }

        /// <summary>
        /// Returns <paramref name="uri" /> with the query options of <paramref name="query" />, a query grouped with GroupBy and Select.
        /// When a RadzenDataGrid's <paramref name="args" /> are given, only <c>args.Filter</c> applies: it joins the filters before the grouping in
        /// <c>$apply=filter(...)</c>, while the grid's sorting and paging, which are for entities, never apply to the groups.
        /// Each option value is escaped once with <see cref="Uri.EscapeDataString(string)" />; the other query parameters of <paramref name="uri" /> are kept.
        /// </summary>
        /// <typeparam name="TSource">The entity type of the entity set.</typeparam>
        /// <typeparam name="TResult">The type of the results.</typeparam>
        /// <param name="uri">The URI of the entity set.</param>
        /// <param name="query">The query.</param>
        /// <param name="args">The arguments of a RadzenDataGrid LoadData event, or null.</param>
        /// <returns>The URI with the query options.</returns>
        public static Uri GetODataUri<TSource, TResult>(this Uri uri, ODataQuery<TSource, TResult> query, LoadDataArgs? args = null)
        {
            ArgumentNullException.ThrowIfNull(uri);
            ArgumentNullException.ThrowIfNull(query);
            return WithOptions(uri, query.State.Options(args));
        }

        private static Uri WithOptions(Uri uri, List<KeyValuePair<string, string>> options)
        {
            var builder = new UriBuilder(uri);
            var parameters = builder.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Where(parameter => !options.Any(option => string.Equals(option.Key, Uri.UnescapeDataString(parameter.Split('=')[0]), StringComparison.OrdinalIgnoreCase)))
                .Concat(options.Select(option => $"{option.Key}={Uri.EscapeDataString(option.Value)}"));

            builder.Query = string.Join("&", parameters);

            return builder.Uri;
        }

        /// <summary>
        /// Expands a navigation property of the collection that the last Include or ThenInclude expanded, e.g.
        /// <c>.Include(team =&gt; team.Members).ThenInclude(member =&gt; member.Manager)</c>, translated to <c>Members($expand=Manager)</c>.
        /// </summary>
        /// <typeparam name="T">The entity type of the entity set.</typeparam>
        /// <typeparam name="TPreviousProperty">The item type of the collection expanded last.</typeparam>
        /// <typeparam name="TProperty">The type of the navigation property.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="navigation">The navigation property of an item of the collection; a collection can be filtered, ordered and paged as in Include.</param>
        /// <returns>A new query to which ThenInclude adds the next level.</returns>
        public static IncludableODataQuery<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(this IIncludableODataQuery<T, IEnumerable<TPreviousProperty>> source, Expression<Func<TPreviousProperty, TProperty>> navigation)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(navigation);
            return new(source.Query.State.Include(navigation, true));
        }

        /// <summary>
        /// Expands a navigation property of the reference that the last Include or ThenInclude expanded, e.g.
        /// <c>.Include(ticket =&gt; ticket.Team).ThenInclude(team =&gt; team.Schedule)</c>, translated to <c>Team($expand=Schedule)</c>.
        /// </summary>
        /// <typeparam name="T">The entity type of the entity set.</typeparam>
        /// <typeparam name="TPreviousProperty">The type of the navigation property expanded last.</typeparam>
        /// <typeparam name="TProperty">The type of the navigation property.</typeparam>
        /// <param name="source">The query.</param>
        /// <param name="navigation">The navigation property; a collection can be filtered, ordered and paged as in Include.</param>
        /// <returns>A new query to which ThenInclude adds the next level.</returns>
        public static IncludableODataQuery<T, TProperty> ThenInclude<T, TPreviousProperty, TProperty>(this IIncludableODataQuery<T, TPreviousProperty> source, Expression<Func<TPreviousProperty, TProperty>> navigation)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(navigation);
            return new(source.Query.State.Include(navigation, true));
        }
    }
}
