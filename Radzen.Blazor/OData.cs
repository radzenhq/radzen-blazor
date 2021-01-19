using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace Radzen
{
    public class ODataEnumerable<T> : IEnumerable<T>
    {
        IEnumerable<T> source;
        public ODataEnumerable(IEnumerable<T> source)
        {
            this.source = source;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return source.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return source.GetEnumerator();
        }
    }

    public class ODataServiceResult<T>
    {
        [JsonPropertyName("@odata.count")]
        public int Count { get; set; }

        public IEnumerable<T> Value { get; set; }
    }

    public static class ODataJsonSerializer
    {
        static bool IsComplex(Type type)
        {
            var underlyingType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? Nullable.GetUnderlyingType(type) : type;

            var baseType = underlyingType.IsGenericType ? underlyingType.GetGenericArguments().FirstOrDefault() : underlyingType;

            return !baseType.IsPrimitive && !typeof(IEnumerable<>).IsAssignableFrom(baseType) && 
                type != typeof(string) && type != typeof(decimal) && type.IsClass;
        }

        static bool IsEnumerable(Type type)
        {
            return !typeof(string).IsAssignableFrom(type) && (typeof(IEnumerable<>).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type));
        }

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

    public class ComplexPropertiesConverter<T> : JsonConverter<T>
    {
        IEnumerable<string> complexProperties;
        public ComplexPropertiesConverter(IEnumerable<string> complexProperties)
        {
            this.complexProperties = complexProperties;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return JsonSerializer.Deserialize<T>(ref reader, options);
        }

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

    public class DateTimeConverterUsingDateTimeParse : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
        }
    }

    public static class ODataExtensions
    {        
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
