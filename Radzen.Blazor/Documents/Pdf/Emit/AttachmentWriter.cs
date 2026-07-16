using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

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
            // The /EmbeddedFiles name tree keys by name and would keep only the last of a
            // duplicate, while /AF lists both filespecs - fail loud rather than emit a file
            // unreachable from the attachments panel whose winner depends on insertion order.
            if (filespecs.ContainsKey(attachment.Name))
            {
                throw new InvalidOperationException(
                    $"Duplicate attachment name '{attachment.Name}'; embedded file names must be unique.");
            }

            var file = FlateFilter.EncodeStream(attachment.Data);
            file.Dictionary["Type"] = new NameObject("EmbeddedFile");
            file.Dictionary["Subtype"] = new NameObject(attachment.MimeType);
            file.Dictionary["Params"] = new DictionaryObject
            {
                ["Size"] = new NumberObject(attachment.Data.Length),
                ["ModDate"] = new StringObject(DocumentSaver.PdfDate(attachment.ModificationDate.ToUniversalTime())),
            };

            var fileReference = writer.Add(file);
            var filespec = new DictionaryObject
            {
                ["Type"] = new NameObject("Filespec"),
                ["F"] = new StringObject(attachment.Name),
                ["UF"] = new StringObject(attachment.Name),
                ["AFRelationship"] = new NameObject(attachment.Relationship.ToString()),
                ["EF"] = new DictionaryObject { ["F"] = fileReference, ["UF"] = fileReference },
            };

            if (!string.IsNullOrEmpty(attachment.Description))
            {
                filespec["Desc"] = new StringObject(attachment.Description);
            }

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

        var nameTree = catalog.TryGetValue("Names", out var existing) && existing is DictionaryObject dictionary
            ? dictionary
            : new DictionaryObject();
        nameTree["EmbeddedFiles"] = writer.Add(new DictionaryObject { ["Names"] = names });
        catalog["Names"] = nameTree;
        catalog["AF"] = af;
    }
}
