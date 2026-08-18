# HtmlEditor: Custom Tools

Add your own buttons to the Blazor HTML Editor toolbar with RadzenHtmlEditorCustomTool.

Keywords: html, editor, rich, text, tool, custom

> API reference: [RadzenHtmlEditor API](https://blazor.radzen.com/api/htmleditor.md)

## Examples

## Radzen Blazor HtmlEditor custom tools

Add your own buttons to the Blazor HTML Editor toolbar with the `RadzenHtmlEditorCustomTool` tag.

### Custom command on Execute event

In its basic form you create a button and handle the `Execute` event of the HtmlEditor to implement the command.

```razor
<RadzenHtmlEditor style="height: 200px;" Execute=@OnExecute>
    <RadzenHtmlEditorCustomTool CommandName="InsertToday" Icon="today" Title="Insert today" />
</RadzenHtmlEditor>

@code {
    async Task OnExecute(HtmlEditorExecuteEventArgs args)
    {
        if (args.CommandName == "InsertToday")
        {
            await InsertDate(args.Editor, DateTime.Now);
        }
    }

    async Task InsertDate(RadzenHtmlEditor editor, DateTime date)
    {
        await editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, $"<strong>{date.ToLongDateString()}</strong>");
    }
}
```


### Custom tool with template

You can also specify custom UI via the `Template` of the RadzenHtmlEditorCustomTool.

```razor
<RadzenHtmlEditor style="height: 200px;">
    <RadzenHtmlEditorCustomTool>
        <Template Context="editor">
            <RadzenDatePicker Change=@(args => OnDateChange(args, editor)) TValue="DateTime" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select date" }})" />
        </Template>
    </RadzenHtmlEditorCustomTool>
</RadzenHtmlEditor>

@code {
    async Task OnDateChange(DateTime? date, RadzenHtmlEditor editor)
    {
        if (date != null)
        {
            await InsertDate(editor, date.Value);
        }
    }

    async Task InsertDate(RadzenHtmlEditor editor, DateTime date)
    {
        await editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, $"<strong>{date.ToLongDateString()}</strong>");
    }
}
```


### Custom dialog

Create custom editor tools with dialog interfaces for specialized editing features.

```razor
<RadzenHtmlEditor style="height: 200px;" Execute=@OnExecute>
    <RadzenHtmlEditorCustomTool CommandName="InsertImageFromList" Icon="attach_file" />
</RadzenHtmlEditor>

@code {
    async Task OnExecute(HtmlEditorExecuteEventArgs args)
    {
        if (args.CommandName == "InsertImageFromList")
        {
            await InsertImageFromList(args.Editor);
        }
    }

    async Task InsertImageFromList(RadzenHtmlEditor editor)
    {
        await editor.SaveSelectionAsync();

        var result = await DialogService.OpenAsync<HtmlEditorCustomDialog>("Select image file"); 

        await editor.RestoreSelectionAsync();

        if (result != null)
        {
            await editor.ExecuteCommandAsync(HtmlEditorCommands.InsertHtml, $"<img alt=\"Selected image file preview\" style=\"max-width: 100%\" src=\"{result}\">");
        }
    }
}
```
