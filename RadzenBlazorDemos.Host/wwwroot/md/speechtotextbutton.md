# SpeechToTextButton

The Blazor Speech to Text Button captures voice input using the browser's speech recognition and writes the transcript to your field.

Keywords: button, speech, voice, dictation, form

> API reference: [RadzenSpeechToTextButton API](https://blazor.radzen.com/api/speechtotextbutton.md)

## Examples

## Blazor SpeechToTextButton

The Blazor Speech to Text Button captures voice input using the browser's speech recognition and writes the transcript to your field.

```razor
<RadzenRow>
    <RadzenColumn Size="6">
        <RadzenSpeechToTextButton Change="@(args => OnSpeechCaptured(args, false, "Button1"))" />
        <EventConsole @ref=@console />
    </RadzenColumn>
    <RadzenColumn Size="6">
        <RadzenSpeechToTextButton Change="@(args => OnSpeechCaptured(args, true, "Button2"))" />
        <RadzenTextArea @bind-Value=@value Change=@(args => OnTextAreaChange(args, "TextArea")) Style="width: 100%" class="rz-mt-4" aria-label="text area" />
    </RadzenColumn>
</RadzenRow>

@code {
    string value;
    EventConsole console;

    void OnTextAreaChange(string value, string name)
    {
        console.Log($"{name} value changed to {value}");
    }

    void OnSpeechCaptured(string speechValue, bool updateTextArea, string name)
    {
        console.Log($"Speech Captured from {name}: {speechValue}");

        if (updateTextArea)
        {
            value += speechValue;
        }
    }
}
```
