# AIChat

The Blazor AI Chat component provides a conversational, streaming chat interface for AI assistants.

Keywords: chat, ai, conversation, message, streaming

> API reference: [RadzenAIChat API](https://blazor.radzen.com/api/aichat.md)

## Examples

## Blazor AIChat

The Blazor AI Chat component provides a conversational, streaming chat interface for building AI assistants and chatbot experiences.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Chat Controls" />
            <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                <RadzenButton Text="Add Welcome" 
                              Icon="add"
                              Click="@(() => basicChat?.AddMessage("Hello! How can I help you today?", false))"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Add User Message" 
                              Icon="person_add"
                              Click="@(() => basicChat?.AddMessage("This is a test message from the user.", true))"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Clear Chat" 
                              Icon="delete_history"
                              ButtonStyle="ButtonStyle.Light"
                              Click="@(() => basicChat?.ClearChat())"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Send Test" 
                              Icon="send" 
                              ButtonStyle="ButtonStyle.Primary"
                              Click="@(() => basicChat?.SendMessage("What is Blazor?"))"
                              Variant="Variant.Flat" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenAIChat @ref="basicChat" 
                Title="AI Assistant" 
                Placeholder="Ask me anything..."
                Style="height: 500px;"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                ResponseReceived="@OnResponseReceived"
                ChatCleared="@OnChatCleared"
                />
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 230px;" />

@code {
    RadzenAIChat basicChat;
    EventConsole console;

    void OnMessageAdded(Radzen.Blazor.ChatMessage message)
    {
        console.Log($"Message added: {(message.IsUser ? "User" : "Assistant")} - {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...", 
                   message.IsUser ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(string message)
    {
        console.Log($"Message sent: {message}", AlertStyle.Info);
    }

    void OnResponseReceived(string response)
    {
        console.Log($"AI Response received: {response.Substring(0, Math.Min(50, response.Length))}...", AlertStyle.Success);
    }

    void OnChatCleared()
    {
        console.Log("Chat cleared", AlertStyle.Warning);
    }
}
```


### Custom styling

Customize the appearance of the AIChat component using `Style`, `ShowClearButton`, `Disabled`, and `ReadOnly` properties.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap" Gap="1rem">
            <RadzenStack Orientation="Orientation.Vertical" Gap="8px">
                <RadzenLabel Text="Show Clear Button" />
                <RadzenSwitch @bind-Value="showClearButton" />
            </RadzenStack>
            
            <RadzenStack Orientation="Orientation.Vertical" Gap="8px">
                <RadzenLabel Text="Disabled" />
                <RadzenSwitch @bind-Value="isDisabled" />
            </RadzenStack>
            
            <RadzenStack Orientation="Orientation.Vertical" Gap="8px">
                <RadzenLabel Text="Read Only" />
                <RadzenSwitch @bind-Value="isReadOnly" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenAIChat @ref="customChat" 
                Title="Custom Assistant" 
                Placeholder="Type your message here..."
                EmptyMessage="Welcome! Start chatting with me."
                ShowClearButton="@showClearButton"
                Disabled="@isDisabled"
                ReadOnly="@isReadOnly"
                Style="height: 400px; border: 2px solid var(--rz-primary);"
                />
</RadzenStack>

@code {
    RadzenAIChat customChat;
    
    private bool showClearButton = true;
    private bool isDisabled = false;
    private bool isReadOnly = false;
}
```


### Compact aichat

Create a more compact chat interface suitable for smaller spaces or sidebar implementations.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenAIChat @ref="compactChat"
                Placeholder="Quick message..."
                ShowClearButton="false"
                Style="height: 300px; max-width: 100%;"
                />
</RadzenStack>

@code {
    RadzenAIChat compactChat;
}
```


### Events and interactions

Handle chat events like `MessageAdded`, `MessageSent`, `ResponseReceived`, and `ChatCleared` to integrate with your application logic.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenAIChat @ref="eventsChat" 
                Title="Event Demo" 
                Placeholder="Type a message to see events..."
                Style="height: 400px;"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                ResponseReceived="@OnResponseReceived"
                ChatCleared="@OnChatCleared"
                />
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 200px;" />

@code {
    RadzenAIChat eventsChat;
    EventConsole console;

    void OnMessageAdded(Radzen.Blazor.ChatMessage message)
    {
        console.Log($"Message added: {(message.IsUser ? "User" : "Assistant")} - {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...", 
                   message.IsUser ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(string message)
    {
        console.Log($"Message sent: {message}", AlertStyle.Info);
    }

    void OnResponseReceived(string response)
    {
        console.Log($"AI Response received: {response.Substring(0, Math.Min(50, response.Length))}...", AlertStyle.Success);
    }

    void OnChatCleared()
    {
        console.Log("Chat cleared", AlertStyle.Warning);
    }
}
```


### Date separator and timestamp format

Use `TimestampFormat` to control how the time is rendered next to each message, and `ShowDateSeparator` (with `DateSeparatorFormat`) to insert a divider between messages whenever the day changes — useful when restoring long conversation history via `LoadMessages`.

```razor
<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="8">
        <RadzenAIChat @ref="aiChat"
                      Title="AI Assistant"
                      Placeholder="Type your message..."
                      TimestampFormat="@timestampFormat"
                      ShowDateSeparator="@showDateSeparator"
                      DateSeparatorFormat="@dateSeparatorFormat"
                      Style="height: 500px;" />
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="4">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Date &amp; Time Options</RadzenText>

                <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                    <RadzenCheckBox @bind-Value="@showDateSeparator" TValue="bool" Name="showDateSeparator" />
                    <RadzenLabel Text="Show date separator between days" Component="showDateSeparator" />
                </RadzenStack>

                <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                    <RadzenLabel Text="Timestamp Format" />
                    <RadzenDropDown @bind-Value="@timestampFormat" Data="@timestampFormats" TextProperty="Text" ValueProperty="Value" />
                </RadzenStack>

                <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                    <RadzenLabel Text="Date Separator Format" />
                    <RadzenDropDown @bind-Value="@dateSeparatorFormat" Disabled="@(!showDateSeparator)" Data="@dateSeparatorFormats" TextProperty="Text" ValueProperty="Value" />
                </RadzenStack>

                <RadzenButton Text="Load sample history"
                              Icon="history"
                              ButtonStyle="ButtonStyle.Light"
                              Variant="Variant.Flat"
                              Click="@LoadSampleHistory" />
            </RadzenStack>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    RadzenAIChat aiChat;

    private bool showDateSeparator = true;
    private string timestampFormat = "HH:mm";
    private string dateSeparatorFormat = "D";

    private readonly IEnumerable<dynamic> timestampFormats = new[]
    {
        new { Text = "Time only (HH:mm)", Value = "HH:mm" },
        new { Text = "Short date + time (g)", Value = "g" },
        new { Text = "Day + time (ddd HH:mm)", Value = "ddd HH:mm" },
        new { Text = "Full date + time (f)", Value = "f" }
    };

    private readonly IEnumerable<dynamic> dateSeparatorFormats = new[]
    {
        new { Text = "Long date (D)", Value = "D" },
        new { Text = "Short date (d)", Value = "d" },
        new { Text = "Month + day (MMMM d)", Value = "MMMM d" },
        new { Text = "ISO (yyyy-MM-dd)", Value = "yyyy-MM-dd" }
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadSampleHistory();
        }
    }

    private async Task LoadSampleHistory()
    {
        if (aiChat == null)
        {
            return;
        }

        var today = DateTime.Today;

        await aiChat.LoadMessages(new[]
        {
            new ChatMessage { Content = "What's a good way to structure a Blazor component library?", IsUser = true, Timestamp = today.AddDays(-2).AddHours(10).AddMinutes(5) },
            new ChatMessage { Content = "Separate markup (`.razor`) from logic (`.razor.cs`), expose parameters via `[Parameter]`, and keep styling in SCSS partials.", IsUser = false, Timestamp = today.AddDays(-2).AddHours(10).AddMinutes(6) },
            new ChatMessage { Content = "How do I handle long-running operations?", IsUser = true, Timestamp = today.AddDays(-1).AddHours(14).AddMinutes(22) },
            new ChatMessage { Content = "Use `async/await` and call `StateHasChanged` through `InvokeAsync` when updating state from background threads.", IsUser = false, Timestamp = today.AddDays(-1).AddHours(14).AddMinutes(23) },
            new ChatMessage { Content = "Thanks — can you summarize the points so far?", IsUser = true, Timestamp = today.AddHours(9).AddMinutes(10) },
            new ChatMessage { Content = "Sure: split markup and code, use `[Parameter]` for inputs, keep styles modular, and marshal UI updates through `InvokeAsync`.", IsUser = false, Timestamp = today.AddHours(9).AddMinutes(11) }
        });
    }
}
```


### AI Chat with Memory

The AIChat component supports conversation memory that remembers previous questions and maintains context across multiple interactions. Use `SessionId` to maintain conversation state and `SessionIdChanged` to track session changes.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenAIChat Title="AI Assistant with Memory" 
                Placeholder="Ask me anything... I'll remember our conversation!"
                SessionId="@currentSessionId"
                SessionIdChanged="@OnSessionIdChanged"
                MessageAdded="@OnMessageAdded"
                ResponseReceived="@OnResponseReceived"
                ShowClearButton="true"
                class="rz-h-100" />
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenRow>
            <RadzenColumn Size="12" SizeLG="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Memory Statistics</RadzenText>
                    <RadzenRow>
                        <RadzenColumn Size="6">
                            <RadzenText TextStyle="TextStyle.H4" TagName="TagName.P">@activeSessionsCount</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Active Sessions</RadzenText>
                        </RadzenColumn>
                        <RadzenColumn Size="6">
                            <RadzenText TextStyle="TextStyle.H4" TagName="TagName.P">@totalMessagesCount</RadzenText>
                            <RadzenText TextStyle="TextStyle.Caption">Total Messages</RadzenText>
                        </RadzenColumn>
                    </RadzenRow>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Session Management</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical">
                        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap">
                            <RadzenButton Text="New Session" Icon="add" ButtonStyle="ButtonStyle.Primary" Click="@OnNewSession" Variant="Variant.Flat" />
                            <RadzenButton Text="Load Session" Icon="folder_open" ButtonStyle="ButtonStyle.Base" Click="@OnLoadSession" Variant="Variant.Flat" />
                            <RadzenButton Text="Clear Session" Icon="clear_all" ButtonStyle="ButtonStyle.Danger" Click="@OnClearSession" Variant="Variant.Flat" />
                        </RadzenStack>
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenLabel Style="white-space: nowrap;">Current Session ID:</RadzenLabel>
                            <RadzenTextBox @bind-Value="@currentSessionId" ReadOnly="true" Style="flex: 1; min-width: 360px;" />
                        </RadzenStack>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Session History</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical">
                        <RadzenDropDown TValue="string" Data="@sessionIds" @bind-Value="@selectedSessionId" Placeholder="Select a session to load" Style="width: 100%;" />
                        <RadzenButton Text="Load Selected Session" Icon="folder_open" ButtonStyle="ButtonStyle.Base" Variant="Variant.Flat"
                                    Click="@OnLoadSelectedSession" Style="width: 100%;" Disabled="@(string.IsNullOrEmpty(selectedSessionId))" />
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
    <RadzenColumn Size="12">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Memory Features</RadzenText>
            <RadzenRow>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Conversation Memory</RadzenText>
                    <ul>
                        <li>The AI remembers all previous messages in the conversation</li>
                        <li>Context is maintained across multiple questions</li>
                        <li>You can ask follow-up questions naturally</li>
                        <li>The AI can reference previous parts of the conversation</li>
                    </ul>
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Session Management</RadzenText>
                    <ul>
                        <li>Create new sessions for different conversations</li>
                        <li>Switch between different conversation contexts</li>
                        <li>Clear sessions to start fresh</li>
                        <li>Session data persists during the application session</li>
                    </ul>
                </RadzenColumn>
            </RadzenRow>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    private string currentSessionId;
    private string selectedSessionId;
    private List<string> sessionIds = new();
    private int activeSessionsCount = 0;
    private int totalMessagesCount = 0;

    [Inject]
    private IAIChatService ChatService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        // Create a new session by default
        currentSessionId = Guid.NewGuid().ToString();
        
        // Update session statistics
        await UpdateSessionStatistics();
    }

    private async Task OnSessionIdChanged(string sessionId)
    {
        currentSessionId = sessionId;
        await UpdateSessionStatistics();
    }

    private async Task OnMessageAdded(Radzen.Blazor.ChatMessage message)
    {
        // Update statistics when a message is added
        await UpdateSessionStatistics();
    }

    private async Task OnResponseReceived(string response)
    {
        // Update statistics when a response is received
        await UpdateSessionStatistics();
    }

    private async Task OnNewSession()
    {
        currentSessionId = Guid.NewGuid().ToString();
        await UpdateSessionStatistics();
    }

    private async Task OnLoadSession()
    {
        // This would typically open a dialog to select a session
        // For demo purposes, we'll just create a new session
        currentSessionId = Guid.NewGuid().ToString();
        await UpdateSessionStatistics();
    }

    private async Task OnClearSession()
    {
        if (!string.IsNullOrEmpty(currentSessionId))
        {
            ChatService.ClearSession(currentSessionId);
            await UpdateSessionStatistics();
        }
    }

    private async Task OnLoadSelectedSession()
    {
        if (!string.IsNullOrEmpty(selectedSessionId))
        {
            currentSessionId = selectedSessionId;
            await UpdateSessionStatistics();
        }
    }

    private async Task UpdateSessionStatistics()
    {
        var sessions = ChatService.GetActiveSessions().ToList();
        activeSessionsCount = sessions.Count;
        totalMessagesCount = sessions.Sum(s => s.Messages.Count);
        
        // Update session IDs list
        sessionIds = sessions.Select(s => s.Id).ToList();
        
        await InvokeAsync(StateHasChanged);
    }
}
```


### Runtime AI Configuration

Customize AI behavior at runtime by configuring the `Model`, `Temperature`, `MaxTokens`, `SystemPrompt`, `Endpoint`, `Proxy`, `ApiKey`, and `ApiKeyHeader` parameters. These parameters can be set on the component or passed to the `SendMessage` method to override settings per message, enabling dynamic switching between different AI providers and models.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenAIChat @ref="modelChat"
                Title="AI Assistant with Dynamic Configuration" 
                Placeholder="Ask me anything..."
                MessageAdded="@OnMessageAdded"
                ResponseReceived="@OnResponseReceived"
                ShowClearButton="true"
                Model="@selectedModel"
                class="rz-h-100" />
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenRow>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">AI Configuration</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenLabel Style="white-space: nowrap;">Model:</RadzenLabel>
                            <RadzenDropDown TValue="string" 
                                          Data="@availableModels" 
                                          @bind-Value="@selectedModel"
                                          Change="@(args => OnClearChat())"
                                          Placeholder="Select a model" 
                                          Style="flex: 1; min-width: 200px;" />
                        </RadzenStack>
                        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                            <RadzenButton Text="Send Test Message" 
                                        Icon="send" 
                                        ButtonStyle="ButtonStyle.Primary"
                                        Click="@OnSendTestMessage"
                                        Variant="Variant.Flat" />
                            <RadzenButton Text="Clear Chat" 
                                        Icon="clear_all"
                                        ButtonStyle="ButtonStyle.Danger"
                                        Click="@OnClearChat"
                                        Variant="Variant.Flat" />
                            <RadzenButton Text="Reset Defaults" 
                                        Icon="refresh"
                                        ButtonStyle="ButtonStyle.Light"
                                        Click="@OnResetDefaults"
                                        Variant="Variant.Flat" />
                        </RadzenStack>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Current Configuration</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical" Gap="0.25rem">
                        <RadzenText TextStyle="TextStyle.Body2">
                            <strong>Model:</strong> @selectedModel
                        </RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2">
                            <strong>Total Messages:</strong> @messagesCount
                        </RadzenText>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Available Models</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical">
                        @foreach (var model in availableModels)
                        {
                            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-2 rz-border-bottom">
                                <RadzenIcon Icon="smart_toy" />
                                <RadzenText TextStyle="TextStyle.Body2">@model</RadzenText>
                                @if (model == selectedModel)
                                {
                                    <RadzenBadge Text="Active" BadgeStyle="BadgeStyle.Success" />
                                }
                            </RadzenStack>
                        }
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
</RadzenRow>

@code {
    private RadzenAIChat modelChat;

    // Default values
    private const string DefaultModel = "@cf/meta/llama-4-scout-17b-16e-instruct";

    // Configuration parameters
    private string selectedModel = DefaultModel;
    
    private List<string> availableModels = new()
    {
        "@cf/meta/llama-4-scout-17b-16e-instruct",
        "@cf/qwen/qwen1.5-14b-chat-awq",
        "@cf/mistral/mistral-7b-instruct-v0.1"
    };

    private int messagesCount = 0;

    private void OnMessageAdded(Radzen.Blazor.ChatMessage message)
    {
        messagesCount++;
        StateHasChanged();
    }

    private void OnResponseReceived(string response)
    {
        // Response received from AI
        StateHasChanged();
    }

    private async Task OnSendTestMessage()
    {
        if (modelChat != null && !string.IsNullOrEmpty(selectedModel))
        {
            await modelChat.SendMessage("What are your model name and parameters?");
        }
    }

    private async Task OnClearChat()
    {
        if (modelChat != null)
        {
            await modelChat.ClearChat();
            messagesCount = 0;
            StateHasChanged();
        }
    }

    private async Task OnResetDefaults()
    {
        selectedModel = DefaultModel;
        await OnClearChat();
        StateHasChanged();
    }
}
```
