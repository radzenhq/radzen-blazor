#nullable enable

using System;
using System.Collections.Generic;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed record MarkedContentTag(string Tag, IReadOnlyDictionary<string, string> Properties);

internal static class ContentOperationTestHelpers
{
    public static List<string> Operators(byte[] content) => ContentStreamTokenizer.Operators(content);

    public static List<MarkedContentTag> Tags(byte[] content)
    {
        var tags = new List<MarkedContentTag>();

        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator is not ("BDC" or "BMC") || operation.Operands.Count == 0)
            {
                continue;
            }

            tags.Add(new MarkedContentTag(operation.Operands[0].Text, Properties(operation)));
        }

        return tags;
    }

    public static List<string> ResourceNames(byte[] content, string @operator)
    {
        var names = new List<string>();

        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator != @operator)
            {
                continue;
            }

            foreach (var operand in operation.Operands)
            {
                if (operand.Kind == ContentTokenKind.Name)
                {
                    names.Add(operand.Text);
                }
            }
        }

        return names;
    }

    public static bool HasSequence(byte[] content, params string[] sequence)
        => IndexOfSequence(Operators(content), sequence) >= 0;

    public static int IndexOfSequence(List<string> operators, params string[] sequence)
    {
        for (var start = 0; start + sequence.Length <= operators.Count; start++)
        {
            var matched = true;
            for (var offset = 0; offset < sequence.Length; offset++)
            {
                if (operators[start + offset] != sequence[offset])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return start;
            }
        }

        return -1;
    }

    public static int Count(byte[] content, string @operator)
        => Operators(content).FindAll(current => current == @operator).Count;

    private static Dictionary<string, string> Properties(ContentOperation operation)
    {
        var properties = new Dictionary<string, string>(StringComparer.Ordinal);

        for (var index = 1; index + 1 < operation.Operands.Count; index++)
        {
            if (operation.Operands[index].Kind != ContentTokenKind.Name)
            {
                continue;
            }

            var value = operation.Operands[index + 1];
            if (value.Kind is ContentTokenKind.Name or ContentTokenKind.Number)
            {
                properties[operation.Operands[index].Text] = value.Text;
                index++;
            }
        }

        return properties;
    }
}
