#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Radzen.Documents.Geometry;
using Radzen.Documents.Layout;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class TableOfContentsPassScopingTests
{
    private static SemanticStructureTree Structure()
        => DocumentLayouter.Layout(DocumentLayoutGeometryTests.TableOfContentsDocument()).Semantics.Structure;

    private static IEnumerable<int> Descendants(SemanticStructureTree tree, int index)
    {
        yield return index;
        foreach (var child in tree.Nodes[index].Children)
        {
            foreach (var descendant in Descendants(tree, child))
            {
                yield return descendant;
            }
        }
    }

    private static int NavigationRoot(SemanticStructureTree tree)
        => Enumerable.Range(0, tree.Nodes.Length)
            .Single(index => tree.Nodes[index].Intent == SemanticIntent.Navigation);

    private static int Associations(SemanticStructureTree tree, int element)
        => tree.Associations.Count(association => association.Element == element);

    [Fact]
    public void TocEntryElements_CarryOneAssociationPerLiveSource()
    {
        var tree = Structure();
        var navigation = tree.Nodes[NavigationRoot(tree)];

        Assert.Equal(2, navigation.Children.Length);

        foreach (var entry in navigation.Children)
        {
            var reference = Assert.Single(tree.Nodes[entry].Children);
            Assert.Equal(SemanticIntent.CrossReference, tree.Nodes[reference].Intent);
            var link = Assert.Single(tree.Nodes[reference].Children);
            Assert.Equal(SemanticIntent.Link, tree.Nodes[link].Intent);

            Assert.Equal(1, Associations(tree, reference));
            Assert.Equal(2, Associations(tree, link));
        }
    }

    [Fact]
    public void TocSubtree_CarriesNoAssociationsBeyondTheLiveEntrySources()
    {
        var tree = Structure();
        var navigation = NavigationRoot(tree);
        var elements = Descendants(tree, navigation).ToHashSet();

        Assert.Equal(3 * 2, tree.Associations.Count(association => elements.Contains(association.Element)));
    }

    [Fact]
    public void EveryAssociationSource_IsReachableFromTheRenderedGeometry()
    {
        var laidOut = DocumentLayouter.Layout(DocumentLayoutGeometryTests.TableOfContentsDocument());
        var reachable = new HashSet<SourceId>();

        foreach (var page in laidOut.Pages)
        {
            foreach (var layer in new[] { page.Body, page.HeaderLayer, page.FooterLayer })
            {
                Assert.Empty(layer.Tables);
                Assert.Empty(layer.Boxes);
                Assert.Empty(layer.Images);
                Assert.Empty(layer.CodeSymbols);
                Collect(reachable, layer.Lines);
            }
        }

        var unreachable = laidOut.Semantics.Structure.Associations
            .Select(association => association.Source)
            .Where(source => !reachable.Contains(source))
            .ToArray();

        Assert.Empty(unreachable);
    }

    private static void Collect(HashSet<SourceId> sources, ImmutableArray<LaidOutLine> lines)
    {
        foreach (var line in lines)
        {
            sources.Add(line.Source);
            foreach (var fragment in line.Line.Fragments)
            {
                sources.Add(fragment.Source);
            }
        }
    }
}
