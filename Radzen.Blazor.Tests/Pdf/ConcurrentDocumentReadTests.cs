#nullable enable
using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class ConcurrentDocumentReadTests
{
    private const int FieldCount = 2000;
    private const int Workers = 64;
    private const int Rounds = 8;

    private static int Widget(int index) => 5 + (index * 3);

    private static string Expected(string name)
        => "value" + name["field".Length..];

    private static byte[] FormSource()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        var references = string.Join(
            " ",
            Enumerable.Range(0, FieldCount).Select(i => Widget(i).ToString(CultureInfo.InvariantCulture) + " 0 R"));

        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Fields [" + references + "] >>\nendobj\n");

        for (var i = 0; i < FieldCount; i++)
        {
            var widget = Widget(i);
            pdf.Object(widget, widget + " 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T "
                + (widget + 1) + " 0 R /V " + (widget + 2) + " 0 R /Rect [0 0 10 10] >>\nendobj\n");
            pdf.Object(widget + 1, (widget + 1) + " 0 obj\n(field" + i + ")\nendobj\n");
            pdf.Object(widget + 2, (widget + 2) + " 0 obj\n(value" + i + ")\nendobj\n");
        }

        var count = Widget(FieldCount - 1) + 3;
        var xref = pdf.Position;
        pdf.Append("xref\n0 " + count + "\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < count; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size " + count + " /Root 1 0 R >>\n");
        pdf.Append("startxref\n" + xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] CyclicLengthSource()
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 4 0 R >>\nstream\n(hello)\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n");
        pdf.Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n"
            + xref.ToString(CultureInfo.InvariantCulture) + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void RunOnWorkers(Action<int> body)
    {
        using var start = new Barrier(Workers);
        var threads = Enumerable.Range(0, Workers).Select(worker => new Thread(() =>
        {
            start.SignalAndWait();
            body(worker);
        })).ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }
    }

    [Fact]
    public void SequentialFieldValueReadsReturnEachFieldsOwnValue()
    {
        var document = Document.LoadFromStream(new MemoryStream(FormSource()));
        var fields = document.AcroForm!.Fields;

        Assert.Equal(FieldCount, fields.Count);
        foreach (var field in fields)
        {
            Assert.Equal(Expected(field.Name), field.Value);
        }
    }

    [Fact]
    public void ConcurrentFieldValueReadsReturnEachFieldsOwnValue()
    {
        var source = FormSource();
        var failures = new ConcurrentBag<string>();

        for (var round = 0; round < Rounds; round++)
        {
            var document = Document.LoadFromStream(new MemoryStream(source));
            var fields = document.AcroForm!.Fields;
            var current = round;

            RunOnWorkers(worker =>
            {
                for (var step = 0; step < FieldCount; step++)
                {
                    var i = (step + (worker * 31)) % FieldCount;
                    try
                    {
                        var field = fields[i];
                        var name = field.Name;
                        var value = field.Value;
                        if (!string.Equals(value, Expected(name), StringComparison.Ordinal))
                        {
                            failures.Add($"round {current} field {i} named '{name}' read '{value}', expected '{Expected(name)}'");
                        }
                    }
                    catch (Exception exception)
                    {
                        failures.Add($"round {current} field {i} threw {exception.GetType().Name}: {exception.Message}");
                    }
                }
            });
        }

        Assert.True(
            failures.IsEmpty,
            $"{failures.Count} of {Rounds * Workers * FieldCount} concurrent reads failed:\n"
                + string.Join("\n", failures.Distinct().Take(20)));
    }

    [Fact]
    public void ConcurrentLoadsOfCyclicLengthStillReportTheCycle()
    {
        var source = CyclicLengthSource();
        var failures = new ConcurrentBag<string>();

        RunOnWorkers(_ =>
        {
            for (var round = 0; round < Rounds; round++)
            {
                var exception = Record.Exception(() =>
                {
                    var document = Document.LoadFromStream(new MemoryStream(source));
                    var content = document.Pages[0].GetContent();
                    if (content is not null && !content.AsSpan().SequenceEqual("(hello)"u8))
                    {
                        failures.Add("recovered content was not the endstream-scanned payload");
                    }
                });

                if (exception is not (null or DocumentParseException))
                {
                    failures.Add($"cyclic /Length threw {exception}");
                }
            }
        });

        Assert.True(failures.IsEmpty, string.Join("\n", failures.Distinct().Take(20)));
    }
}
