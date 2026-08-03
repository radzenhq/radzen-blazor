#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Fonts;
using Xunit;
using Xunit.Sdk;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentElementChangeTrackingMatrixTests
{
    private static readonly Type[] Tracked =
    [
        typeof(TextContent),
        typeof(PathContent),
        typeof(XObjectContent),
        typeof(RawContent),
        typeof(InlineImageContent),
    ];

    private static readonly byte[] InlineImagePayload = [0x00, 0x01, 0xFF, 0x0A];

    private static byte[] Source()
    {
        var bytes = new List<byte>();
        bytes.AddRange(Encoding.ASCII.GetBytes("BI /W 2 /H 1 /CS /RGB /BPC 8 ID "));
        bytes.AddRange(InlineImagePayload);
        bytes.AddRange(Encoding.ASCII.GetBytes(
            " EI\n" +
            "BT /F0 12 Tf 10 700 Td (Hi) Tj ET\n" +
            "[3 2] 0 d\n" +
            "0 0 m 10 10 l S\n" +
            "/Im0 Do\n" +
            "/Sh0 sh\n"));
        return [.. bytes];
    }

    private static ContentCollection Materialized()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Source());
        return InterpreterTestSupport.Load(document.ToArray()).Pages[0].Content;
    }

    private static ContentElement Element(Type type)
    {
        foreach (var element in Materialized())
        {
            if (element.GetType() == type)
            {
                return element;
            }
        }

        throw new XunitException($"The fixture content stream did not materialize a {type.Name}.");
    }

    private static byte[] EmitBytes(ContentElement element)
    {
        using var writer = new ContentWriter();
        element.Emit(writer);
        return writer.ToArray();
    }

    [Fact]
    public void EveryMutableMemberOfALoadedElementOpensAChangeDetectionDoor()
    {
        foreach (var type in Tracked)
        {
            foreach (var property in SettableProperties(type, typeof(ContentElement)))
            {
                AssertDoor(type, property.Name, () => Element(type), (element, _) => Mutate(property, element));
            }

            foreach (var method in PublicVoidMutators(type, typeof(ContentElement)))
            {
                AssertDoor(type, method.Name + "()", () => Element(type), (element, _) => Invoke(method, element));
            }
        }

        foreach (var property in SettableProperties(typeof(Font), typeof(object)))
        {
            AssertDoor(typeof(Font), property.Name, TextFixture, (_, nested) => Mutate(property, nested!));
        }

        foreach (var property in SettableProperties(typeof(GradientBrush), typeof(object)))
        {
            AssertDoor(typeof(GradientBrush), property.Name, GradientFixture, (_, nested) => Mutate(property, nested!));
        }
    }

    private static ContentElement TextFixture() => Element(typeof(TextContent));

    private static ContentElement GradientFixture()
    {
        var path = (PathContent)Element(typeof(PathContent));
        path.FillGradient = new LinearGradient(0, 0, 10, 10,
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));
        path.AcceptChanges();
        return path;
    }

    private static object? Nested(Type owner, ContentElement element) => owner == typeof(Font)
        ? ((TextContent)element).Font
        : owner == typeof(GradientBrush) ? ((PathContent)element).FillGradient : element;

    private static void AssertDoor(Type owner, string member, Func<ContentElement> fixture, Action<ContentElement, object?> mutate)
    {
        var element = fixture();
        Assert.False(element.IsModified, $"{owner.Name}.{member}: the materialized fixture was already modified before the mutation.");

        var baseline = EmitBytes(element);
        mutate(element, Nested(owner, element));

        if (element.IsModified)
        {
            return;
        }

        Assert.True(EmitBytes(element).SequenceEqual(baseline),
            $"{owner.Name}.{member}: mutation changed emitted bytes but the flag stayed clean");
    }

    private static void Mutate(PropertyInfo property, object target)
        => property.SetValue(target, DistinctValue(property.PropertyType, property.GetValue(target), $"{property.DeclaringType?.Name}.{property.Name}"));

    private static void Invoke(MethodInfo method, object target)
        => method.Invoke(target, [.. method.GetParameters().Select(p => MethodArgument(p.ParameterType, $"{method.Name}({p.Name})"))]);

    private static IEnumerable<PropertyInfo> SettableProperties(Type type, Type stopAfter)
    {
        foreach (var declaring in Hierarchy(type, stopAfter))
        {
            foreach (var property in declaring.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (property.SetMethod is { } setter && (setter.IsPublic || setter.IsAssembly || setter.IsFamilyOrAssembly)
                    && !IsInitOnly(setter) && property.GetIndexParameters().Length == 0)
                {
                    yield return property;
                }
            }
        }
    }

    private static bool IsInitOnly(MethodInfo setter)
        => setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

    private static IEnumerable<MethodInfo> PublicVoidMutators(Type type, Type stopAfter)
    {
        foreach (var declaring in Hierarchy(type, stopAfter))
        {
            foreach (var method in declaring.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (method.ReturnType == typeof(void) && !method.IsSpecialName)
                {
                    yield return method;
                }
            }
        }
    }

    private static IEnumerable<Type> Hierarchy(Type type, Type stopAfter)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            yield return current;
            if (current == stopAfter)
            {
                yield break;
            }
        }
    }

    private static object? DistinctValue(Type type, object? current, string member)
    {
        if (Nullable.GetUnderlyingType(type) is { } underlying)
        {
            return current is null ? SomeValue(underlying, member) : null;
        }

        if (type == typeof(bool))
        {
            return !(bool)current!;
        }

        if (type == typeof(double))
        {
            return (double)current! + 17.25;
        }

        if (type == typeof(Unit))
        {
            return Unit.FromPoint(((Unit)current!).Point + 17.25);
        }

        if (type == typeof(string))
        {
            return (string?)current == "probe" ? "other" : "probe";
        }

        if (type == typeof(Matrix))
        {
            return (Matrix)current! == Matrix.Identity ? Matrix.Translate(3, 4) : Matrix.Identity;
        }

        if (type == typeof(Color))
        {
            return (Color)current! == Color.Red ? Color.Blue : Color.Red;
        }

        if (type.IsEnum)
        {
            foreach (var value in Enum.GetValues(type))
            {
                if (!value.Equals(current))
                {
                    return value;
                }
            }

            throw new XunitException($"{member}: enum {type.Name} has no second value to distinguish with.");
        }

        if (type == typeof(Font))
        {
            return new Font { Size = ((Font?)current)?.Size + 13 ?? 13 };
        }

        if (type == typeof(ReverseFont))
        {
            return current is null ? ReverseFont.FromBase14("Helvetica") : null;
        }

        if (type == typeof(GradientBrush))
        {
            return current is null ? SomeValue(typeof(GradientBrush), member) : null;
        }

        throw new XunitException(
            $"{member}: no distinct-value rule for type {type}. Add one - a member left uncovered is exactly what this matrix exists to catch.");
    }

    private static object SomeValue(Type type, string member)
    {
        if (type == typeof(double))
        {
            return 19.5;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(Color))
        {
            return Color.Red;
        }

        if (type == typeof(ReadOnlyMemory<byte>))
        {
            return new ReadOnlyMemory<byte>([0x5A, 0x5B]);
        }

        if (type == typeof(ReadOnlyMemory<double>))
        {
            return new ReadOnlyMemory<double>([9.5, 8.5]);
        }

        if (type == typeof(ImmutableArray<TextAdjustment>))
        {
            return ImmutableArray.Create(new TextAdjustment([0x5A], -33));
        }

        if (type == typeof(DeviceColor))
        {
            return DeviceColor.Cmyk(0.25, 0.25, 0.25, 0.25);
        }

        if (type == typeof(GradientBrush))
        {
            return new LinearGradient(0, 0, 5, 5, new GradientStop(0, Color.Green), new GradientStop(1, Color.Yellow));
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0)!;
        }

        throw new XunitException(
            $"{member}: no sample-value rule for type {type}. Add one - a member left uncovered is exactly what this matrix exists to catch.");
    }

    private static object MethodArgument(Type type, string member)
    {
        if (type == typeof(Unit))
        {
            return Unit.FromPoint(7);
        }

        if (type == typeof(double))
        {
            return 0.25;
        }

        if (type == typeof(double[]))
        {
            return new double[] { 4, 5 };
        }

        throw new XunitException(
            $"{member}: no argument rule for parameter type {type}. Add one - a mutator left uncovered is exactly what this matrix exists to catch.");
    }
}
