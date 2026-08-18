# Chat

The Blazor Chat component supports multi-participant conversations with distinct user identities and real-time messaging.

Keywords: chat, conversation, message, users, team, group

> API reference: [RadzenChat API](https://blazor.radzen.com/api/chat.md)

## Examples

## Blazor Chat

The Blazor Chat component supports multi-participant conversations with distinct user identities and real-time messaging - for team chats, group discussions, and messaging apps.

```razor
@inherits ComponentBase

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Chat Controls" />
            <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                <RadzenButton Text="Add User Message" 
                              Icon="person_add"
                              Click="@(async () => await basicChat?.AddMessage("Hello everyone! How's the project going?", "user1"))"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Add User Message" 
                              Icon="message"
                              Click="@(async () => await basicChat?.AddMessage("Great progress! The new features are working perfectly.", "user2"))"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Clear Chat" 
                              Icon="delete_history"
                              ButtonStyle="ButtonStyle.Light"
                              Click="@(() => basicChat?.ClearChat())"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Send Message" 
                              Icon="send" 
                              ButtonStyle="ButtonStyle.Primary"
                              Click="@(() => basicChat?.SendMessage("Thanks for the update!"))"
                              Variant="Variant.Flat" />

                <RadzenButton Text="Simulate Jane typing"
                              Icon="keyboard"
                              Click="@SimulateJaneTyping"
                              Variant="Variant.Flat" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenChat @ref="basicChat" 
                Title="Team Chat" 
                CurrentUserId="user1"
                Users="@users"
                Messages="@messages"
                MessagesChanged="@OnMessagesChanged"
                Placeholder="Type your message..."
                Style="height: 500px;"
                ShowTypingIndicator="true"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                ChatCleared="@OnChatCleared"
                TypingChanged="@OnTypingChanged"
                />
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 230px;" />

@code {
    RadzenChat basicChat;
    EventConsole console;
    
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" }
        });

        // Add some sample messages
        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Welcome to the team chat! 👋", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "Thanks John! Looking forward to working together.", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Same here! Let's make this project amazing! 🚀", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) }
        });
    }

    void OnMessageAdded(ChatMessage message)
    {
        var participant = users.FirstOrDefault(p => p.Id == message.UserId);
        var participantName = participant?.Name ?? "Unknown";
        console.Log($"Message added: {participantName} - {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...", 
                   message.UserId == "user1" ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(ChatMessage message)
    {
        console.Log($"Message sent: {message.Content}", AlertStyle.Info);
    }

    void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }

    void OnChatCleared()
    {
        console.Log("Chat cleared", AlertStyle.Warning);
    }

    async Task SimulateJaneTyping()
    {
        if (basicChat == null)
        {
            return;
        }

        await basicChat.SetUserTyping("user2", true);
        await Task.Delay(1500);
        await basicChat.SetUserTyping("user2", false);
    }

    void OnTypingChanged(ChatTypingEventArgs args)
    {
        // In a real app you would broadcast this via SignalR to other clients.
        console.Log($"TypingChanged: {args.UserId} => {args.IsTyping}", AlertStyle.Info);
    }
}
```


### Multi-participant support

The Chat component supports multiple users with distinct identities, avatars, and online status indicators. Use `Users` to manage the chat users and `CurrentUserId` to identify the current user.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenChat Title="Multi-User Chat" 
                    CurrentUserId="@currentUserId"
                    Users="@users" 
                    Messages="@messages"
                    MessagesChanged="@OnMessagesChanged"
                    MessageAdded="@OnMessageAdded"
                    UserAdded="@OnUserAdded"
                    UserRemoved="@OnUserRemoved"
                    ShowUsers="true"
                    ShowUserNames="true"
                    MaxVisibleUsers="4"
                    class="rz-h-100" />
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenRow>
            <RadzenColumn Size="12" SizeLG="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">User Management</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                            <RadzenButton Text="Add User" Icon="person_add" ButtonStyle="ButtonStyle.Primary" Click="@OnAddUser" Variant="Variant.Flat" />
                            <RadzenButton Text="Remove Last" Icon="person_remove" ButtonStyle="ButtonStyle.Danger" Click="@OnRemoveUser" Variant="Variant.Flat" />
                            <RadzenButton Text="Add Sample Messages" Icon="message" ButtonStyle="ButtonStyle.Success" Click="@OnAddSampleMessages" Variant="Variant.Flat" />
                        </RadzenStack>
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenLabel Style="white-space: nowrap;">Current User:</RadzenLabel>
                            <RadzenDropDown TValue="string" Data="@users" TextProperty="Name" ValueProperty="Id" @bind-Value="@currentUserId" Placeholder="Select current user" Style="flex: 1; min-width: 200px;" />
                        </RadzenStack>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">User List</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical">
                        @foreach (var participant in users)
                        {
                            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-2 rz-border-bottom">
                                <RadzenStack AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" class="rz-chat-message-avatar" style="width: 2rem; height: 2rem;">
                                    <span class="rz-chat-avatar-initials">@participant.GetInitials()</span>
                                </RadzenStack>
                                <RadzenText TextStyle="TextStyle.Body2">@participant.Name</RadzenText>
                                <RadzenBadge Text="@(participant.IsOnline ? "Online" : "Offline")" 
                                           BadgeStyle="@(participant.IsOnline ? BadgeStyle.Success : BadgeStyle.Light)" />
                                @if (participant.Id == currentUserId)
                                {
                                    <RadzenBadge Text="You" BadgeStyle="BadgeStyle.Primary" />
                                }
                            </RadzenStack>
                        }
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
    <RadzenColumn Size="12">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Multi-User Features</RadzenText>
            <RadzenRow>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">User Management</RadzenText>
                    <ul>
                        <li>Add and remove users dynamically</li>
                        <li>Each participant has a unique identity and avatar</li>
                        <li>Online/offline status tracking</li>
                        <li>User list displayed in chat header</li>
                    </ul>
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Message Features</RadzenText>
                    <ul>
                        <li>Messages are aligned based on sender (user vs users)</li>
                        <li>User names shown above messages</li>
                        <li>Avatar initials generated from participant names</li>
                        <li>Message timestamps for all users</li>
                    </ul>
                </RadzenColumn>
            </RadzenRow>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    private string currentUserId = "user1";
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();
    private Random random = new();

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2", IsOnline = true },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c", IsOnline = true },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00", IsOnline = false },
            new ChatUser { Id = "user4", Name = "Alice Brown", Color = "#7b1fa2", IsOnline = true }
        });

        OnAddSampleMessages();
    }

    private void OnAddUser()
    {
        var names = new[] { "Emma Davis", "Michael Garcia", "Sarah Martinez", "David Rodriguez", "Lisa Anderson" };
        var colors = new[] { "#1976d2", "#388e3c", "#f57c00", "#7b1fa2", "#d32f2f", "#0288d1", "#689f38", "#ffa000" };
        
        var newUser = new ChatUser
        {
            Id = Guid.NewGuid().ToString(),
            Name = names[random.Next(names.Length)],
            Color = colors[random.Next(colors.Length)],
            IsOnline = random.Next(2) == 1
        };
        
        users.Add(newUser);
        StateHasChanged();
    }

    private void OnRemoveUser()
    {
        if (users.Count > 1)
        {
            users.RemoveAt(users.Count - 1);
            StateHasChanged();
        }
    }

    private void OnAddSampleMessages()
    {
        var sampleMessages = new[]
        {
            new ChatMessage { Content = "Hello team! 👋", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "Hi John! Ready for our sprint planning?", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Absolutely! I've prepared the user stories.", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) },
            new ChatMessage { Content = "Perfect! Let's get started. 🚀", UserId = "user4", Timestamp = DateTime.Now.AddMinutes(-27) }
        };
        
        messages.AddRange(sampleMessages);
        StateHasChanged();
    }

    private void OnMessageAdded(ChatMessage message)
    {
        // Message is already added to the messages list by the component
        StateHasChanged();
    }

    private void OnUserAdded(ChatUser participant)
    {
        // User is already added to the users list
        StateHasChanged();
    }

    private void OnUserRemoved(ChatUser participant)
    {
        // User is already removed from the users list
        StateHasChanged();
    }

    private void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }
}
```


### Customization options

Customize the appearance and behavior of the Chat component using various properties like `ShowUsers`, `ShowUserNames`, `MaxVisibleUsers`, and `ShowClearButton`.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenChat Title="Customizable Chat" 
                    CurrentUserId="@currentUserId"
                    Users="@users" 
                    Messages="@messages"
                    MessagesChanged="@OnMessagesChanged"
                    ShowUsers="@showUsers"
                    ShowUserNames="@showUserNames"
                    ShowClearButton="@showClearButton"
                    MaxVisibleUsers="@maxVisibleUsers"
                    Placeholder="@placeholder"
                    EmptyMessage="@emptyMessage"
                    class="rz-h-100" />
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenRow>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Display Options</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenCheckBox @bind-Value="@showUsers" TValue="bool" Name="showUsers" />
                            <RadzenLabel Text="Show Users in Header" Component="showUsers" />
                        </RadzenStack>
                        
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenCheckBox @bind-Value="@showUserNames" TValue="bool" Name="showUserNames" />
                            <RadzenLabel Text="Show User Names Above Messages" Component="showUserNames" />
                        </RadzenStack>
                        
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenCheckBox @bind-Value="@showClearButton" TValue="bool" Name="showClearButton" />
                            <RadzenLabel Text="Show Clear Button" Component="showClearButton" />
                        </RadzenStack>
                        
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap">
                            <RadzenLabel Text="Max Visible Users:" Style="white-space: nowrap;" />
                            <RadzenNumeric @bind-Value="@maxVisibleUsers" Min="1" Max="10" Style="width: 100px;" />
                        </RadzenStack>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
            <RadzenColumn Size="12">
                <RadzenCard Variant="Variant.Outlined">
                    <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Text Customization</RadzenText>
                    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem">
                        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                            <RadzenLabel Text="Placeholder Text" />
                            <RadzenTextBox @bind-Value="@placeholder" Placeholder="Enter placeholder text..." />
                        </RadzenStack>
                        
                        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
                            <RadzenLabel Text="Empty Message" />
                            <RadzenTextBox @bind-Value="@emptyMessage" Placeholder="Enter empty message text..." />
                        </RadzenStack>
                        
                        <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                            <RadzenButton Text="Reset to Defaults" 
                                        Icon="refresh" 
                                        ButtonStyle="ButtonStyle.Light" 
                                        Click="@OnResetDefaults" 
                                        Variant="Variant.Flat" />
                            <RadzenButton Text="Add Sample Messages" 
                                        Icon="message" 
                                        ButtonStyle="ButtonStyle.Success" 
                                        Click="@OnAddSampleMessages" 
                                        Variant="Variant.Flat" />
                        </RadzenStack>
                    </RadzenStack>
                </RadzenCard>
            </RadzenColumn>
        </RadzenRow>
    </RadzenColumn>
    <RadzenColumn Size="12">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Customization Features</RadzenText>
            <RadzenRow>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Display Controls</RadzenText>
                    <ul>
                        <li><code>ShowUsers</code> - Toggle participant avatars in header</li>
                        <li><code>ShowUserNames</code> - Show/hide names above messages</li>
                        <li><code>ShowClearButton</code> - Control clear chat button visibility</li>
                        <li><code>MaxVisibleUsers</code> - Limit header participant display</li>
                    </ul>
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Text Customization</RadzenText>
                    <ul>
                        <li><code>Placeholder</code> - Customize input field placeholder</li>
                        <li><code>EmptyMessage</code> - Set message when chat is empty</li>
                        <li><code>Title</code> - Set chat header title</li>
                        <li>All text properties support localization</li>
                    </ul>
                </RadzenColumn>
            </RadzenRow>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    private string currentUserId = "user1";
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();
    
    // Customization options
    private bool showUsers = true;
    private bool showUserNames = true;
    private bool showClearButton = true;
    private int maxVisibleUsers = 5;
    private string placeholder = "Type your message...";
    private string emptyMessage = "No messages yet. Start a conversation!";

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" },
            new ChatUser { Id = "user4", Name = "Alice Brown", Color = "#7b1fa2" },
            new ChatUser { Id = "user5", Name = "Charlie Wilson", Color = "#d32f2f" },
            new ChatUser { Id = "user6", Name = "Diana Prince", Color = "#0288d1" }
        });
    }

    private void OnResetDefaults()
    {
        showUsers = true;
        showUserNames = true;
        showClearButton = true;
        maxVisibleUsers = 5;
        placeholder = "Type your message...";
        emptyMessage = "No messages yet. Start a conversation!";
        StateHasChanged();
    }

    private void OnAddSampleMessages()
    {
        messages.Clear();
        var sampleMessages = new[]
        {
            new ChatMessage { Content = "Welcome to our customizable chat! 🎨", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "This is great! We can customize everything.", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "I love the **markdown** support and emoji! 😊", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) },
            new ChatMessage { Content = "The participant management is really smooth.", UserId = "user4", Timestamp = DateTime.Now.AddMinutes(-27) },
            new ChatMessage { Content = "Perfect for team collaboration! 👥", UserId = "user5", Timestamp = DateTime.Now.AddMinutes(-26) }
        };
        
        messages.AddRange(sampleMessages);
        StateHasChanged();
    }

    private void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }
}
```


