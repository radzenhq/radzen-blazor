#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class PublicApiBaselineTests
{
    private static readonly string[] NeutralNamespaces =
    [
        "Radzen.Documents",
        "Radzen.Documents.Codes",
        "Radzen.Documents.Core",
        "Radzen.Documents.Fonts",
    ];

    private const string BaselineResource = "Radzen.Blazor.Tests.Documents.PublicApiBaseline.txt";

    [Fact]
    public void PublicSurfaceOfTheNeutralNamespaces_MatchesTheApprovedBaseline()
    {
        var actual = Surface();
        var expected = Baseline();

        Assert.Equal(
            string.Join(Environment.NewLine, expected),
            string.Join(Environment.NewLine, actual));
    }

    internal static IReadOnlyList<string> Surface()
    {
        var lines = new List<string>();
        foreach (var type in typeof(Document).Assembly.GetTypes()
            .Where(type => type.Namespace is { } declared
                && NeutralNamespaces.Contains(declared, StringComparer.Ordinal)
                && IsPubliclyVisible(type)))
        {
            lines.Add($"{Kind(type)} {Name(type)}");
            lines.AddRange(Members(type));
        }

        lines.Sort(StringComparer.Ordinal);
        return lines;
    }

    private static IEnumerable<string> Members(Type type)
    {
        const BindingFlags Declared =
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var constructor in type.GetConstructors(Declared))
        {
            yield return $"{Name(type)}..ctor({Parameters(constructor)})";
        }

        foreach (var property in type.GetProperties(Declared))
        {
            var accessors = string.Concat(
                property.GetGetMethod() is not null ? " get;" : string.Empty,
                property.GetSetMethod() is not null ? " set;" : string.Empty);
            var index = property.GetIndexParameters();
            var name = index.Length == 0
                ? property.Name
                : $"{property.Name}[{string.Join(", ", index.Select(parameter => Name(parameter.ParameterType)))}]";
            yield return $"{Name(type)}.{name} : {Name(property.PropertyType)} {{{accessors} }}";
        }

        foreach (var method in type.GetMethods(Declared).Where(method => !IsAccessor(method)))
        {
            yield return $"{Name(type)}.{method.Name}({Parameters(method)}) : {Name(method.ReturnType)}";
        }

        foreach (var field in type.GetFields(Declared).Where(field => !field.IsSpecialName))
        {
            yield return $"{Name(type)}.{field.Name} : {Name(field.FieldType)}";
        }

        foreach (var @event in type.GetEvents(Declared))
        {
            yield return $"{Name(type)}.{@event.Name} : event {Name(@event.EventHandlerType!)}";
        }
    }

    private static bool IsAccessor(MethodInfo method)
        => method.IsSpecialName
            && (method.Name.StartsWith("get_", StringComparison.Ordinal)
                || method.Name.StartsWith("set_", StringComparison.Ordinal)
                || method.Name.StartsWith("add_", StringComparison.Ordinal)
                || method.Name.StartsWith("remove_", StringComparison.Ordinal));

    private static string Parameters(MethodBase method)
        => string.Join(", ", method.GetParameters().Select(parameter => Name(parameter.ParameterType)));

    private static string Kind(Type type) => type switch
    {
        { IsEnum: true } => "enum",
        { IsInterface: true } => "interface",
        { IsValueType: true } => "struct",
        { IsAbstract: true, IsSealed: true } => "static class",
        { IsAbstract: true } => "abstract class",
        { IsSealed: true } => "sealed class",
        _ => "class",
    };

    private static string Name(Type type)
    {
        if (type.IsByRef)
        {
            return "ref " + Name(type.GetElementType()!);
        }

        if (type.IsArray)
        {
            return Name(type.GetElementType()!) + "[]";
        }

        if (!type.IsGenericType)
        {
            return type.FullName ?? type.Name;
        }

        var definition = type.GetGenericTypeDefinition().FullName ?? type.Name;
        var trimmed = definition[..definition.IndexOf('`', StringComparison.Ordinal)];
        return $"{trimmed}<{string.Join(", ", type.GetGenericArguments().Select(Name))}>";
    }

    private static bool IsPubliclyVisible(Type type)
        => type.IsNested
            ? type.IsNestedPublic && IsPubliclyVisible(type.DeclaringType!)
            : type.IsPublic;

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
