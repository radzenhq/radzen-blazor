# Search & Redact

Extract text from PDF files in Blazor and C#, search with hit geometry to highlight matches, and redact content so the removed text is gone from the file.

Keywords: document, processing, pdf, extract, text, search, find, highlight, redact, redaction, remove

## Examples

## PDF Search & Redact

Extract text from PDF files, find matches with their exact position on the page, and redact content so it is removed from the file - not hidden behind a black box.

### Extract text

Read the text of a document or a single page with `ExtractText`, or get each run with its bounds using `ExtractPositionedText`.

### Find & highlight

`FindText` returns every match with its page and quadrilaterals - draw highlight annotations over the hits to build search-and-mark tooling.

### Redact

`RedactText` removes the matched content from the page itself. Extracting the text again afterwards proves the removed words are gone from the file.
