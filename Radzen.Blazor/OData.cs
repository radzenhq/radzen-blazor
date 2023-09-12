using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using System.Globalization;

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
        public IEnumerable<T> Value { get; set; }
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
        static bool IsComplex(Type type)
        {
            var underlyingType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(type) : type;

            var baseType = underlyingType.IsGenericType ? underlyingType.GetGenericArguments().FirstOrDefault() : underlyingType;

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
            return !typeof(string).IsAssignableFrom(type) && (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type));
        }

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="TValue">The type of the t value.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        /// <returns>System.String.</returns>
        public static string Serialize<TValue>(TValue value, JsonSerializerOptions options = null)
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
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
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
            return DateTime.Parse(reader.GetString());
        }

        /// <summary>
        /// Writes the specified writer.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value.</param>
        /// <param name="options">The options.</param>
        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
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
        /// <param name="count">if set to <c>true</c> [count].</param>
        /// <returns>Uri.</returns>
        public static Uri GetODataUri(this Uri uri, string filter = null, int? top = null, int? skip = null, string orderby = null, string expand = null, string select = null, bool? count = null)
        {
            var uriBuilder = new UriBuilder(uri);
            var queryString = HttpUtility.ParseQueryString(uriBuilder.Query);

            if (!string.IsNullOrEmpty(filter))
            {
                queryString["$filter"] = $"{filter.Replace("\"", "'")}";
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

            if (count != null)
            {
                queryString["$count"] = $"{count}".ToLower();
            }

            uriBuilder.Query = queryString.ToString();

            return uriBuilder.Uri;
        }
    }
}
