﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Radzen;
using Radzen.Blazor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;
using System.Text.RegularExpressions;

namespace System.Linq.Dynamic.Core
{
    /// <summary>
    /// Class DynamicExtensions used to replace System.Linq.Dynamic.Core library.
    /// </summary>
    public static class DynamicExtensions
    {
        static string rtPath = Path.GetDirectoryName(typeof(object).Assembly.Location);

        static CSharpCompilation Compilation = CSharpCompilation.Create(Guid.NewGuid().ToString())
              .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
              .WithOptimizationLevel(OptimizationLevel.Debug))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(rtPath, "System.Runtime.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(rtPath, "System.Collections.dll")))
              .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
              .AddReferences(MetadataReference.CreateFromFile(typeof(Expression).Assembly.Location))
              .AddReferences(MetadataReference.CreateFromFile(typeof(Queryable).Assembly.Location))
              .AddReferences(MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location));
        private static Assembly Compile(CSharpCompilation compilation, string code, AssemblyLoadContext context)
        {
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

            if (errors.Any())
            {
                var message = string.Join(Environment.NewLine, errors.Select(e => e.GetMessage()));

                throw new InvalidOperationException($"Compilation of {code} failed: {message}");
            }

            using var projectStream = new MemoryStream();

            var result = compilation.Emit(projectStream);

            if (!result.Success)
            {
                errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

                var message = string.Join(Environment.NewLine, errors.Select(e => e.GetMessage()));

                throw new InvalidOperationException(message);
            }

            projectStream.Seek(0, SeekOrigin.Begin);

            return context.LoadFromStream(projectStream);
        }

        /// <summary>
        /// Filters using the specified filter descriptors.
        /// </summary>
        public static IQueryable<T> Where<T>(
            this IQueryable<T> source,
            string selector,
            object[] parameters = null, object[] otherParameters = null)
        {
            try
            {
                if (parameters != null)
                {
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var value = object.Equals(parameters[i], string.Empty) ? @"""""" :
                            parameters[i] == null ? @"null" :
                                parameters[i] is string ? @$"""{parameters[i].ToString().Replace("\"", "\\\"")}"""  : 
                                    parameters[i] is bool ? $"{parameters[i]}".ToLower() : parameters[i];

                        selector = selector.Replace($"@{i}", $"{value}");
                    }
                }

                var code = $@"
using System;
using System.Linq;
using System.Linq.Expressions;
namespace Dynamic;
public static class Linq 
{{ 
  public static Expression<Func<{typeof(T).FullName.Replace("+", ".")}, bool>> where = {(selector == "true" ? "i => true" : selector).Replace("DateTime", "DateTime.Parse").Replace("DateTimeOffset", "DateTimeOffset.Parse").Replace("DateOnly", "DateOnly.Parse").Replace("Guid", "Guid.Parse").Replace(" = "," == ")};
}}";