### Events and interactions

Handle chat events like `MessageAdded`, `MessageSent`, `UserAdded`, and `ChatCleared` to integrate with your application logic and provide real-time updates.

```razor
@inherits ComponentBase

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Event Testing Controls" />
            <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="0.5rem">
                <RadzenButton Text="Send Message" 
                              Icon="send"
                              Click="@(() => eventsChat?.SendMessage("Testing message events!"))"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Add User" 
                              Icon="person_add"
                              Click="@OnAddUser"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Remove User" 
                              Icon="person_remove"
                              Click="@OnRemoveUser"
                              Variant="Variant.Flat" />
                
                <RadzenButton Text="Clear Chat" 
                              Icon="delete_history"
                              ButtonStyle="ButtonStyle.Light"
                              Click="@(() => eventsChat?.ClearChat())"
                              Variant="Variant.Flat" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenChat @ref="eventsChat" 
                Title="Events Demo" 
                CurrentUserId="@currentUserId"
                Users="@users"
                Messages="@messages"
                MessagesChanged="@OnMessagesChanged"
                Placeholder="Type a message to test events..."
                Style="height: 500px;"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                UserAdded="@OnUserAdded"
                UserRemoved="@OnUserRemoved"
                ChatCleared="@OnChatCleared"
                />
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 300px;" />

@code {
    RadzenChat eventsChat;
    EventConsole console;
    
    private string currentUserId = "user1";
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();
    private Random random = new();

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" }
        });

        // Add some sample messages
        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Welcome to the events demo! 🎉", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "This chat demonstrates all the available events.", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Try the buttons above to see events in action!", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) }
        });
    }

    private void OnAddUser()
    {
        var names = new[] { "Emma Davis", "Michael Garcia", "Sarah Martinez", "David Rodriguez", "Lisa Anderson" };
        var colors = new[] { "#1976d2", "#388e3c", "#f57c00", "#7b1fa2", "#d32f2f", "#0288d1", "#689f38", "#ffa000" };
        
        var newUser = new ChatUser
        {
            Id = Guid.NewGuid().ToString(),
            Name = names[random.Next(names.Length)],
            Color = colors[random.Next(colors.Length)]
        };
        
        users.Add(newUser);
        StateHasChanged();
    }

    private void OnRemoveUser()
    {
        if (users.Count > 1)
        {
            users.RemoveAt(users.Count - 1);
            StateHasChanged();
        }
    }

    void OnMessageAdded(ChatMessage message)
    {
        var participant = users.FirstOrDefault(p => p.Id == message.UserId);
        var participantName = participant?.Name ?? "Unknown";
        console.Log($"MessageAdded: {participantName} - {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...", 
                   message.UserId == currentUserId ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(ChatMessage message)
    {
        console.Log($"MessageSent: {message.Content}", AlertStyle.Info);
    }

    void OnUserAdded(ChatUser participant)
    {
        console.Log($"UserAdded: {participant.Name} (ID: {participant.Id})", AlertStyle.Success);
    }

    void OnUserRemoved(ChatUser participant)
    {
        console.Log($"UserRemoved: {participant.Name} (ID: {participant.Id})", AlertStyle.Warning);
    }

    void OnChatCleared()
    {
        console.Log("ChatCleared: All messages have been removed", AlertStyle.Warning);
    }

    void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }
}
```


