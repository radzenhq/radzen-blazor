using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Linq;
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
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
            );

            Assert.Contains("Test Chat", component.Markup);
        }

        [Fact]
        public void RadzenChat_ShouldShowEmptyMessageWhenNoMessages()
        {
            using var ctx = new TestContext();
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, new List<ChatUser>())
                .Add(p => p.Messages, new List<ChatMessage>())
                .Add(p => p.EmptyMessage, "No messages yet!")
            );

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
            var component = ctx.RenderComponent<RadzenChat>(parameters => parameters
                .Add(p => p.Title, "Test Chat")
                .Add(p => p.CurrentUserId, "user1")
                .Add(p => p.Users, users)
                .Add(p => p.Messages, messages)
            );

            Assert.Contains(timestamp.ToString("HH:mm"), component.Markup);
        }
    }
}
