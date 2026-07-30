#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class ClosedModelHierarchyTests
{
    public static TheoryData<Type> ClosedBases() => new() { typeof(Block), typeof(Inline), typeof(TextInline) };

    [Theory]
    [MemberData(nameof(ClosedBases))]
    public void ClosedHierarchy_HasNoTypeAnExternalAssemblyCouldDeriveFrom(Type root)
    {
        var derivable = Family(root).Where(IsExternallyDerivable).Select(type => type.Name).ToList();

        Assert.Empty(derivable);
    }

    [Theory]
    [MemberData(nameof(ClosedBases))]
    public void ClosedHierarchy_IsNotEmpty(Type root)
    {
        Assert.NotEmpty(Family(root).Where(type => type != root));
    }

    private static IEnumerable<Type> Family(Type root)
        => root.Assembly.GetTypes().Where(type => type == root || type.IsSubclassOf(root));

    private static bool IsExternallyDerivable(Type type)
        => !type.IsSealed
            && type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(constructor => constructor.IsPublic || constructor.IsFamily || constructor.IsFamilyOrAssembly);
}