### Date separator and timestamp format

Use `TimestampFormat` to control how the time is rendered next to each message, and `ShowDateSeparator` (with `DateSeparatorFormat`) to insert a divider between messages whenever the day changes — useful for chats that span multiple days.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="8">
        <RadzenChat CurrentUserId="user1"
                    Title="Project Chat"
                    Users="@users"
                    Messages="@messages"
                    MessagesChanged="@OnMessagesChanged"
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
            </RadzenStack>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();

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

    protected override void OnInitialized()
    {
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" }
        });

        var today = DateTime.Today;

        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Kicking off the sprint — who's handling the API changes?", UserId = "user1", Timestamp = today.AddDays(-2).AddHours(9).AddMinutes(15) },
            new ChatMessage { Content = "I've got the endpoints. Should have a PR up by end of day.", UserId = "user2", Timestamp = today.AddDays(-2).AddHours(9).AddMinutes(22) },
            new ChatMessage { Content = "PR is up: #482. Ready for review.", UserId = "user2", Timestamp = today.AddDays(-2).AddHours(17).AddMinutes(40) },
            new ChatMessage { Content = "Reviewed — left a couple of comments on the validation logic.", UserId = "user3", Timestamp = today.AddDays(-1).AddHours(8).AddMinutes(30) },
            new ChatMessage { Content = "Addressed them. Merged.", UserId = "user2", Timestamp = today.AddDays(-1).AddHours(10).AddMinutes(5) },
            new ChatMessage { Content = "Deploying to staging now.", UserId = "user1", Timestamp = today.AddHours(9).AddMinutes(0) },
            new ChatMessage { Content = "Looks good on my end. 🚀", UserId = "user3", Timestamp = today.AddHours(9).AddMinutes(48) }
        });
    }

    private void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }
}
```


### Compact chat

Create a more compact chat interface suitable for smaller spaces or sidebar implementations by adjusting the height and hiding optional elements.

```razor
@inherits ComponentBase

