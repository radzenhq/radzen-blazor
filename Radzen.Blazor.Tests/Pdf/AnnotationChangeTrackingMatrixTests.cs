#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Radzen.Documents.Pdf;
using Xunit;
using Xunit.Sdk;

namespace Radzen.Blazor.Pdf.Tests;

public class AnnotationChangeTrackingMatrixTests
{
    private static readonly Type[] Tracked =
    [
        typeof(TextAnnotation),
        typeof(HighlightAnnotation),
        typeof(UnderlineAnnotation),
        typeof(StrikeOutAnnotation),
        typeof(SquigglyAnnotation),
        typeof(LinkAnnotation),
        typeof(StampAnnotation),
        typeof(InkAnnotation),
        typeof(FreeTextAnnotation),
        typeof(SquareAnnotation),
        typeof(CircleAnnotation),
    ];

    private static readonly PdfRect Bounds = PdfRect.FromSize(20, 30, 100, 40);

    private static Annotation New(Type type)
    {
        var annotation = (Annotation)Activator.CreateInstance(type, Bounds)!;
        if (annotation is InkAnnotation ink)
        {
            ink.Strokes.Add([new AnnotationPoint(25, 35), new AnnotationPoint(60, 55)]);
        }

        if (annotation is LinkAnnotation link)
        {
            link.Uri = new Uri("https://example.com/");
        }

        return annotation;
    }

    private static Document Loaded(Type type)
    {
        var source = new Document();
        source.Pages.Add().Annotations.Add(New(type));
        using var stream = new MemoryStream(source.ToArray());
        return Document.LoadFromStream(stream);
    }

