using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// Emits embedded files on save: one compressed EmbeddedFile stream and /Filespec
// per attachment, wired into the catalog /Names /EmbeddedFiles name tree and the
// /AF associated-files array.
internal sealed class AttachmentWriter(Document document)
{
    public void WriteAttachments(DocumentWriter writer, DictionaryObject catalog)
    {
        var filespecs = new SortedDictionary<string, ReferenceObject>(StringComparer.Ordinal);
        var af = new ArrayObject();

        foreach (var attachment in document.Attachments)
        {
            var file = FlateFilter.EncodeStream(attachment.Data);
            file.Dictionary["Type"] = new NameObject("EmbeddedFile");
            file.Dictionary["Subtype"] = new NameObject(attachment.MimeType);
            file.Dictionary["Params"] = new DictionaryObject { ["Size"] = new NumberObject(attachment.Data.Length) };

            var filespec = new DictionaryObject
            {
                ["Type"] = new NameObject("Filespec"),
                ["F"] = new StringObject(attachment.Name),
                ["UF"] = new StringObject(attachment.Name),
                ["AFRelationship"] = new NameObject(attachment.Relationship.ToString()),
                ["EF"] = new DictionaryObject { ["F"] = writer.Add(file) },
            };

            var reference = writer.Add(filespec);
            filespecs[attachment.Name] = reference;
            af.Add(reference);
        }

        var names = new ArrayObject();
        foreach (var (name, reference) in filespecs)
        {
            names.Add(new StringObject(name));
            names.Add(reference);
        }

        catalog["Names"] = new DictionaryObject
        {
            ["EmbeddedFiles"] = writer.Add(new DictionaryObject { ["Names"] = names }),
        };
        catalog["AF"] = af;
    }
}