<RadzenRow Gap="1rem">
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Compact Chat</RadzenText>
            <RadzenChat Title="Team Chat" 
                        CurrentUserId="@currentUserId"
                        Users="@users" 
                        Messages="@messages"
                        MessagesChanged="@OnMessagesChanged"
                        ShowUsers="true"
                        ShowUserNames="false"
                        ShowClearButton="false"
                        MaxVisibleUsers="3"
                        Placeholder="Quick message..."
                        EmptyMessage="Start typing..."
                        Style="height: 300px;" />
        </RadzenCard>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeLG="6">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Minimal Chat</RadzenText>
            <RadzenChat Title="" 
                        CurrentUserId="@currentUserId"
                        Users="@users" 
                        Messages="@messages"
                        MessagesChanged="@OnMessagesChanged"
                        ShowUsers="false"
                        ShowUserNames="false"
                        ShowClearButton="false"
                        Placeholder="Type here..."
                        EmptyMessage=""
                        Style="height: 250px;" />
        </RadzenCard>
    </RadzenColumn>
    <RadzenColumn Size="12">
        <RadzenCard Variant="Variant.Outlined">
            <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P">Compact Chat Features</RadzenText>
            <RadzenRow>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Space-Saving Options</RadzenText>
                    <ul>
                        <li>Reduced height for sidebar implementations</li>
                        <li>Hide participant names to save vertical space</li>
                        <li>Limit visible users in header</li>
                        <li>Remove optional UI elements (clear button, title)</li>
                    </ul>
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="6">
                    <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P">Use Cases</RadzenText>
                    <ul>
                        <li>Sidebar chat panels</li>
                        <li>Mobile-responsive layouts</li>
                        <li>Embedded chat widgets</li>
                        <li>Minimal notification systems</li>
                    </ul>
                </RadzenColumn>
            </RadzenRow>
        </RadzenCard>
    </RadzenColumn>
