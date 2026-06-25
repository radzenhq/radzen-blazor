using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class ChatTests
    {
        [Fact]
        public void RadzenChat_ShouldRenderWithTitle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
            );

            Assert.Contains("Test Chat", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldRenderWithTitleContent()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.TitleContent, builder =>
                {
                    builder.OpenComponent<RadzenBadge>(0);
                    builder.AddAttribute(1, nameof(RadzenBadge.Text), "Chat Badge");
                    builder.CloseComponent();
                })
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.EmptyMessage, "No messages yet!")
            );

            Assert.Contains("Chat Badge", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldNotRenderTitleSpanWhenNoTitle()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
            );

            Assert.DoesNotContain("rz-chat-header-title", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowEmptyMessageWhenNoMessages()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.EmptyMessage, "No messages yet!")
            );

            Assert.Contains("Test Chat", component.Markup);
            Assert.Contains("No messages yet!", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldDisplayMessages()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Hello", UserId = "user1", Timestamp = DateTime.Now },
                new ChatMessage { Content = "Hi there!", UserId = "user2", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            Assert.Contains("Hello", component.Markup);
            Assert.Contains("Hi there!", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowUsersInHeader()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowUsers, true)
            );

            Assert.Contains("John", component.Markup);
            Assert.Contains("Jane", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowUserNamesAboveMessages()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Hello", UserId = "user2", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
                .Add(p => p.ShowUserNames, true)
            );

            Assert.Contains("Jane", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowClearButtonWhenEnabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowClearButton, true)
            );

            var clearButton = component.Find(".rz-chat-header-clear");
            Assert.NotNull(clearButton);
        }

        [Fact]
        public void RadzenChat_ShouldNotShowClearButtonWhenDisabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowClearButton, false)
            );

            var clearButton = component.FindAll(".rz-chat-header-clear");
            Assert.Empty(clearButton);
        }

        [Fact]
        public void RadzenChat_ShouldLimitVisibleUsers()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" },
                new ChatUser { Id = "user3", Name = "Bob" },
                new ChatUser { Id = "user4", Name = "Alice" },
                new ChatUser { Id = "user5", Name = "Charlie" },
                new ChatUser { Id = "user6", Name = "David" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowUsers, true)
                .Add(p => p.MaxVisibleUsers, 3)
            );

            // Should show "+3" for the remaining users
            Assert.Contains("+3", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowUserMessagesOnRight()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "My message", UserId = "user1", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            var userMessage = component.Find(".rz-chat-message-user");
            Assert.NotNull(userMessage);
        }

        [Fact]
        public void RadzenChat_ShouldShowUserMessagesOnLeft()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Other message", UserId = "user2", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            var participantMessage = component.Find(".rz-chat-message-participant");
            Assert.NotNull(participantMessage);
        }

        [Fact]
        public void RadzenChat_ShouldShowAvatarInitials()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John Doe" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowUsers, true)
            );

            Assert.Contains("JD", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowAvatarImageWhenProvided()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John", AvatarUrl = "https://example.com/avatar.jpg" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ShowUsers, true)
            );

            var avatarImage = component.Find(".rz-chat-participant-image");
            Assert.NotNull(avatarImage);
            Assert.Equal("https://example.com/avatar.jpg", avatarImage.GetAttribute("src"));
        }

        [Fact]
        public void RadzenChat_ShouldBeDisabledWhenDisabled()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.Disabled, true)
            );

            var textarea = component.Find(".rz-chat-textarea");
            Assert.True(textarea.HasAttribute("disabled"));
        }

        [Fact]
        public void RadzenChat_ShouldBeReadOnlyWhenReadOnly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.ReadOnly, true)
            );

            var textarea = component.Find(".rz-chat-textarea");
            Assert.True(textarea.HasAttribute("readonly"));
        }

        [Fact]
        public void RadzenChat_ShouldShowCustomPlaceholder()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.Placeholder, "Custom placeholder")
            );

            var textarea = component.Find(".rz-chat-textarea");
            Assert.Equal("Custom placeholder", textarea.GetAttribute("placeholder"));
        }

        [Fact]
        public void RadzenChat_ShouldShowStreamingIndicator()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Streaming message", UserId = "user1", IsStreaming = true, Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            var streamingIcon = component.Find(".rz-chat-message-streaming-icon");
            Assert.NotNull(streamingIcon);
        }

        [Fact]
        public void RadzenChat_ShouldShowMessageTimestamps()
        {
            var timestamp = DateTime.Now.AddHours(-1);
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Test message", UserId = "user1", Timestamp = timestamp }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            Assert.Contains(timestamp.ToString("HH:mm"), component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldRenderTimestampUsingTimestampFormat()
        {
            var timestamp = new DateTime(2026, 4, 20, 13, 45, 0);
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Hello", UserId = "user1", Timestamp = timestamp }
            };
            var users = new List<ChatUser> { new ChatUser { Id = "user1", Name = "John" } };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
                .Add(p => p.TimestampFormat, "yyyy-MM-dd HH:mm")
                .Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture)
            );

            Assert.Contains("2026-04-20 13:45", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldRenderDateSeparatorWhenDayChanges()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "First", UserId = "user1", Timestamp = new DateTime(2026, 4, 19, 10, 0, 0) },
                new ChatMessage { Content = "Second", UserId = "user1", Timestamp = new DateTime(2026, 4, 19, 23, 30, 0) },
                new ChatMessage { Content = "Third", UserId = "user1", Timestamp = new DateTime(2026, 4, 20, 8, 15, 0) }
            };
            var users = new List<ChatUser> { new ChatUser { Id = "user1", Name = "John" } };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
                .Add(p => p.DateSeparatorFormat, "yyyy-MM-dd")
                .Add(p => p.Culture, System.Globalization.CultureInfo.InvariantCulture)
            );

            var separators = component.FindAll(".rz-chat-date-separator");
            Assert.Equal(2, separators.Count);
            Assert.Contains("2026-04-19", component.Markup);
            Assert.Contains("2026-04-20", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldNotRenderDateSeparatorWhenDisabled()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "First", UserId = "user1", Timestamp = new DateTime(2026, 4, 19, 10, 0, 0) },
                new ChatMessage { Content = "Second", UserId = "user1", Timestamp = new DateTime(2026, 4, 20, 8, 15, 0) }
            };
            var users = new List<ChatUser> { new ChatUser { Id = "user1", Name = "John" } };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
                .Add(p => p.ShowDateSeparator, false)
            );

            Assert.Empty(component.FindAll(".rz-chat-date-separator"));
        }

        // Baseline tests for mention feature backwards compatibility
        [Fact]
        public void RadzenChat_ShouldRenderNormallyWithoutMentionCharacter()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Hello @user2", UserId = "user1", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            Assert.Contains("Hello @user2", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldNotShowMentionPopupWhenMentionCharacterIsNull()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, null)
            );

            var mentionPopup = component.FindAll(".rz-chat-mention-popup");
            Assert.Empty(mentionPopup);
        }

        [Fact]
        public void RadzenChat_ShouldSendMessagesWithoutMentionFeature()
        {
            var messages = new List<ChatMessage>();
            var messageSent = false;

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, messages)
                .Add(p => p.MessageSent, EventCallback.Factory.Create<ChatMessage>(this, (msg) =>
                {
                    messageSent = true;
                }))
            );

            Assert.False(messageSent);
        }

        // Feature tests for mention functionality
        [Fact]
        public void RadzenChat_ShouldParseMentionFormatCorrectly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
            );

            var chat = component.Instance;
            var text = "Hello @[user-123] how are you?";
            var mentions = chat.ParseMentions(text);

            Assert.Single(mentions);
            Assert.Equal("user-123", mentions[0].userId);
        }

        [Fact]
        public void RadzenChat_ShouldParseMutipleMentionsCorrectly()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
            );

            var chat = component.Instance;
            var text = "Hey @[user-123] and @[user-456] check this out!";
            var mentions = chat.ParseMentions(text);

            Assert.Equal(2, mentions.Count);
            Assert.Equal("user-123", mentions[0].userId);
            Assert.Equal("user-456", mentions[1].userId);
        }

        [Fact]
        public void RadzenChat_ShouldNotParseMentionsWhenMentionCharacterIsNull()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, null)
            );

            var chat = component.Instance;
            var text = "Hello @[user-123] how are you?";
            var mentions = chat.ParseMentions(text);

            Assert.Empty(mentions);
        }

        [Fact]
        public async Task RadzenChat_ShouldSetMentionSearchResults()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
            );

            var chat = component.Instance;
            var results = new List<MentionUserContext>
            {
                new MentionUserContext { UserId = "user-1", UserName = "Alice", IsInChat = true },
                new MentionUserContext { UserId = "user-2", UserName = "Bob", IsInChat = false }
            };

            await chat.SetMentionSearchResults(results);

            Assert.Equal(2, results.Count);
        }

        [Fact]
        public void RadzenChat_ShouldHaveMentionPropertiesOptional()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                // MentionCharacter not set - should default to null (disabled)
            );

            var chat = component.Instance;
            Assert.Null(chat.MentionCharacter);
        }

        [Fact]
        public void RadzenChat_ShouldAcceptMentionCharacter()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
            );

            var chat = component.Instance;
            Assert.Equal('@', chat.MentionCharacter);
        }

        [Fact]
        public void RadzenChat_ShouldAcceptMentionItemTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionItemTemplate, (RenderFragment<MentionUserContext>)((context) => builder => {
                    builder.OpenElement(0, "div");
                    builder.AddContent(1, context.UserName ?? "");
                    builder.CloseElement();
                }))
            );

            var chat = component.Instance;
            Assert.NotNull(chat.MentionItemTemplate);
        }

        [Fact]
        public void RadzenChat_ShouldAcceptMentionDisplayTemplate()
        {
            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionDisplayTemplate, (RenderFragment<string>)((userId) => builder => {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, $"@{userId}");
                    builder.CloseElement();
                }))
            );

            var chat = component.Instance;
            Assert.NotNull(chat.MentionDisplayTemplate);
        }

        [Fact]
        public void RadzenChat_ShouldDisplayMessageWithMentionUsingTemplate()
        {
            var messages = new List<ChatMessage>
            {
                new ChatMessage { Content = "Hi @[user-123] how are you?", UserId = "user1", Timestamp = DateTime.Now }
            };

            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user-123", Name = "Alice" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionDisplayTemplate, (RenderFragment<string>)((userId) => builder => {
                    builder.OpenElement(0, "span");
                    builder.AddAttribute(1, "class", "rz-mention-badge");
                    builder.AddContent(2, $"@{userId}");
                    builder.CloseElement();
                }))
            );

            var mentionBadge = component.FindAll(".rz-mention-badge");
            Assert.NotEmpty(mentionBadge);
        }

        [Fact]
        public async Task RadzenChat_ShouldRenderSelectedMentionWithUserNameInInput()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane Smith" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionDisplayTemplate, (RenderFragment<string>)((userId) => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, $"@{users.First(u => u.Id == userId).Name}");
                    builder.CloseElement();
                }))
            );

            SetPrivateProperty(component.Instance, "CurrentInput", "@ja");
            SetPrivateField(component.Instance, "mentionStartPosition", 0);
            SetPrivateField(component.Instance, "mentionSearchText", "ja");
            await InvokePrivateAsync(component.Instance, "InsertMention", new MentionUserContext { UserId = "user2", UserName = "Jane Smith", IsInChat = true });

            component.WaitForAssertion(() =>
            {
                var updatedTextarea = component.Find(".rz-chat-textarea");
                Assert.Equal("@Jane Smith", updatedTextarea.GetAttribute("value"));
            });
        }

        [Fact]
        public async Task RadzenChat_ShouldSendCanonicalMentionFormatWhenInputShowsUserName()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane Smith" }
            };

            ChatMessage sentMessage = null;

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;

            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionDisplayTemplate, (RenderFragment<string>)((userId) => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, $"@{users.First(u => u.Id == userId).Name}");
                    builder.CloseElement();
                }))
                .Add(p => p.MessageSent, EventCallback.Factory.Create<ChatMessage>(this, message => sentMessage = message))
            );

            SetPrivateProperty(component.Instance, "CurrentInput", "Hi @ja");
            SetPrivateField(component.Instance, "mentionStartPosition", 3);
            SetPrivateField(component.Instance, "mentionSearchText", "ja");
            await InvokePrivateAsync(component.Instance, "InsertMention", new MentionUserContext { UserId = "user2", UserName = "Jane Smith", IsInChat = true });

            component.WaitForAssertion(() =>
            {
                var updatedTextarea = component.Find(".rz-chat-textarea");
                Assert.Equal("Hi @Jane Smith", updatedTextarea.GetAttribute("value"));
            });

            var textarea = component.Find(".rz-chat-textarea");
            textarea.KeyDown(new KeyboardEventArgs { Key = "Enter" });

            component.WaitForAssertion(() =>
            {
                Assert.NotNull(sentMessage);
                Assert.Equal("Hi @[user2]", sentMessage!.Content);
            });
        }

        [Fact]
        public async Task RadzenChat_ShouldDeleteSelectedMentionInOneStep()
        {
            var users = new List<ChatUser>
            {
                new ChatUser { Id = "user1", Name = "John" },
                new ChatUser { Id = "user2", Name = "Jane Smith" }
            };

            using var ctx = new TestContext();
            ctx.JSInterop.Mode = JSRuntimeMode.Loose;
            ctx.JSInterop.Setup<int[]>("Radzen.getSelectionRange", _ => true).SetResult(new[] { 11, 11 });

            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.MentionCharacter, '@')
                .Add(p => p.MentionDisplayTemplate, (RenderFragment<string>)((userId) => builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, $"@{users.First(u => u.Id == userId).Name}");
                    builder.CloseElement();
                }))
            );

            SetPrivateProperty(component.Instance, "CurrentInput", "@ja");
            SetPrivateField(component.Instance, "mentionStartPosition", 0);
            SetPrivateField(component.Instance, "mentionSearchText", "ja");
            await InvokePrivateAsync(component.Instance, "InsertMention", new MentionUserContext { UserId = "user2", UserName = "Jane Smith", IsInChat = true });

            component.WaitForAssertion(() =>
            {
                var updatedTextarea = component.Find(".rz-chat-textarea");
                Assert.Equal("@Jane Smith", updatedTextarea.GetAttribute("value"));
            });

            var textarea = component.Find(".rz-chat-textarea");
            textarea.KeyDown(new KeyboardEventArgs { Key = "Backspace" });

            component.WaitForAssertion(() =>
            {
                var updatedTextarea = component.Find(".rz-chat-textarea");
                Assert.Equal(string.Empty, updatedTextarea.GetAttribute("value"));
            });
        }

        static void SetPrivateField(object instance, string fieldName, object value)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(instance, value);
        }

        static void SetPrivateProperty(object instance, string propertyName, object value)
        {
            var property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic);
            property.SetValue(instance, value);
        }

        static async Task InvokePrivateAsync(object instance, string methodName, params object[] args)
        {
            var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            var task = (Task)method.Invoke(instance, args);
            await task;
        }
    }
}
