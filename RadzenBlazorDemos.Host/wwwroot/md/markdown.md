# Markdown

Render Markdown content as HTML in Blazor with RadzenMarkdown - auto-linked headings and support for embedded Blazor components.

Keywords: markdown, text, content, render

> API reference: [RadzenMarkdown API](https://blazor.radzen.com/api/markdown.md)

## Examples

## Blazor Markdown

The Blazor Markdown component (RadzenMarkdown) renders Markdown content as HTML, with auto-linked headings and support for embedding Blazor components inside the markdown.

```razor
<RadzenCard Variant="Variant.Outlined">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" Wrap="FlexWrap.Wrap">
        <RadzenDropDown @bind-Value=@autoLinkHeadingDepth Data=@autoLinkHeadings TextProperty=@nameof(AutoLinkDepth.Text) ValueProperty=@nameof(AutoLinkDepth.Value) Name="autoLinkHeadings" />
        <RadzenLabel Text="Auto link heading depth" Component="autoLinkHeadings" />
    </RadzenStack>
</RadzenCard>
<RadzenMarkdown class="rz-p-0 rz-p-md-12" AutoLinkHeadingDepth=@autoLinkHeadingDepth>
### RadzenMarkdown

**RadzenMarkdown** allows you to render Markdown content in your Blazor applications.

#### Supported markdown syntax
1. Everything from the [basic syntax](https://www.markdownguide.org/basic-syntax/)
1. [Tables](https://www.markdownguide.org/extended-syntax/#tables)
1. [Fenced code blocks](https://www.markdownguide.org/extended-syntax/#fenced-code-blocks)
1. [Emoji shortcodes](https://www.webfx.com/tools/emoji-cheat-sheet/) e.g. `:smile:` :smile:, `:heart:` :heart:, `:+1:` :+1:

#### Features
Use markdown content right in your Blazor components - no need to create separate files. Type it directly in your .razor file:

```razor
@($@"<RadzenMarkdown>
# Hello, Blazor :wave:!
This is a **bold** text.
</RadzenMarkdown>")
```
</RadzenMarkdown>
@code {
    class AutoLinkDepth
    {
        public string Text { get; set; }
        public int Value { get; set; }
    }

    List<AutoLinkDepth> autoLinkHeadings = new List<AutoLinkDepth>()
    {
        new AutoLinkDepth() { Text = "None", Value = 0 },
        new AutoLinkDepth() { Text = "1", Value = 1 },
        new AutoLinkDepth() { Text = "2", Value = 2 },
        new AutoLinkDepth() { Text = "3", Value = 3 },
        new AutoLinkDepth() { Text = "4", Value = 4 },
        new AutoLinkDepth() { Text = "5", Value = 5 },
        new AutoLinkDepth() { Text = "6", Value = 6 }
    };
    int autoLinkHeadingDepth = 4;
}
```


### Get and set the text

Use the `Text` property to get or set the markdown content of RadzenMarkdown.

```razor
<RadzenStack Gap="3rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
    <RadzenLabel Component="html">Allow HTML</RadzenLabel>
    <RadzenSwitch @bind-Value=@allowHtml />
</RadzenStack>
<RadzenRow class="rz-pt-6" Gap="3rem" RowGap="1rem">
    <RadzenColumn SizeMD=6>
        <RadzenTextArea Value=@markdown @oninput=@OnInput Style="width: 100%; height: 400px" />
    </RadzenColumn>
    <RadzenColumn SizeMD=6>
        <RadzenMarkdown Text=@markdown AllowHtml=@allowHtml />
    </RadzenColumn>
</RadzenRow>
@code {
    bool allowHtml = true;
    string markdown = @"# Hello, Blazor :wave:!
- Try the **RadzenMarkdown** component.
- Update this text";

    void OnInput(ChangeEventArgs args)
    {
        markdown = args.Value.ToString();
    }
}
```


### Markdown with Blazor components inside

Embed Blazor components within Markdown content for interactive documentation and rich content.

```razor
<RadzenMarkdown class="rz-p-0 rz-p-md-12">
### You can use arbitrary Blazor components within the markdown content.

| Blazor component | Description |
| --- | --- |
| `RadzenButton` | <RadzenButton Text="Focus RadzenDatePicker" Click=@FocusDatePicker />|
| `RadzenDatePicker` | <RadzenDatePicker @ref=@datePicker @bind-Value=@date /> |
| `button` | <button type="button" @onclick=@FocusInput>Focus input</button>|
| `input` | <input @ref=@input /> |
| Text `@@date`| @date |
</RadzenMarkdown>
@code {
    DateTime date = DateTime.Today;
    RadzenDatePicker<DateTime> datePicker;
    ElementReference input;

    void FocusDatePicker()
    {
        datePicker.FocusAsync();
    }

    void FocusInput()
    {
        input.FocusAsync();
    }
}
```
