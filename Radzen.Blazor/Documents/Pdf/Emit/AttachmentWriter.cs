using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class AttachmentWriter(Document document)
{
    public void WriteAttachments(DocumentWriter writer, DictionaryObject catalog)
    {
        var filespecs = new SortedDictionary<string, ReferenceObject>(StringComparer.Ordinal);
        var af = new ArrayObject();

        foreach (var attachment in document.Attachments)
        {
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
                ["F"] = StringObject.FromText(attachment.Name),
                ["UF"] = StringObject.FromText(attachment.Name),
                ["AFRelationship"] = new NameObject(attachment.Relationship.ToString()),
                ["EF"] = new DictionaryObject { ["F"] = fileReference, ["UF"] = fileReference },
            };

            if (!string.IsNullOrEmpty(attachment.Description))
            {
                filespec["Desc"] = StringObject.FromText(attachment.Description);
            }

            var reference = writer.Add(filespec);
            filespecs[attachment.Name] = reference;
            af.Add(reference);
        }

        NameTree.AddCategory(writer, catalog, "EmbeddedFiles",
            filespecs.Select(entry => (entry.Key, (DocumentObject)entry.Value)));
        catalog["AF"] = af;
    }
}