</RadzenRow>

@code {
    private string currentUserId = "user1";
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob", Color = "#f57c00" },
            new ChatUser { Id = "user4", Name = "Alice", Color = "#7b1fa2" }
        });

        // Add some sample messages
        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Quick update: Project is on track! ✅", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "Great news! 🎉", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Thanks for the update", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) },
            new ChatMessage { Content = "Perfect timing for the demo", UserId = "user4", Timestamp = DateTime.Now.AddMinutes(-27) }
        });
    }

    private void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }
}
```


### RenderFragment as Title

Use a RenderFragment to display more complex content in the title of the chat component.

```razor
@inherits ComponentBase

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Title Content RenderFragment" />
        </RadzenStack>
    </RadzenCard>
    <RadzenChat @ref="basicChat"
                CurrentUserId="user1"
                Users="@users"
                Messages="@messages"
                MessagesChanged="@OnMessagesChanged"
                Placeholder="Type your message..."
                Style="height: 500px;"
                ShowTypingIndicator="true"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                ChatCleared="@OnChatCleared"
                TypingChanged="@OnTypingChanged">
        <TitleContent>
            <RadzenBadge>Chat Title Badge</RadzenBadge>
        </TitleContent>
    </RadzenChat>
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 230px;" />

@code {
    RadzenChat basicChat;
    EventConsole console;
    
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" }
        });

        // Add some sample messages
        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Welcome to the team chat! 👋", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "Thanks John! Looking forward to working together.", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Same here! Let's make this project amazing! 🚀", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) }
        });
    }

    void OnMessageAdded(ChatMessage message)
    {
        var participant = users.FirstOrDefault(p => p.Id == message.UserId);
        var participantName = participant?.Name ?? "Unknown";
        console.Log($"Message added: {participantName} - {message.Content.Substring(0, Math.Min(50, message.Content.Length))}...", 
                   message.UserId == "user1" ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(ChatMessage message)
    {
        console.Log($"Message sent: {message.Content}", AlertStyle.Info);
    }

    void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }

    void OnChatCleared()
    {
        console.Log("Chat cleared", AlertStyle.Warning);
    }

    async Task SimulateJaneTyping()
    {
        if (basicChat == null)
        {
            return;
        }

        await basicChat.SetUserTyping("user2", true);
        await Task.Delay(1500);
        await basicChat.SetUserTyping("user2", false);
    }

    void OnTypingChanged(ChatTypingEventArgs args)
    {
        // In a real app you would broadcast this via SignalR to other clients.
        console.Log($"TypingChanged: {args.UserId} => {args.IsTyping}", AlertStyle.Info);
    }
}
```


### Mention Users

Use a Mention Character to define on which character a callback should be triggered and display a search popup.

```razor
@inherits ComponentBase

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Mention Users in Chat" />
        </RadzenStack>
    </RadzenCard>
    <RadzenChat @ref="basicChat"
                CurrentUserId="user1"
                Users="@users"
                Messages="@messages"
                MessagesChanged="@OnMessagesChanged"
                Placeholder="@PlaceholderText"
                Style="height: 500px;"
                ShowTypingIndicator="true"
                MessageAdded="@OnMessageAdded"
                MessageSent="@OnMessageSent"
                ChatCleared="@OnChatCleared"
                TypingChanged="@OnTypingChanged"
                MentionCharacter="@MentionCharacter"
                MentionDisplaySize="5"
                MentionSearch="@LoadMentions"
                MentionUsers="@mentionUsers"
                MentionUsersCount="@mentionUsersCount"
                MentionItemTemplate="@MentionItemTemplate"
                MentionDisplayTemplate="@MentionDisplayTemplate">
        <TitleContent>
            <RadzenBadge>Chat with Mentions</RadzenBadge>
        </TitleContent>
    </RadzenChat>
