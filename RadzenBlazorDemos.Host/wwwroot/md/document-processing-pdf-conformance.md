# Conformance & Accessibility

Produce archival PDF/A and accessible, tagged PDF/UA documents in Blazor and C#. Conformance is validated at save time with actionable error messages.

Keywords: document, processing, pdf, pdfa, pdfua, conformance, archival, accessibility, accessible, tagged, structure, validation

## Examples

## PDF Conformance & Accessibility

Produce archival PDF/A and accessible, tagged PDF/UA documents. Conformance is enforced when the document is saved - an invalid combination fails immediately with a message that names the rule and the fix.

### PDF/A

Target PDF/A-2, PDF/A-3, or PDF/A-4 in their B, A, E, and F variants by setting `Conformance`. Fonts embed, an sRGB output intent is included, and level-specific rules are applied automatically.

### PDF/UA

Set `Accessibility` to PDF/UA-1 to emit a full logical structure tree from headings, lists, tables, and links, with document language, title, and alternate text for images.

### Validation

Try to save an invalid combination - encryption under PDF/A, a missing title under PDF/UA, attachments where the level forbids them - and the save fails with the exact rule and the suggested fix instead of producing a non-conformant file.
