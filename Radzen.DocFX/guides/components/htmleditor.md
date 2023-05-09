# HtmlEditor component
This article demonstrates how to use RadzenHtmlEditor.

## Get and set the value
As all Radzen Blazor input components the HtmlEditor has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenHtmlEditor @bind-Value=@htmlValue />
@code {
  string htmlValue = "<h1>Hello World!!!</h1>";
}
```

## Tools
The HtmlEditor provides various tools for content editing - bold, italic, color, various text formatting etc.
By default all tools are enabled. Here is how to specify a custom set of tools:

```
<RadzenHtmlEditor @bind-Value=@value>
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
```

### All tools
The Radzen HtmlEditor supports the following tools:

- RadzenHtmlEditorUndo - allows the user to undo the last action (result of other tool, typing or pasting).
- RadzenHtmlEditorRedo - allows the use to redo the last undone action.
- RadzenHtmlEditorSeparator - displays a vertical separator used to delimit group of similar tools.
- RadzenHtmlEditorBold - toggles the bold style of the selected text.
- RadzenHtmlEditorItalic - toggles the italic style of the selected text.
- RadzenHtmlEditorUnderline - toggles the underline style of the selected text.
- RadzenHtmlEditorStrikeThrough - toggles the strikethrough style of the selected text.
- RadzenHtmlEditorAlignLeft - toggles left text alignment.
- RadzenHtmlEditorAlignCenter - toggles center text alignment.
- RadzenHtmlEditorAlignRight - toggles right text alignment.
- RadzenHtmlEditorJustify - toggles justified text alignment.
- RadzenHtmlEditorIndent - indents the selected text.
- RadzenHtmlEditorOutdent - outdents the selected text.
- RadzenHtmlEditorUnorderedList - inserts unordered (bullet) list.
- RadzenHtmlEditorOrderedList - inserts ordered (numbered) list.
- RadzenHtmlEditorColor - sets the foreground color of the selected text.
- RadzenHtmlEditorBackground - sets the background color of the selected text.
- RadzenHtmlEditorRemoveFormat - removes the visual styling of the selected text.
- RadzenHtmlEditorSource - edit the HTML source as text.
- RadzenHtmlEditorSubscript - converts the selected text to subscript.
- RadzenHtmlEditorSuperscript - converts the selected text to superscript
- RadzenHtmlEditorLink - inserts a hyperlink.
- RadzenHtmlEditorUnlink - removes a hyperlink.
- RadzenHtmlEditorImage - allows the user to insert an image by either uploading a file or selecting a URL. Requires File upload to be implemented and the `UploadUrl` property of the HtmlEditor to be set.
- RadzenHtmlEditorFontName - set the font of the selected text.
- RadzenHtmlEditorFontSize - set the font size of the selected text.
- RadzenHtmlEditorFormatBlock - allows the user to format the selected text as heading or paragraph.
- RadzenHtmlEditorCustomTool - allows the developer to implement a [custom tool](#custom-tools).

### Default tools

By default RadzenHtmlEditor uses these tools:

```
<RadzenHtmlEditorUndo />
<RadzenHtmlEditorRedo />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorBold />
<RadzenHtmlEditorItalic />
<RadzenHtmlEditorUnderline />
<RadzenHtmlEditorStrikeThrough />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorAlignLeft />
<RadzenHtmlEditorAlignCenter />
<RadzenHtmlEditorAlignRight />
<RadzenHtmlEditorJustify />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorIndent />
<RadzenHtmlEditorOutdent />
<RadzenHtmlEditorUnorderedList />
<RadzenHtmlEditorOrderedList />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorColor />
<RadzenHtmlEditorBackground />
<RadzenHtmlEditorRemoveFormat />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorSubscript />
<RadzenHtmlEditorSuperscript />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorLink />
<RadzenHtmlEditorUnlink />
<RadzenHtmlEditorImage />
<RadzenHtmlEditorFontName />
<RadzenHtmlEditorFontSize />
<RadzenHtmlEditorFormatBlock />
<RadzenHtmlEditorSeparator />
<RadzenHtmlEditorSource />
```
### Custom tools
RadzenHtmlEditor allows the developer to create custom tools via the `RadzenHtmlEditorCustomTool` tag.

In its basic form you create a button and handle the `Execute` event of the HtmlEditor to implement the command.

```
<RadzenHtmlEditor Execute=@OnExecute>
    <RadzenHtmlEditorCustomTool CommandName="InsertToday" Icon="today" Title="Insert today" />
</RadzenHtmlEditor>
@code {
    async Task OnExecute(HtmlEditorExecuteEventArgs args)
    {
        if (args.CommandName == "InsertToday")
        {
            var date = DateTime.Now;
            await args.Editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, $"<strong>{date.ToLongDateString()}</strong>");
        }
    }
}
```

You can also specify custom UI via the `Template` of the RadzenHtmlEditorCustomTool.

```
<RadzenHtmlEditor>
    <RadzenHtmlEditorCustomTool>
        <Template Context="editor">
            <RadzenDatePicker Change=@(args => OnDateChange(args, editor)) TValue="DateTime" />
        </Template>
    </RadzenHtmlEditorCustomTool>
</RadzenHtmlEditor>
@code {
  async Task OnDateChange(DateTime? date, RadzenHtmlEditor editor)
  {
      if (date != null)
      {
          await editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, $"<strong>{date.Value.ToLongDateString()}</strong>");
      }
  }
}
```
## Upload files
RadzenHtmlEditor requires file upload support to be implemented for uploading and pasting images. Here is a minimal implementation
that stores the uploaded files in the `wwwroot` directory of the application and uses GUID for the file names to avoid naming conflicts.

# [Page](#tab/page)
```
<RadzenHtmlEditor UploadUrl="upload/image" />
```
# [Controller](#tab/controller)
```
using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;

namespace YourApplicationNamespace.Controllers
{
    public partial class UploadController : Controller
    {
        private readonly IWebHostEnvironment environment;

        public UploadController(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        [HttpPost("upload/image")]
        public IActionResult Image(IFormFile file)
        {
            try
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                using (var stream = new FileStream(Path.Combine(environment.WebRootPath, fileName), FileMode.Create))
                {
                    // Save the file
                    file.CopyTo(stream);

                    // Return the URL of the file
                    var url = Url.Content($"~/{fileName}");

                    return Ok(new { Url = url });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
```
