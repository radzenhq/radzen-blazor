using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Pdf;

internal sealed record OutlineSnapshot(
    string Title,
    bool HasTarget,
    string? Anchor,
    int? PageIndex,
    OutlineFit Fit,
    double[] FitArguments,
    Color? Color,
    bool Bold,
    bool Italic,
    bool Collapsed,
    IReadOnlyList<OutlineSnapshot> Children)
{
    public static IReadOnlyList<OutlineSnapshot> Capture(IEnumerable<OutlineItem> items)
        => [.. items.Select(Capture)];

    public static bool Matches(IReadOnlyList<OutlineSnapshot> snapshot, IList<OutlineItem> items)
        => snapshot.Count == items.Count && snapshot.Select((item, index) => item.Matches(items[index])).All(value => value);

    private static OutlineSnapshot Capture(OutlineItem item) => new(
        item.Title,
        item.Target is not null,
        item.Target?.Anchor,
        item.Target?.PageIndex,
        item.Target?.Fit ?? default,
        item.Target is null ? [] : [.. item.Target.FitArguments],
        item.Color,
        item.Bold,
        item.Italic,
        item.Collapsed,
        Capture(item.Children));

    private bool Matches(OutlineItem item)
        => Title == item.Title
            && HasTarget == (item.Target is not null)
            && Anchor == item.Target?.Anchor
            && PageIndex == item.Target?.PageIndex
            && Fit == (item.Target?.Fit ?? default)
            && FitArguments.SequenceEqual(item.Target?.FitArguments ?? [])
            && Color == item.Color
            && Bold == item.Bold
            && Italic == item.Italic
            && Collapsed == item.Collapsed
            && Matches(Children, item.Children);
}

internal sealed record PageLabelSnapshot(int StartPage, PageLabelStyle? Style, string? Prefix, int Start)
{
    public static IReadOnlyList<PageLabelSnapshot> Capture(IEnumerable<PageLabel> labels)
        => [.. labels.Select(label => new PageLabelSnapshot(label.StartPage, label.Style, label.Prefix, label.Start))];

    public static bool Matches(IReadOnlyList<PageLabelSnapshot> snapshot, IList<PageLabel> labels)
        => snapshot.Count == labels.Count
            && snapshot.Select((item, index) => item == new PageLabelSnapshot(
                labels[index].StartPage, labels[index].Style, labels[index].Prefix, labels[index].Start)).All(value => value);
}