    [Fact]
    public void EveryAnnotationSubclassIsTracked()
    {
        var discovered = typeof(Annotation).Assembly.GetTypes()
            .Where(type => typeof(Annotation).IsAssignableFrom(type) && !type.IsAbstract)
            .ToList();

        Assert.NotEmpty(discovered);
        Assert.Equal([.. discovered.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal)],
            [.. Tracked.Select(type => type.Name).OrderBy(name => name, StringComparer.Ordinal)]);
    }

    [Fact]
    public void EveryMutableMemberOfALoadedAnnotationOpensAChangeDetectionDoor()
    {
        var checkedMembers = 0;
        foreach (var type in Tracked)
        {
            foreach (var property in SettableProperties(type))
            {
                AssertDoor(type, property.Name, annotation => Mutate(property, annotation));
                checkedMembers++;
            }

            foreach (var property in CollectionProperties(type))
            {
                AssertDoor(type, property.Name + ".Add()", annotation => AddTo(property, annotation));
                checkedMembers++;
            }
        }

        AssertDoor(typeof(InkAnnotation), "Strokes[0].Add()",
            annotation => ((InkAnnotation)annotation).Strokes[0].Add(new AnnotationPoint(70, 75)));
        checkedMembers++;

        foreach (var property in SettableProperties(typeof(Font), typeof(object)))
        {
            AssertDoor(typeof(FreeTextAnnotation), "Font." + property.Name,
                annotation => Mutate(property, ((FreeTextAnnotation)annotation).Font));
            checkedMembers++;
        }

        Assert.InRange(checkedMembers, 80, 200);
    }

    [Fact]
    public void EveryStructuralMutationOfALoadedAnnotationListIsAChange()
    {
        AssertContainerDoor("RemoveAt", annotations => annotations.RemoveAt(0));
        AssertContainerDoor("Remove", annotations => annotations.Remove(annotations[0]));
        AssertContainerDoor("Clear", annotations => annotations.Clear());
        AssertContainerDoor("Add", annotations => annotations.Add(new SquareAnnotation(Bounds)));
        AssertContainerDoor("Insert", annotations => annotations.Insert(0, new SquareAnnotation(Bounds)));
    }

    private static void AssertContainerDoor(string member, Action<AnnotationCollection> mutate)
    {
        var document = Loaded(typeof(SquareAnnotation));
        var annotations = document.Pages[0].Annotations;
        Assert.False(annotations.HasChanges, $"AnnotationCollection.{member}: the loaded fixture was already changed.");

        mutate(annotations);

        Assert.True(annotations.HasChanges, $"AnnotationCollection.{member}: mutation left the collection reading unchanged.");
    }

    private static void AssertDoor(Type owner, string member, Action<Annotation> mutate)
    {
        var document = Loaded(owner);
        var annotation = document.Pages[0].Annotations[0];
        Assert.False(annotation.IsModified, $"{owner.Name}.{member}: the loaded fixture was already modified before the mutation.");

        mutate(annotation);

        if (annotation.IsModified)
        {
            return;
        }

        Assert.True(Rebuilt(owner, mutate).SequenceEqual(Rebuilt(owner, _ => { })),
            $"{owner.Name}.{member}: mutation changed emitted bytes but the flag stayed clean");
    }

    private static byte[] Rebuilt(Type owner, Action<Annotation> mutate)
    {
        var document = Loaded(owner);
        var annotation = document.Pages[0].Annotations[0];
        mutate(annotation);
        annotation.Bounds = annotation.Bounds;
        return document.ToArray();
    }

    private static void Mutate(PropertyInfo property, object target)
        => property.SetValue(target, DistinctValue(property.PropertyType, property.GetValue(target), $"{property.DeclaringType?.Name}.{property.Name}"));

    private static void AddTo(PropertyInfo property, object target)
    {
        var collection = property.GetValue(target)!;
        var item = property.PropertyType.GetGenericArguments()[0];
        collection.GetType().GetMethod("Add")!.Invoke(collection, [SomeValue(item, $"{property.DeclaringType?.Name}.{property.Name}")]);
    }

    private static IEnumerable<PropertyInfo> SettableProperties(Type type, Type? stopAfter = null)
    {
        foreach (var declaring in Hierarchy(type, stopAfter ?? typeof(Annotation)))
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

    private static IEnumerable<PropertyInfo> CollectionProperties(Type type)
    {
        foreach (var declaring in Hierarchy(type, typeof(Annotation)))
        {
            foreach (var property in declaring.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (property.SetMethod is null && property.PropertyType.IsGenericType
                    && property.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    yield return property;
                }
            }
        }
    }

    private static bool IsInitOnly(MethodInfo setter)
        => setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

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

        if (type == typeof(string))
        {
            return (string?)current == "probe" ? "other" : "probe";
        }

        if (type == typeof(PdfRect))
        {
            return PdfRect.FromSize(11, 12, 77, 88);
        }

        if (type == typeof(Color))
        {
            return (Color)current! == Color.Red ? Color.Blue : Color.Red;
        }

        if (type == typeof(Font))
        {
            return new Font { Size = ((Font?)current)?.Size + 13 ?? 13 };
        }

        if (type == typeof(AnnotationAppearance))
        {
            return current is null ? SomeValue(typeof(AnnotationAppearance), member) : null;
        }

        if (type == typeof(Uri))
        {
            return current is null ? SomeValue(typeof(Uri), member) : null;
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

        throw new XunitException(
            $"{member}: no distinct-value rule for type {type}. Add one - a member left uncovered is exactly what this matrix exists to catch.");
    }

    private static object SomeValue(Type type, string member)
    {
        if (type == typeof(double))
        {
            return 19.5;
        }

        if (type == typeof(int))
        {
            return 0;
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(string))
        {
            return "probe";
        }

        if (type == typeof(Color))
        {
            return Color.Green;
        }

        if (type == typeof(PdfRect))
        {
            return PdfRect.FromSize(5, 6, 30, 12);
        }

        if (type == typeof(AnnotationPoint))
        {
            return new AnnotationPoint(44, 45);
        }

        if (type == typeof(InkStroke))
        {
            return new InkStroke { new AnnotationPoint(12, 13), new AnnotationPoint(14, 15) };
        }

        if (type == typeof(Uri))
        {
            return new Uri("https://example.com/probe");
        }

        if (type == typeof(AnnotationAppearance))
        {
            var appearance = new AnnotationAppearance();
            appearance.Content.Add(new PathContent { FillColor = Color.Green });
            return appearance;
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0)!;
        }

        throw new XunitException(
            $"{member}: no sample-value rule for type {type}. Add one - a member left uncovered is exactly what this matrix exists to catch.");
    }
}
