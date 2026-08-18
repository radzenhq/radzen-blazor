# Forms

Create PDF forms with text, checkbox, radio, and dropdown fields in Blazor and C#. Fill existing forms, read the values back, and flatten fields into page content.

Keywords: document, processing, pdf, form, acroform, field, text, checkbox, radio, dropdown, fill, read, flatten

## Examples

## PDF Forms

The full form round-trip: author fields, fill them, read the values back, and flatten the result into permanent page content.

### Author fields

Place text inputs, checkboxes, radio groups, and dropdowns inline in the document flow, or position them absolutely with field definitions.

### Fill

Load a PDF with a form and fill it from code: `FillField`, `CheckField`, `SelectOption`, and `SelectRadioOption` on `PortableDocument.AcroForm`.

### Read values

Enumerate `AcroForm.Fields` to read names, types, and values from a filled form - the other half of the round-trip.

### Flatten

`Flatten()` bakes fields and annotations into page content so the values become permanent and no longer editable.