</RadzenStack>

<EventConsole @ref="console" Style="min-height: 230px;" />

@code {
    RadzenChat basicChat;
    EventConsole console;
    
    private List<ChatUser> users = new();
    private List<ChatMessage> messages = new();
    private List<MentionUserContext> mentionUsers = new();
    private int? mentionUsersCount;
    private const char MentionCharacter = '@';
    private const string PlaceholderText = "Type your message... (try typing @ to mention someone)";

    protected override void OnInitialized()
    {
        // Initialize users
        users.AddRange(new[]
        {
            new ChatUser { Id = "user1", Name = "John Doe", Color = "#1976d2" },
            new ChatUser { Id = "user2", Name = "Jane Smith", Color = "#388e3c" },
            new ChatUser { Id = "user3", Name = "Bob Johnson", Color = "#f57c00" }
        });

        // Add some sample messages
        messages.AddRange(new[]
        {
            new ChatMessage { Content = "Welcome to the team chat! 👋", UserId = "user1", Timestamp = DateTime.Now.AddMinutes(-30) },
            new ChatMessage { Content = "Thanks John! Looking forward to working together.", UserId = "user2", Timestamp = DateTime.Now.AddMinutes(-29) },
            new ChatMessage { Content = "Same here! Let's make this project amazing! 🚀", UserId = "user3", Timestamp = DateTime.Now.AddMinutes(-28) }
        });
    }

    // Template for rendering individual mention search results
    private RenderFragment<MentionUserContext> MentionItemTemplate => (context) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "rz-mention-item");
        builder.AddAttribute(2, "style", "display: flex; align-items: center; gap: 0.5rem; padding: 0.5rem;");

        // User avatar/initials
        var user = users.FirstOrDefault(u => u.Id == context.UserId);
        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "style", $"width: 24px; height: 24px; border-radius: 50%; background: {user?.Color ?? "#ccc"}; display: flex; align-items: center; justify-content: center; color: white; font-size: 12px; font-weight: bold;");
        builder.AddContent(5, user?.GetInitials() ?? "?");
        builder.CloseElement();

        // User name
        builder.OpenElement(6, "span");
        builder.AddContent(7, context.UserName ?? "");
        builder.CloseElement();

        // Online indicator
        if (context.IsInChat)
        {
            builder.OpenElement(8, "span");
            builder.AddAttribute(9, "style", "margin-left: auto; width: 8px; height: 8px; border-radius: 50%; background: #4caf50;");
            builder.CloseElement();
        }

        builder.CloseElement();
    };

    // Template for rendering stored mentions in chat messages
    private RenderFragment<string> MentionDisplayTemplate => (userId) => builder =>
    {
        var mentionedUser = users.FirstOrDefault(u => u.Id == userId);
        
        builder.OpenElement(0, "span");
        builder.AddAttribute(1, "class", "rz-mention-badge");
        builder.AddAttribute(2, "style", $"background: {mentionedUser?.Color ?? "#ccc"}20; color: {mentionedUser?.Color ?? "#ccc"}; padding: 0.2rem 0.4rem; border-radius: 4px; font-weight: 500;");
        builder.AddAttribute(3, "title", mentionedUser?.Name ?? userId);
        builder.AddContent(4, $"@{mentionedUser?.Name ?? userId}");
        builder.CloseElement();
    };

    async Task LoadMentions(MentionSearchArgs args)
    {
        // Simulate a small delay for network request
        await Task.Delay(100);

        // Filter users based on search text
        var searchText = args.Filter ?? string.Empty;
        var filteredUsers = users
            .Where(u => u.Id != "user1") // Exclude current user
            .Where(u => u.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            .Skip(args.Skip ?? 0)
            .Take(args.Top ?? 5)
            .Select(u => new MentionUserContext
            {
                UserId = u.Id,
                UserName = u.Name,
                IsInChat = true
            })
            .ToList();

        if ((args.Skip ?? 0) == 0)
        {
            mentionUsers = filteredUsers;
        }
        else
        {
            mentionUsers.AddRange(filteredUsers);
        }

        mentionUsersCount = users.Count(u => u.Id != "user1" && u.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        StateHasChanged();

        console.Log($"Mention search: '{searchText}' - Found {filteredUsers.Count} results", AlertStyle.Info);
    }

    void OnMessageAdded(ChatMessage message)
    {
        var participant = users.FirstOrDefault(p => p.Id == message.UserId);
        var participantName = participant?.Name ?? "Unknown";
        var contentPreview = message.Content.Substring(0, Math.Min(50, message.Content.Length));
        console.Log($"Message added: {participantName} - {contentPreview}...", 
                   message.UserId == "user1" ? AlertStyle.Info : AlertStyle.Success);
    }

    void OnMessageSent(ChatMessage message)
    {
        console.Log($"Message sent: {message.Content}", AlertStyle.Info);
    }

    void OnMessagesChanged(IEnumerable<ChatMessage> newMessages)
    {
        messages = newMessages.ToList();
        StateHasChanged();
    }

    void OnChatCleared()
    {
        console.Log("Chat cleared", AlertStyle.Warning);
    }

    async Task SimulateJaneTyping()
    {
        if (basicChat == null)
        {
            return;
        }

        await basicChat.SetUserTyping("user2", true);
        await Task.Delay(1500);
        await basicChat.SetUserTyping("user2", false);
    }

    void OnTypingChanged(ChatTypingEventArgs args)
    {
        // In a real app you would broadcast this via SignalR to other clients.
        console.Log($"TypingChanged: {args.UserId} => {args.IsTyping}", AlertStyle.Info);
    }
}
```
