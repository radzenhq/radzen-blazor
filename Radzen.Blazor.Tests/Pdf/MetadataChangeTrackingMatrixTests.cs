#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Xunit.Sdk;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class MetadataChangeTrackingMatrixTests
{
    private static readonly Type[] Tracked =
    [
        typeof(DocumentInfo),
        typeof(OutlineItem),
        typeof(PageLabel),
        typeof(Attachment),
        typeof(FacturXProfile),
    ];

    private static byte[] Source()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.Pages.Add();
        document.Info.Title = "title";
        document.Info.Author = "author";
        document.Outline.Add(new OutlineItem("root", OutlineTarget.ToPage(0))
        {
            Children = { new OutlineItem("child", OutlineTarget.ToPage(1)) },
        });
        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.Decimal });
        document.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.UppercaseRoman, Prefix = "A" });
        document.Attachments.Add("factur-x.xml", [1, 2, 3], AttachmentRelationship.Data, "text/xml")
            .FacturX = new FacturXProfile();
        return document.ToArray();
    }

    private static PortableDocument Loaded()
    {
        using var stream = new MemoryStream(Source());
        return PortableDocument.LoadFromStream(stream);
    }

    private static object Target(Type owner, PortableDocument document) => owner switch
    {
        _ when owner == typeof(DocumentInfo) => document.Info,
        _ when owner == typeof(OutlineItem) => document.Outline[0],
        _ when owner == typeof(PageLabel) => document.PageLabels[1],
        _ when owner == typeof(Attachment) => document.Attachments[0],
        _ when owner == typeof(FacturXProfile) => Profile(document),
        _ => throw new XunitException($"No probe target for {owner}."),
    };

    private static FacturXProfile Profile(PortableDocument document)
    {
        var attachment = document.Attachments[0];
        attachment.FacturX ??= new FacturXProfile();
        document.AcceptMetadataChanges();
        return attachment.FacturX;
    }

    private static bool AnythingModified(PortableDocument document)
        => document.Info.IsModified || document.OutlineChanged || document.PageLabelsChanged
            || document.Attachments.IsModified;

    [Fact]
    public void LoadingFreezesTheWholeMetadataModel()
    {
        var document = Loaded();

        Assert.False(document.Info.IsModified);
        Assert.False(document.OutlineChanged);
        Assert.False(document.PageLabelsChanged);
        Assert.False(document.Attachments.IsModified);
    }

    [Fact]
    public void AssigningACollectionItemToItselfLeavesTrackingAndTheMaterializedGraphUntouched()
    {
        var document = Loaded();
        var outline = document.Outline;
        var graph = new DocumentObjectGraph();
        document.MaterializedGraph = graph;

        outline[0] = outline[0];

        Assert.False(document.OutlineChanged);
        Assert.Same(graph, document.MaterializedGraph);
    }

    [Fact]
    public void EveryMutableMemberOfLoadedMetadataOpensAChangeDetectionDoor()
    {
        foreach (var owner in Tracked)
        {
            foreach (var property in SettableProperties(owner))
            {
                AssertDoor(owner, property.Name, document => Mutate(property, Target(owner, document)));
            }
        }

        AssertDoor(typeof(OutlineItem), "Children.Add()",
            document => document.Outline[0].Children.Add(new OutlineItem("added", OutlineTarget.ToPage(1))));

        AssertDoor(typeof(OutlineItem), "Children[0].Title",
            document => document.Outline[0].Children[0].Title = "renamed");

    }

    [Fact]
    public void EveryStructuralMutationOfLoadedMetadataIsAChange()
    {
        AssertContainer("Outline.RemoveAt", document => document.Outline.RemoveAt(0));
        AssertContainer("Outline.Clear", document => document.Outline.Clear());
        AssertContainer("Outline.Add", document => document.Outline.Add(new OutlineItem("new", null)));
        AssertContainer("Outline[0].Children.Clear", document => document.Outline[0].Children.Clear());
        AssertContainer("PageLabels.RemoveAt", document => document.PageLabels.RemoveAt(1));
        AssertContainer("PageLabels.Clear", document => document.PageLabels.Clear());
        AssertContainer("PageLabels.Add", document => document.PageLabels.Add(new PageLabel(1)));
        AssertContainer("Attachments.RemoveAt", document => document.Attachments.RemoveAt(0));
        AssertContainer("Attachments.Clear", document => document.Attachments.Clear());
        AssertContainer("Attachments.Add",
            document => document.Attachments.Add("extra.bin", [9], AttachmentRelationship.Data, "application/octet-stream"));
    }

    private static void AssertContainer(string member, Action<PortableDocument> mutate)
    {
        var document = Loaded();
        Assert.False(AnythingModified(document), $"{member}: the loaded fixture was already changed.");

        mutate(document);

        Assert.True(AnythingModified(document), $"{member}: mutation left the metadata reading unchanged.");
    }

    private static void AssertDoor(Type owner, string member, Action<PortableDocument> mutate)
    {
        var document = Loaded();
        Assert.False(AnythingModified(document), $"{owner.Name}.{member}: the loaded fixture was already modified.");

        mutate(document);

        if (AnythingModified(document))
        {
            return;
        }

        Assert.True(Rebuilt(mutate).SequenceEqual(Rebuilt(_ => { })),
            $"{owner.Name}.{member}: mutation changed emitted bytes but the flag stayed clean");
    }

    private static byte[] Rebuilt(Action<PortableDocument> mutate)
    {
        var document = Loaded();
        mutate(document);
        document.Info.Title = document.Info.Title;
        foreach (var item in document.Outline)
        {
            Force(item);
        }

        foreach (var label in document.PageLabels)
        {
            label.Start = label.Start;
        }

        foreach (var attachment in document.Attachments)
        {
            attachment.Description = attachment.Description;
            if (attachment.FacturX is { } profile)
            {
                profile.Version = profile.Version;
            }
        }

        return document.ToArray();
    }

    private static void Force(OutlineItem item)
    {
        item.Title = item.Title;
        foreach (var child in item.Children)
        {
            Force(child);
        }
    }

    private static void Mutate(PropertyInfo property, object target)
        => property.SetValue(target, DistinctValue(property.PropertyType, property.GetValue(target), $"{property.DeclaringType?.Name}.{property.Name}"));

    private static IEnumerable<PropertyInfo> SettableProperties(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
        {
            if (property.SetMethod is { } setter && (setter.IsPublic || setter.IsAssembly || setter.IsFamilyOrAssembly)
                && !IsInitOnly(setter) && property.GetIndexParameters().Length == 0)
            {
                yield return property;
            }
        }
    }

    private static bool IsInitOnly(MethodInfo setter)
        => setter.ReturnParameter.GetRequiredCustomModifiers()
            .Any(modifier => modifier.FullName == "System.Runtime.CompilerServices.IsExternalInit");

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

        if (type == typeof(int))
        {
            return (int)current! + 7;
        }

        if (type == typeof(string))
        {
            return (string?)current == "probe" ? "other" : "probe";
        }

        if (type == typeof(DateTimeOffset))
        {
            return ((DateTimeOffset)current!).AddDays(3);
        }

        if (type == typeof(OutlineTarget))
        {
            return current is null ? SomeValue(typeof(OutlineTarget), member) : null;
        }

        if (type == typeof(FacturXProfile))
        {
            return current is null ? SomeValue(typeof(FacturXProfile), member) : null;
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
        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 1;
        }

        if (type == typeof(string))
        {
            return "probe";
        }

        if (type == typeof(Color))
        {
            return Color.Green;
        }

        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffset(2021, 4, 5, 6, 7, 8, TimeSpan.Zero);
        }

        if (type == typeof(OutlineTarget))
        {
            return OutlineTarget.ToPageFit(1);
        }

        if (type == typeof(FacturXProfile))
        {
            return new FacturXProfile { Version = "9.9" };
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0)!;
        }

        throw new XunitException(
            $"{member}: no sample-value rule for type {type}. Add one - a member left uncovered is exactly what this matrix exists to catch.");
    }
}
