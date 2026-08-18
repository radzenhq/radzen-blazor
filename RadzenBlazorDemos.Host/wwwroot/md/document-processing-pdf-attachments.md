# Attachments & e-Invoicing

Embed file attachments in PDF documents in Blazor and C# and produce Factur-X and ZUGFeRD hybrid e-invoices - a PDF/A-3 invoice with machine-readable XML inside.

Keywords: document, processing, pdf, attachment, embed, file, facturx, factur-x, zugferd, einvoice, e-invoicing, en16931, invoice, xml

## Examples

## PDF Attachments & e-Invoicing

Embed files inside a PDF, and produce Factur-X / ZUGFeRD hybrid e-invoices - a human-readable PDF/A-3 invoice carrying its machine-readable XML counterpart.

### Attachments

Attach any file with `PortableDocument.Attachments.Add`, including its MIME type and relationship, and read attachments back from loaded documents.

### Factur-X

Produce a Factur-X / ZUGFeRD e-invoice: a PDF/A-3 document with the invoice XML attached as an alternative representation and the profile declared in the document metadata. The XML content comes from your invoicing system - the library guarantees the container: conformance, the attachment relationship, and the metadata declaration.
