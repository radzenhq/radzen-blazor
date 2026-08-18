# Fonts & Unicode

Use the built-in PDF fonts or embed and subset TrueType fonts in Blazor and C#. Configure fallback chains and render Cyrillic, Greek, Chinese, Japanese, and Korean text.

Keywords: document, processing, pdf, font, truetype, embed, subset, fallback, kerning, unicode, cyrillic, greek, chinese, japanese, korean, cjk

## Examples

## PDF Fonts & Unicode

Use the built-in PDF fonts or embed your own TrueType fonts - subset automatically so documents stay small - and render international text that copies and pastes correctly.

### Built-in fonts

The fourteen standard PDF fonts - Helvetica, Times, Courier, Symbol, and ZapfDingbats families - work without embedding anything, with full metrics and kerning.

### Embedding & subsetting

Register a TrueType or OpenType font with `Document.Fonts.Register`. Only the glyphs the document uses are embedded, and text still extracts and searches correctly in any viewer.

### Fallback

Configure a fallback chain with `Document.Fonts.SetFallback` so characters missing from the primary font render from the next font that covers them. When no italic face is registered, an italic style is synthesized.

### Unicode

Latin, Cyrillic, Greek, Chinese, Japanese, and Korean text renders with correct glyph mapping, so copying text out of the viewer produces the original characters. Complex scripts such as Arabic, Hebrew, Indic, and Thai are not yet supported.
