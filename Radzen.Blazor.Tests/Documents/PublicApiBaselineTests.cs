#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class PublicApiBaselineTests
{
    private const string NamespaceRoot = "Radzen.Documents";

    private const string BaselineResource = "Radzen.Blazor.Tests.Documents.PublicApiBaseline.txt";

    private const string WriteVariable = "RADZEN_WRITE_API_BASELINE";

    [Fact]
    public void PublicSurfaceOfTheDocumentNamespaces_MatchesTheApprovedBaseline()
    {
        var actual = Surface();

        if (Environment.GetEnvironmentVariable(WriteVariable) is { Length: > 0 } path)
        {
            File.WriteAllLines(path, actual);
        }

        Assert.Equal(
            string.Join(Environment.NewLine, Baseline()),
            string.Join(Environment.NewLine, actual));
    }

    [Fact]
    public void EveryDocumentNamespaceWithPublicTypes_IsCoveredByTheBaseline()
    {
        var covered = typeof(Document).Assembly.GetTypes()
            .Where(IsVisibleOutside)
            .Select(type => type.Namespace)
            .Where(space => space is { } declared
                && (declared == NamespaceRoot
                    || declared.StartsWith(NamespaceRoot + ".", StringComparison.Ordinal)))
            .Distinct()
            .ToList();

        Assert.All(covered, space => Assert.Contains(
            Surface(),
            line => line.Contains(space + ".", StringComparison.Ordinal)));
    }

    internal static IReadOnlyList<string> Surface()
    {
        var context = new NullabilityInfoContext();
        var lines = new List<string>();

        foreach (var type in typeof(Document).Assembly.GetTypes()
            .Where(IsIncluded)
            .OrderBy(DisplayName, StringComparer.Ordinal))
        {
            lines.Add(Declaration(type));

            var members = Members(type, context).ToList();
            members.Sort(StringComparer.Ordinal);
            lines.AddRange(members);
        }

        return lines;
    }

    private static bool IsIncluded(Type type)
        => type.Namespace is { } declared
            && (declared == NamespaceRoot
                || declared.StartsWith(NamespaceRoot + ".", StringComparison.Ordinal))
            && IsVisibleOutside(type);

    private static string Declaration(Type type)
    {
        var builder = new StringBuilder();
        builder.Append(DisplayName(type)).Append(" = ").Append(Accessibility(type)).Append(' ').Append(Kind(type));

        var bases = new List<string>();

        if (type.IsEnum)
        {
            bases.Add(Name(type.GetEnumUnderlyingType(), null));
        }
        else if (type.BaseType is { } parent
            && parent != typeof(object)
            && parent != typeof(ValueType)
            && !(type.IsClass && parent == typeof(MulticastDelegate)))
        {
            bases.Add(Name(parent, null));
        }

        bases.AddRange(DirectInterfaces(type).Select(face => Name(face, null)).OrderBy(name => name, StringComparer.Ordinal));

        if (bases.Count > 0)
        {
            builder.Append(" : ").Append(string.Join(", ", bases));
        }

        builder.Append(Constraints(type.IsGenericTypeDefinition ? type.GetGenericArguments() : []));

        return builder.ToString();
    }

    private static IEnumerable<Type> DirectInterfaces(Type type)
    {
        var all = type.GetInterfaces();
        var inherited = new HashSet<Type>(type.BaseType?.GetInterfaces() ?? []);

        foreach (var face in all)
        {
            foreach (var nested in face.GetInterfaces())
            {
                inherited.Add(nested);
            }
        }

        return all.Where(face => !inherited.Contains(face) && IsVisibleOutside(face));
    }

    private static IEnumerable<string> Members(Type type, NullabilityInfoContext context)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var owner = DisplayName(type);

        foreach (var constructor in type.GetConstructors(Declared).Where(IsVisibleOutside))
        {
            yield return $"{Modifiers(constructor)} {owner}..ctor({Parameters(constructor, context)})";
        }

        foreach (var property in type.GetProperties(Declared).Where(IsVisibleOutside))
        {
            var info = context.Create(property);
            var index = property.GetIndexParameters();
            var name = index.Length == 0
                ? property.Name
                : $"{property.Name}[{Parameters(index, context)}]";
            yield return
                $"{PropertyModifiers(property)} {owner}.{name} : {Name(property.PropertyType, info)} {{{Accessors(property)} }}";
        }

        foreach (var method in type.GetMethods(Declared).Where(method => !IsAccessor(method) && IsVisibleOutside(method)))
        {
            var arguments = method.IsGenericMethodDefinition ? method.GetGenericArguments() : [];
            var generics = arguments.Length == 0
                ? string.Empty
                : $"<{string.Join(", ", arguments.Select(argument => argument.Name))}>";
            yield return
                $"{Modifiers(method)} {owner}.{method.Name}{generics}({Parameters(method, context)})"
                + $" : {Name(method.ReturnType, context.Create(method.ReturnParameter))}{Constraints(arguments)}";
        }

        foreach (var field in type.GetFields(Declared).Where(field => !field.IsSpecialName && IsVisibleOutside(field)))
        {
            yield return $"{FieldModifiers(field)} {owner}.{field.Name} : {Name(field.FieldType, context.Create(field))}";
        }

        foreach (var @event in type.GetEvents(Declared).Where(IsVisibleOutside))
        {
            yield return $"{EventModifiers(@event)} {owner}.{@event.Name} : event {Name(@event.EventHandlerType!, null)}";
        }
    }

    private static string Constraints(Type[] arguments)
    {
        var builder = new StringBuilder();

        foreach (var argument in arguments)
        {
            var parts = new List<string>();
            var attributes = argument.GenericParameterAttributes;

            if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint))
            {
                parts.Add("class");
            }

            if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                parts.Add("struct");
            }

            parts.AddRange(argument.GetGenericParameterConstraints()
                .Where(constraint => constraint != typeof(ValueType))
                .Select(constraint => Name(constraint, null))
                .OrderBy(name => name, StringComparer.Ordinal));

            if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint)
                && !attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                parts.Add("new()");
            }

            if (parts.Count > 0)
            {
                builder.Append(" where ").Append(argument.Name).Append(" : ").Append(string.Join(", ", parts));
            }
        }

        return builder.ToString();
    }

    private static string Accessors(PropertyInfo property)
    {
        var builder = new StringBuilder();
        var getter = property.GetGetMethod(true);
        var setter = property.GetSetMethod(true);

        if (getter is not null && IsVisibleOutside(getter))
        {
            builder.Append(' ').Append(Accessibility(getter)).Append(" get;");
        }

        if (setter is not null && IsVisibleOutside(setter))
        {
            var kind = setter.ReturnParameter.GetRequiredCustomModifiers()
                .Any(modifier => modifier == typeof(IsExternalInit)) ? "init;" : "set;";
            builder.Append(' ').Append(Accessibility(setter)).Append(' ').Append(kind);
        }

        return builder.ToString();
    }

    private static bool IsAccessor(MethodInfo method)
        => method.IsSpecialName
            && (method.Name.StartsWith("get_", StringComparison.Ordinal)
                || method.Name.StartsWith("set_", StringComparison.Ordinal)
                || method.Name.StartsWith("add_", StringComparison.Ordinal)
                || method.Name.StartsWith("remove_", StringComparison.Ordinal));

    private static string Parameters(MethodBase method, NullabilityInfoContext context)
        => Parameters(method.GetParameters(), context);

    private static string Parameters(ParameterInfo[] parameters, NullabilityInfoContext context)
        => string.Join(", ", parameters.Select(parameter => Parameter(parameter, context)));

    private static string Parameter(ParameterInfo parameter, NullabilityInfoContext context)
    {
        var builder = new StringBuilder();

        if (parameter.IsOut)
        {
            builder.Append("out ");
        }
        else if (parameter.ParameterType.IsByRef)
        {
            builder.Append(parameter.IsIn ? "in " : "ref ");
        }

        if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
        {
            builder.Append("params ");
        }

        builder.Append(Name(parameter.ParameterType, context.Create(parameter)))
            .Append(' ')
            .Append(parameter.Name);

        if (parameter.HasDefaultValue)
        {
            builder.Append(" = ").Append(Literal(parameter.DefaultValue));
        }

        return builder.ToString();
    }

    private static string Literal(object? value) => value switch
    {
        null => "null",
        string text => $"\"{string.Concat(text.Select(Escape))}\"",
        bool flag => flag ? "true" : "false",
        char character => $"'{Escape(character)}'",
        Enum @enum => $"{Name(@enum.GetType(), null)}.{@enum}",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string Escape(char character)
        => character is >= ' ' and <= '~' && character != '\\'
            ? character.ToString()
            : $"\\u{(int)character:X4}";

    private static string Kind(Type type) => type switch
    {
        { IsEnum: true } => "enum",
        { IsInterface: true } => "interface",
        _ when typeof(Delegate).IsAssignableFrom(type) => "delegate",
        { IsValueType: true } => "struct",
        { IsAbstract: true, IsSealed: true } => "static class",
        { IsAbstract: true } => "abstract class",
        { IsSealed: true } => "sealed class",
        _ => "class",
    };

    private static string Accessibility(Type type) => type switch
    {
        { IsNested: false } => "public",
        { IsNestedPublic: true } => "public",
        { IsNestedFamORAssem: true } => "protected internal",
        { IsNestedFamily: true } => "protected",
        _ => "internal",
    };

    private static string Accessibility(MethodBase method) => method switch
    {
        { IsPublic: true } => "public",
        { IsFamilyOrAssembly: true } => "protected internal",
        { IsFamily: true } => "protected",
        _ => "internal",
    };

    private static string Accessibility(FieldInfo field) => field switch
    {
        { IsPublic: true } => "public",
        { IsFamilyOrAssembly: true } => "protected internal",
        { IsFamily: true } => "protected",
        _ => "internal",
    };

    private static string Modifiers(MethodBase method)
    {
        var builder = new StringBuilder(Accessibility(method));

        if (method.IsStatic)
        {
            builder.Append(" static");
        }

        if (method.IsAbstract)
        {
            builder.Append(" abstract");
        }
        else if (method is { IsVirtual: true, IsFinal: false } && !method.DeclaringType!.IsInterface)
        {
            builder.Append(" virtual");
        }

        return builder.ToString();
    }

    private static string PropertyModifiers(PropertyInfo property)
        => Modifiers((property.GetGetMethod(true) ?? property.GetSetMethod(true))!);

    private static string EventModifiers(EventInfo @event)
        => Modifiers((@event.GetAddMethod(true) ?? @event.GetRemoveMethod(true))!);

    private static string FieldModifiers(FieldInfo field)
    {
        var builder = new StringBuilder(Accessibility(field));

        if (field.IsLiteral)
        {
            builder.Append(" const");
        }
        else if (field.IsStatic)
        {
            builder.Append(" static");
        }

        if (field.IsInitOnly)
        {
            builder.Append(" readonly");
        }

        return builder.ToString();
    }

    private static string Name(Type type, NullabilityInfo? info)
    {
        if (type.IsByRef)
        {
            return Name(type.GetElementType()!, info);
        }

        if (type.IsPointer)
        {
            return Name(type.GetElementType()!, null) + "*";
        }

        if (type.IsArray)
        {
            var brackets = $"[{new string(',', type.GetArrayRank() - 1)}]";
            return Name(type.GetElementType()!, info?.ElementType) + brackets + Suffix(type, info);
        }

        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            var argument = info?.GenericTypeArguments is { Length: 1 } single ? single[0] : null;
            return Name(underlying, argument) + "?";
        }

        if (type.IsGenericParameter)
        {
            return type.Name + Suffix(type, info);
        }

        if (!type.IsGenericType)
        {
            return Bare(type) + Suffix(type, info);
        }

        var arguments = type.GetGenericArguments();
        var nested = info?.GenericTypeArguments;
        var rendered = arguments.Select((argument, index) =>
            Name(argument, nested is { } list && index < list.Length ? list[index] : null));

        return $"{Bare(type.GetGenericTypeDefinition())}<{string.Join(", ", rendered)}>{Suffix(type, info)}";
    }

    private static string Suffix(Type type, NullabilityInfo? info)
        => !type.IsValueType && info?.ReadState == NullabilityState.Nullable ? "?" : string.Empty;

    private static string Bare(Type type)
    {
        var full = (type.FullName ?? (type.Namespace is null ? type.Name : $"{type.Namespace}.{type.Name}"))
            .Replace('+', '.');
        var builder = new StringBuilder();

        foreach (var segment in full.Split('.'))
        {
            var tick = segment.IndexOf('`', StringComparison.Ordinal);
            builder.Append(tick < 0 ? segment : segment[..tick]).Append('.');
        }

        return builder.ToString(0, builder.Length - 1);
    }

    private static string DisplayName(Type type)
    {
        if (!type.IsGenericTypeDefinition)
        {
            return Bare(type);
        }

        return $"{Bare(type)}<{string.Join(", ", type.GetGenericArguments().Select(Variance))}>";
    }

    private static string Variance(Type argument)
    {
        var attributes = argument.GenericParameterAttributes & GenericParameterAttributes.VarianceMask;

        return attributes switch
        {
            GenericParameterAttributes.Covariant => "out " + argument.Name,
            GenericParameterAttributes.Contravariant => "in " + argument.Name,
            _ => argument.Name,
        };
    }

    private static bool IsVisibleOutside(Type type)
    {
        if (!type.IsNested)
        {
            return type.IsPublic;
        }

        return (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem)
            && IsVisibleOutside(type.DeclaringType!);
    }

    private static bool IsVisibleOutside(MethodBase method)
        => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;

    private static bool IsVisibleOutside(FieldInfo field)
        => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static bool IsVisibleOutside(PropertyInfo property)
        => (property.GetGetMethod(true) is { } getter && IsVisibleOutside(getter))
            || (property.GetSetMethod(true) is { } setter && IsVisibleOutside(setter));

    private static bool IsVisibleOutside(EventInfo @event)
        => (@event.GetAddMethod(true) is { } adder && IsVisibleOutside(adder))
            || (@event.GetRemoveMethod(true) is { } remover && IsVisibleOutside(remover));

    private static IReadOnlyList<string> Baseline()
    {
        using var stream = typeof(PublicApiBaselineTests).Assembly.GetManifestResourceStream(BaselineResource)
            ?? throw new FileNotFoundException($"Embedded resource '{BaselineResource}' not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var lines = new List<string>();

        while (reader.ReadLine() is { } line)
        {
            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }
}
