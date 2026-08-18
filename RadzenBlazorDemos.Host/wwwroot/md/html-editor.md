# HtmlEditor: Default Tools

The Blazor HTML Editor is a rich text (WYSIWYG) editor with a full toolbar of formatting tools, image and link support, and HTML output.

Keywords: html, editor, rich, text, wysiwyg

> API reference: [RadzenHtmlEditor API](https://blazor.radzen.com/api/htmleditor.md)

## Examples

## Blazor HtmlEditor

The Blazor HTML Editor is a rich text (WYSIWYG) editor with a full toolbar of formatting tools, image and link support, and HTML output.

### Get and set the value

As all Radzen Blazor input components the HtmlEditor has a `Value` property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenHtmlEditor @bind-Value=@htmlValue style="height: 450px;" Input=@OnInput Change=@OnChange Paste=@OnPaste UploadComplete=@OnUploadComplete Execute=@OnExecute UploadUrl="api/upload/image" />

<EventConsole @ref=@console />

@code {
    string htmlValue = @"<h2 style=""text-align:center"">Accelerated, smarter, and cost-effective Blazor development</h2>
    <h3 style=""text-align:center"">Radzen Blazor Studio provides tons of productivity gains for Blazor developers</h3>
    <div style=""text-align:center"">
        <img alt=""Radzen Blazor Studio"" src=""images/radzen-blazor-studio-dark.png"" width=""300"">
    </div>
    <h4 style=""text-align:center"">Get started today. Radzen Blazor Studio is free to use.</h4>
    <div style=""text-align:center"">
        <a href=""https://www.radzen.com/blazor-studio/download"" target=""_blank"" title=""Get Radzen Blazor Studio for Windows, Mac or Linux"">Download Now</a>
    </div>";

    EventConsole console;

    void OnPaste(HtmlEditorPasteEventArgs args)
    {
        console.Log($"Paste: {args.Html}");
    }

    void OnChange(string html)
    {
        console.Log($"Change: {html}");
    }

    void OnInput(string html)
    {
        console.Log($"Input: {html}");
    }

    void OnExecute(HtmlEditorExecuteEventArgs args)
    {
        console.Log($"Execute: {args.CommandName}");
    }

    void OnUploadComplete(UploadCompleteEventArgs args)
    {
        console.Log($"Upload complete: {args.RawResponse}");
    }
}
```


### All tools

The Radzen HtmlEditor supports the following tools:
RadzenHtmlEditorUndo - allows the user to undo the last action (result of other tool, typing or pasting). RadzenHtmlEditorRedo - allows the user to redo the last undone action. RadzenHtmlEditorSeparator - displays a vertical separator used to delimit group of similar tools. RadzenHtmlEditorBold - toggles the bold style of the selected text. RadzenHtmlEditorItalic - toggles the italic style of the selected text. RadzenHtmlEditorUnderline - toggles the underline style of the selected text. RadzenHtmlEditorStrikeThrough - toggles the strikethrough style of the selected text. RadzenHtmlEditorAlignLeft - toggles left text alignment. RadzenHtmlEditorAlignCenter - toggles center text alignment. RadzenHtmlEditorAlignRight - toggles right text alignment. RadzenHtmlEditorJustify - toggles justified text alignment. RadzenHtmlEditorIndent - indents the selected text. RadzenHtmlEditorOutdent - outdents the selected text. RadzenHtmlEditorUnorderedList - inserts unordered (bullet) list. RadzenHtmlEditorOrderedList - inserts ordered (numbered) list. RadzenHtmlEditorColor - sets the foreground color of the selected text. RadzenHtmlEditorBackground - sets the background color of the selected text. RadzenHtmlEditorRemoveFormat - removes the visual styling of the selected text. RadzenHtmlEditorSubscript - converts the selected text to subscript. RadzenHtmlEditorSource - edit the HTML source as text. RadzenHtmlEditorSuperscript - converts the selected text to superscript RadzenHtmlEditorLink - inserts a hyperlink. RadzenHtmlEditorUnlink - removes a hyperlink. RadzenHtmlEditorImage - allows the user to insert an image by either uploading a file or selecting a URL. Requires File upload to be implemented and the UploadUrl property of the HtmlEditor to be set. RadzenHtmlEditorTable - inserts and edits HTML tables including rows, columns, merge/split, styling, copy/paste, resizing and context menu actions. RadzenHtmlEditorFontName - set the font of the selected text. RadzenHtmlEditorFontSize - set the font size of the selected text. RadzenHtmlEditorFormatBlock - allows the user to format the selected text as heading or paragraph. RadzenHtmlEditorCustomTool - allows the developer to implement a custom tool.

### Custom set of tools (text-editing only)

Here is how to specify a custom set of tools.

```razor
<RadzenHtmlEditor @bind-Value=@value style="height: 500px;" UploadUrl="api/upload/image">
    <RadzenHtmlEditorUndo />
    <RadzenHtmlEditorRedo />
    <RadzenHtmlEditorSeparator />
    <RadzenHtmlEditorBold />
    <RadzenHtmlEditorItalic />
    <RadzenHtmlEditorUnderline />
    <RadzenHtmlEditorStrikeThrough />
    <RadzenHtmlEditorSeparator />
    <RadzenHtmlEditorColor />
    <RadzenHtmlEditorBackground />
    <RadzenHtmlEditorRemoveFormat />
</RadzenHtmlEditor>

@code {
    string value = @"<h1>Hello World!</h1>";
}
```


### Upload images

You can insert images by pasting them or using the RadzenHtmlEditorImage tool. By default images are inserted as base64 encoded strings. To upload images to the server you need to implement a file upload endpoint and set the `UploadUrl` property of the RadzenHtmlEditor component: `&lt;RadzenHtmlEditor @-Value=@ UploadUrl="api/upload/image" /&gt;`. For a sample implementation check the `UploadController.cs` tab.

```razor
<RadzenHtmlEditor @bind-Value=@htmlValue UploadUrl="api/upload/image" style="height: 300px;"  />

@code {
    string htmlValue = "<h1>Hello World!</h1>";
}
```


### Focus

Programmatically set focus to the HTML editor using the `FocusAsync()` method.

```razor
<RadzenButton style="margin-bottom: 1rem" Click=@OnClick>Focus</RadzenButton>
<RadzenHtmlEditor @ref=@htmlEditor @bind-Value=@htmlValue style="height: 300px;" UploadUrl="api/upload/image" />
@code {
    RadzenHtmlEditor htmlEditor;

    string htmlValue = "<h1>Hello World!</h1>";

    async Task OnClick()
    {
        await htmlEditor.FocusAsync();
    }
}
```


### Table editor

The built-in table editor supports inserting tables, adding and removing rows or columns, merge and split operations, keyboard navigation, copy and paste for cell ranges, column resizing and a property panel for cell styling.

```razor
<RadzenAlert AlertStyle="AlertStyle.Info" Variant="Variant.Flat" class="rz-mb-4">
    Try Shift+Click to select a rectangular cell range, use the context menu for copy/paste, and drag the right cell edge to resize a whole column.
</RadzenAlert>

<RadzenHtmlEditor @bind-Value=@value Style="height: 420px;">
</RadzenHtmlEditor>

@code {
    string value = @"<p>Use the built-in table tools to insert and edit tables.</p>
<table style='width:100%; border-collapse:collapse' border='1'>
  <thead>
    <tr>
      <th style='width:180px'>Feature</th>
      <th>Status</th>
      <th>Notes</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Selection</td>
      <td>Ready</td>
      <td>Use Shift+Click for ranges.</td>
    </tr>
    <tr>
      <td>Copy / Paste</td>
      <td>Ready</td>
      <td>Works on rectangular cell ranges.</td>
    </tr>
    <tr>
      <td>Resize</td>
      <td>Ready</td>
      <td>Drag the right cell edge to resize the column.</td>
    </tr>
  </tbody>
</table>";
}
```
