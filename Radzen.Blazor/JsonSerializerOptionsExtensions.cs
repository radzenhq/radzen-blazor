using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Radzen
{
    static class JsonSerializerOptionsExtensions
    {
        public static JsonTypeInfo<T> GetJsonTypeInfo<T>(this JsonSerializerOptions options)
        {
#if NET11_0_OR_GREATER
            return options.GetTypeInfo<T>();
#else
            return (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
#endif
        }
    }
}
