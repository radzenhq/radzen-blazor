namespace Radzen
{
    /// <summary>
    /// Constants for trimming annotation messages used across the library.
    /// </summary>
    internal static class TrimMessages
    {
        // Category for UnconditionalSuppressMessage
        internal const string Trimming = "Trimming";

        // Common warning IDs
        internal const string IL2026 = "IL2026";
        internal const string IL2046 = "IL2046";
        internal const string IL2055 = "IL2055";
        internal const string IL2057 = "IL2057";
        internal const string IL2060 = "IL2060";
        internal const string IL2067 = "IL2067";
        internal const string IL2070 = "IL2070";
        internal const string IL2072 = "IL2072";
        internal const string IL2075 = "IL2075";
        internal const string IL2077 = "IL2077";
        internal const string IL2080 = "IL2080";
        internal const string IL2087 = "IL2087";
        internal const string IL2090 = "IL2090";
        internal const string IL2091 = "IL2091";
        internal const string IL2095 = "IL2095";
        internal const string IL2111 = "IL2111";

        // RequiresUnreferencedCode messages
        internal const string PropertyAccessReflection = "Uses reflection for dynamic property access.";
        internal const string ExpressionTreeReflection = "Uses reflection for dynamic property access via Expression trees.";
        internal const string ExpressionParserReflection = "Uses reflection for dynamic expression parsing.";
        internal const string DynamicTypeGeneration = "Uses Reflection.Emit for dynamic type generation.";
        internal const string AssemblyScanning = "Scans all loaded assemblies to resolve types by name.";
        internal const string GenericMethodReflection = "This method uses reflection to invoke a generic method.";
        internal const string CustomAttributeReflection = "Uses reflection to inspect custom attributes on generic type definitions.";
        internal const string DynamicLinqReflection = "Uses reflection for dynamic LINQ operations.";

        // UnconditionalSuppressMessage justifications
        internal const string DataTypePreserved = "Data item types are preserved by the application.";
        internal const string NumericTypePreserved = "TValue is a numeric primitive type that is always preserved.";
        internal const string ComponentTypePreserved = "Component types are preserved by the Blazor framework.";
        internal const string ModelTypePreserved = "Model types are preserved by the Blazor framework's form binding.";
        internal const string EnumTypePreserved = "Enum types used in Radzen components are preserved.";
        internal const string ODataTypePreserved = "OData model types are preserved by the application.";
        internal const string ThemeTypePreserved = "Theme is a simple string type that is trim-safe.";
        internal const string CollectionTypePreserved = "Collection types are preserved by the application.";
        internal const string AssemblyMetadataPreserved = "Assembly version metadata is always available.";
    }
}