                var assembly = Compile(Compilation
                  .AddReferences(MetadataReference.CreateFromFile(typeof(T).Assembly.Location))
                  .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest))),
                  code, new AssemblyLoadContext("RadzenALC", true));

                Expression<Func<T, bool>> whereMethod = (Expression<Func<T, bool>>)assembly
                        .GetType("Dynamic.Linq").GetFields().FirstOrDefault().GetValue(null);

                return source.Where(whereMethod);
            }
            catch
            {
                throw new InvalidOperationException($"Invalid Where selector");
            }
        }

        /// <summary>
        /// Sorts the elements of a sequence in ascending or descending order according to a key.
        /// </summary>
        public static IOrderedQueryable<T> OrderBy<T>(
            this IQueryable<T> source,
            string selector,
            object[] parameters = null)
        {
            return Radzen.QueryableExtension.OrderBy(source, selector);
        }

        /// <summary>
        /// Projects each element of a sequence into a collection of property values.
        /// </summary>
        public static IQueryable Select<T>(
            this IQueryable<T> source,
            string selector,
            object[] parameters = null)
        {
            try
            {
                var parameter = Expression.Parameter(typeof(T), "it");

                var className = $"Class{Guid.NewGuid()}".Replace("-", "");

                var properties = selector.Replace("new (", "").Replace(")", "").Trim().Split(",", StringSplitOptions.RemoveEmptyEntries);

                var declaredProperties = string.Join(Environment.NewLine, properties
                    .Select(s =>
                    {
                        var original = s.Split(" as ").FirstOrDefault().Trim();
                        var property = QueryableExtension.GetNestedPropertyExpression(parameter, original);
                        var name = s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim() : s.Trim();
                        return $@"public {property.Type.DisplayName(fullName: false) + (original.Contains(".") ? "?" : "")} {name} {{ get; set; }}";
                    }));

                selector = string.Join(", ", properties
                    .Select(s => (s.Contains(" as ") ? s.Split(" as ").LastOrDefault().Trim() : s.Trim()) + " = " + $"it.{s.Split(" as ").FirstOrDefault().Replace(".", "?.").Trim()}"));

                var code = $@"
using System;
using System.Linq;
using System.Linq.Expressions;
namespace Dynamic;
public class {className}
{{
  {declaredProperties}
}}
public static class Linq 
{{ 
    public static IQueryable Select(IQueryable source)
    {{
        var list = System.Linq.Enumerable.ToList(System.Linq.Queryable.Cast<{typeof(T).FullName.Replace("+", ".")}>(source));
        return System.Linq.Queryable.AsQueryable(list.Select(it => new {className}() {{ {selector} }}));
    }}
}}";

                var assembly = Compile(Compilation
                  .AddReferences(MetadataReference.CreateFromFile(typeof(T).Assembly.Location))
                  .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Latest))),
                  code, new AssemblyLoadContext("RadzenALC", true));

                return (IQueryable)assembly.GetType("Dynamic.Linq").GetMethods().FirstOrDefault().Invoke(null, new object[] { source });
            }
            catch
            {
                throw new InvalidOperationException($"Invalid selector");
            }
        }
    }

    static class SharedTypeExtensions
    {
        private static readonly Dictionary<Type, string> BuiltInTypeNames = new()
        {
            { typeof(bool), "bool" },
            { typeof(byte), "byte" },
            { typeof(char), "char" },
            { typeof(decimal), "decimal" },
            { typeof(double), "double" },
            { typeof(float), "float" },
            { typeof(int), "int" },
            { typeof(long), "long" },
            { typeof(object), "object" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(string), "string" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(ushort), "ushort" },
            { typeof(void), "void" }
        };

        public static Type UnwrapNullableType(this Type type)
            => Nullable.GetUnderlyingType(type) ?? type;

        public static bool IsNullableValueType(this Type type)
            => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

        public static bool IsNullableType(this Type type)
            => !type.IsValueType || type.IsNullableValueType();

        public static bool IsValidEntityType(this Type type)
            => type.IsClass
                && !type.IsArray;

        public static bool IsPropertyBagType(this Type type)
        {
            if (type.IsGenericTypeDefinition)
            {
                return false;
            }

            var types = GetGenericTypeImplementations(type, typeof(IDictionary<,>));
            return types.Any(
                t => t.GetGenericArguments()[0] == typeof(string)
                    && t.GetGenericArguments()[1] == typeof(object));
        }

        public static Type MakeNullable(this Type type, bool nullable = true)
            => type.IsNullableType() == nullable
                ? type
                : nullable
                    ? typeof(Nullable<>).MakeGenericType(type)
                    : type.UnwrapNullableType();

        public static bool IsNumeric(this Type type)
        {
            type = type.UnwrapNullableType();

            return type.IsInteger()
                || type == typeof(decimal)
                || type == typeof(float)
                || type == typeof(double);
        }

        public static bool IsInteger(this Type type)
        {
            type = type.UnwrapNullableType();

            return type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(byte)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(ushort)
                || type == typeof(sbyte)
                || type == typeof(char);
        }

        public static bool IsSignedInteger(this Type type)
            => type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(sbyte);

        public static bool IsAnonymousType(this Type type)
            => type.Name.StartsWith("<>", StringComparison.Ordinal)
                && type.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Length > 0
                && type.Name.Contains("AnonymousType");

        public static PropertyInfo GetAnyProperty(this Type type, string name)
        {
            var props = type.GetRuntimeProperties().Where(p => p.Name == name).ToList();
            if (props.Count > 1)
            {
                throw new AmbiguousMatchException();
            }

            return props.SingleOrDefault();
        }

        public static bool IsInstantiable(this Type type)
            => !type.IsAbstract
                && !type.IsInterface
                && (!type.IsGenericType || !type.IsGenericTypeDefinition);

        public static Type UnwrapEnumType(this Type type)
        {
            var isNullable = type.IsNullableType();
            var underlyingNonNullableType = isNullable ? type.UnwrapNullableType() : type;
            if (!underlyingNonNullableType.IsEnum)
            {
                return type;
            }

            var underlyingEnumType = Enum.GetUnderlyingType(underlyingNonNullableType);
            return isNullable ? MakeNullable(underlyingEnumType) : underlyingEnumType;
        }

        public static Type GetSequenceType(this Type type)
        {
            var sequenceType = TryGetSequenceType(type);
            if (sequenceType == null)
            {
                throw new ArgumentException($"The type {type.Name} does not represent a sequence");
            }

            return sequenceType;
        }

        public static Type TryGetSequenceType(this Type type)
            => type.TryGetElementType(typeof(IEnumerable<>))
                ?? type.TryGetElementType(typeof(IAsyncEnumerable<>));

        public static Type TryGetElementType(this Type type, Type interfaceOrBaseType)
        {
            if (type.IsGenericTypeDefinition)
            {
                return null;
            }

            var types = GetGenericTypeImplementations(type, interfaceOrBaseType);

            Type singleImplementation = null;
            foreach (var implementation in types)
            {
                if (singleImplementation == null)
                {
                    singleImplementation = implementation;
                }
                else
                {
                    singleImplementation = null;
                    break;
                }
            }

            return singleImplementation?.GenericTypeArguments.FirstOrDefault();
        }

        public static bool IsCompatibleWith(this Type propertyType, Type fieldType)
        {
            if (propertyType.IsAssignableFrom(fieldType)
                || fieldType.IsAssignableFrom(propertyType))
            {
                return true;
            }

            var propertyElementType = propertyType.TryGetSequenceType();
            var fieldElementType = fieldType.TryGetSequenceType();

            return propertyElementType != null
                && fieldElementType != null
                && IsCompatibleWith(propertyElementType, fieldElementType);
        }

        public static IEnumerable<Type> GetGenericTypeImplementations(this Type type, Type interfaceOrBaseType)
        {
            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericTypeDefinition)
            {
                var baseTypes = interfaceOrBaseType.GetTypeInfo().IsInterface
                    ? typeInfo.ImplementedInterfaces
                    : type.GetBaseTypes();
                foreach (var baseType in baseTypes)
                {
                    if (baseType.IsGenericType
                        && baseType.GetGenericTypeDefinition() == interfaceOrBaseType)
                    {
                        yield return baseType;
                    }
                }

                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == interfaceOrBaseType)
                {
                    yield return type;
                }
            }
        }

        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            var currentType = type.BaseType;

            while (currentType != null)
            {
                yield return currentType;

                currentType = currentType.BaseType;
            }
        }

        public static List<Type> GetBaseTypesAndInterfacesInclusive(this Type type)
        {
            var baseTypes = new List<Type>();
            var typesToProcess = new Queue<Type>();
            typesToProcess.Enqueue(type);

            while (typesToProcess.Count > 0)
            {
                type = typesToProcess.Dequeue();
                baseTypes.Add(type);

                if (type.IsNullableValueType())
                {
                    typesToProcess.Enqueue(Nullable.GetUnderlyingType(type)!);
                }

                if (type.IsConstructedGenericType)
                {
                    typesToProcess.Enqueue(type.GetGenericTypeDefinition());
                }

                if (!type.IsGenericTypeDefinition
                    && !type.IsInterface)
                {
                    if (type.BaseType != null)
                    {
                        typesToProcess.Enqueue(type.BaseType);
                    }

                    foreach (var @interface in GetDeclaredInterfaces(type))
                    {
                        typesToProcess.Enqueue(@interface);
                    }
                }
            }

            return baseTypes;
        }

        public static IEnumerable<Type> GetTypesInHierarchy(this Type type)
        {
            var currentType = type;

            while (currentType != null)
            {
                yield return currentType;

                currentType = currentType.BaseType;
            }
        }

        public static IEnumerable<Type> GetDeclaredInterfaces(this Type type)
        {
            var interfaces = type.GetInterfaces();
            if (type.BaseType == typeof(object)
                || type.BaseType == null)
            {
                return interfaces;
            }

            return interfaces.Except(type.BaseType.GetInterfaces());
        }

        public static ConstructorInfo GetDeclaredConstructor(this Type type, Type[] types)
        {
            types ??= Array.Empty<Type>();

            return type.GetTypeInfo().DeclaredConstructors
                .SingleOrDefault(
                    c => !c.IsStatic
                        && c.GetParameters().Select(p => p.ParameterType).SequenceEqual(types))!;
        }

        public static IEnumerable<PropertyInfo> GetPropertiesInHierarchy(this Type type, string name)
        {
            var currentType = type;
            do
            {
                var typeInfo = currentType.GetTypeInfo();
                foreach (var propertyInfo in typeInfo.DeclaredProperties)
                {
                    if (propertyInfo.Name.Equals(name, StringComparison.Ordinal)
                        && !(propertyInfo.GetMethod ?? propertyInfo.SetMethod)!.IsStatic)
                    {
                        yield return propertyInfo;
                    }
                }

                currentType = typeInfo.BaseType;
            }
            while (currentType != null);
        }

        // Looking up the members through the whole hierarchy allows to find inherited private members.
        public static IEnumerable<MemberInfo> GetMembersInHierarchy(this Type type)
        {
            var currentType = type;

            do
            {
                // Do the whole hierarchy for properties first since looking for fields is slower.
                foreach (var propertyInfo in currentType.GetRuntimeProperties().Where(pi => !(pi.GetMethod ?? pi.SetMethod)!.IsStatic))
                {
                    yield return propertyInfo;
                }

                foreach (var fieldInfo in currentType.GetRuntimeFields().Where(f => !f.IsStatic))
                {
                    yield return fieldInfo;
                }

                currentType = currentType.BaseType;
            }
            while (currentType != null);
        }

        public static IEnumerable<MemberInfo> GetMembersInHierarchy(this Type type, string name)
            => type.GetMembersInHierarchy().Where(m => m.Name == name);

        private static readonly Dictionary<Type, object> CommonTypeDictionary = new()
    {
#pragma warning disable IDE0034 // Simplify 'default' expression - default causes default(object)
        { typeof(int), default(int) },
        { typeof(Guid), default(Guid) },
        { typeof(DateOnly), default(DateOnly) },
        { typeof(DateTime), default(DateTime) },
        { typeof(DateTimeOffset), default(DateTimeOffset) },
        { typeof(TimeOnly), default(TimeOnly) },
        { typeof(long), default(long) },
        { typeof(bool), default(bool) },
        { typeof(double), default(double) },
        { typeof(short), default(short) },
        { typeof(float), default(float) },
        { typeof(byte), default(byte) },
        { typeof(char), default(char) },
        { typeof(uint), default(uint) },
        { typeof(ushort), default(ushort) },
        { typeof(ulong), default(ulong) },
        { typeof(sbyte), default(sbyte) }
#pragma warning restore IDE0034 // Simplify 'default' expression
    };

        public static object GetDefaultValue(this Type type)
        {
            if (!type.IsValueType)
            {
                return null;
            }

            // A bit of perf code to avoid calling Activator.CreateInstance for common types and
            // to avoid boxing on every call. This is about 50% faster than just calling CreateInstance
            // for all value types.
            return CommonTypeDictionary.TryGetValue(type, out var value)
                ? value
                : Activator.CreateInstance(type);
        }

        public static IEnumerable<Reflection.TypeInfo> GetConstructibleTypes(this Assembly assembly)
            => assembly.GetLoadableDefinedTypes().Where(
                t => !t.IsAbstract
                    && !t.IsGenericTypeDefinition);

        public static IEnumerable<Reflection.TypeInfo> GetLoadableDefinedTypes(this Assembly assembly)
        {
            try
            {
                return assembly.DefinedTypes;
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).Select(IntrospectionExtensions.GetTypeInfo!);
            }
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public static string DisplayName(this Type type, bool fullName = true, bool compilable = false)
        {
            var stringBuilder = new StringBuilder();
            ProcessType(stringBuilder, type, fullName, compilable);
            return stringBuilder.ToString();
        }

        private static void ProcessType(StringBuilder builder, Type type, bool fullName, bool compilable)
        {
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                ProcessGenericType(builder, type, genericArguments, genericArguments.Length, fullName, compilable);
            }
            else if (type.IsArray)
            {
                ProcessArrayType(builder, type, fullName, compilable);
            }
            else if (BuiltInTypeNames.TryGetValue(type, out var builtInName))
            {
                builder.Append(builtInName);
            }
            else if (!type.IsGenericParameter)
            {
                if (compilable)
                {
                    if (type.IsNested)
                    {
                        ProcessType(builder, type.DeclaringType!, fullName, compilable);
                        builder.Append('.');
                    }
                    else if (fullName)
                    {
                        builder.Append(type.Namespace).Append('.');
                    }

                    builder.Append(type.Name);
                }
                else
                {
                    builder.Append(fullName ? type.FullName : type.Name);
                }
            }
        }

        private static void ProcessArrayType(StringBuilder builder, Type type, bool fullName, bool compilable)
        {
            var innerType = type;
            while (innerType.IsArray)
            {
                innerType = innerType.GetElementType()!;
            }

            ProcessType(builder, innerType, fullName, compilable);

            while (type.IsArray)
            {
                builder.Append('[');
                builder.Append(',', type.GetArrayRank() - 1);
                builder.Append(']');
                type = type.GetElementType()!;
            }
        }

        private static void ProcessGenericType(
            StringBuilder builder,
            Type type,
            Type[] genericArguments,
            int length,
            bool fullName,
            bool compilable)
        {
            if (type.IsConstructedGenericType
                && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                ProcessType(builder, type.UnwrapNullableType(), fullName, compilable);
                builder.Append('?');
                return;
            }

            var offset = type.IsNested ? type.DeclaringType!.GetGenericArguments().Length : 0;

            if (compilable)
            {
                if (type.IsNested)
                {
                    ProcessType(builder, type.DeclaringType!, fullName, compilable);
                    builder.Append('.');
                }
                else if (fullName)
                {
                    builder.Append(type.Namespace);
                    builder.Append('.');
                }
            }
            else
            {
                if (fullName)
                {
                    if (type.IsNested)
                    {
                        ProcessGenericType(builder, type.DeclaringType!, genericArguments, offset, fullName, compilable);
                        builder.Append('+');
                    }
                    else
                    {
                        builder.Append(type.Namespace);
                        builder.Append('.');
                    }
                }
            }

            var genericPartIndex = type.Name.IndexOf('`');
            if (genericPartIndex <= 0)
            {
                builder.Append(type.Name);
                return;
            }

            builder.Append(type.Name, 0, genericPartIndex);
            builder.Append('<');

            for (var i = offset; i < length; i++)
            {
                ProcessType(builder, genericArguments[i], fullName, compilable);
                if (i + 1 == length)
                {
                    continue;
                }

                builder.Append(',');
                if (!genericArguments[i + 1].IsGenericParameter)
                {
                    builder.Append(' ');
                }
            }

            builder.Append('>');
        }

        public static IEnumerable<string> GetNamespaces(this Type type)
        {
            if (BuiltInTypeNames.ContainsKey(type))
            {
                yield break;
            }

            yield return type.Namespace!;

            if (type.IsGenericType)
            {
                foreach (var typeArgument in type.GenericTypeArguments)
                {
                    foreach (var ns in typeArgument.GetNamespaces())
                    {
                        yield return ns;
                    }
                }
            }
        }

        public static ConstantExpression GetDefaultValueConstant(this Type type)
            => (ConstantExpression)GenerateDefaultValueConstantMethod
                .MakeGenericMethod(type).Invoke(null, Array.Empty<object>())!;

        private static readonly MethodInfo GenerateDefaultValueConstantMethod =
            typeof(SharedTypeExtensions).GetTypeInfo().GetDeclaredMethod(nameof(GenerateDefaultValueConstant))!;

        private static ConstantExpression GenerateDefaultValueConstant<TDefault>()
            => Expression.Constant(default(TDefault), typeof(TDefault));
    }
}
