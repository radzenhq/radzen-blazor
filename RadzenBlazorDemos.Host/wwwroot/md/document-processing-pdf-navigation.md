# Navigation

Add a table of contents, bookmarks, hyperlinks, and internal anchors to PDF documents in Blazor and C#. Configure page labels and viewer preferences.

Keywords: document, processing, pdf, toc, table, contents, bookmark, outline, link, hyperlink, anchor, page, label, viewer, preferences

## Examples

## PDF Navigation

Help readers find their way: a table of contents with page numbers, a bookmark tree in the viewer sidebar, links within and out of the document, and page labels.

### Table of contents

Add a table of contents whose entries link to anchors and show the resolved page number with a dot leader.

### Bookmarks

Build the outline tree shown in the viewer sidebar - nested items with bold, italic, and colored titles targeting pages or anchors.

### Links

Turn any inline content into a web link with `Inline.Link` or an internal jump with `Inline.LinkToAnchor`.

### Page labels

Number front matter i, ii, iii and the body 1, 2, 3 - the viewer's page indicator follows the labels.

### Viewer preferences

Control how the document opens: initial page mode and layout, window fitting, and whether the viewer shows the document title.
