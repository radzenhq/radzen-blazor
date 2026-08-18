# Edit Existing PDFs

Load existing PDF files in Blazor and C#. Merge documents, split and extract pages, reorder and rotate them, and apply watermarks to loaded pages.

Keywords: document, processing, pdf, load, edit, merge, split, extract, append, reorder, rotate, watermark, pages

## Examples

## Edit Existing PDFs

Load PDF files - including ones produced by other tools - and reshape them: merge, split, extract, reorder, rotate, and watermark.

### Load

Open a document with `PortableDocument.LoadFromStream`. The reader handles cross-reference tables and streams, object streams, and repairs common structural damage, with configurable limits that keep hostile files from exhausting resources.

### Merge & split

Append whole documents, import selected pages, or split one document into several - fonts, images, and form fields carry over and deduplicate.

### Reorder & rotate

Move, remove, and rotate pages through the `Pages` collection, and adjust page boxes to crop.

### Watermark

Apply a text or image watermark to the pages of a loaded document with `AddWatermark` - the same API that watermarks generated documents.
