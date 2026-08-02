using System.Collections.Generic;
using System;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Scene;

namespace Radzen.Documents.Layout;

internal sealed class FormFieldRules : ISceneVisitor
{
    private readonly HashSet<(string Name, string Value)> exports = [];
    private readonly Dictionary<string, string> selection = new(StringComparer.Ordinal);

    public void Enforce(LaidOutPage page) => SceneWalk.Page(page, this);

    void ISceneVisitor.Line(in LaidOutLine line, in SceneFrame frame)
    {
        foreach (var run in line.Line.ShapedRuns)
        {
            if (run.Paint.FormField is { Kind: FormFieldKind.Radio } field)
            {
                Radio(field);
            }
        }
    }

    private void Radio(in FormFieldPaint field)
    {
        if (!exports.Add((field.Name, field.Value)))
        {
            throw new InvalidOperationException(
                $"Radio group '{field.Name}' has two buttons exporting '{field.Value}'; "
                + "give each button of a group a distinct Value.");
        }

        if (!field.Chosen)
        {
            return;
        }

        if (selection.TryGetValue(field.Name, out var already))
        {
            throw new InvalidOperationException(
                $"Radio group '{field.Name}' selects both '{already}' and '{field.Value}'; "
                + "only one button of a group may be selected.");
        }

        selection.Add(field.Name, field.Value);
    }
}
