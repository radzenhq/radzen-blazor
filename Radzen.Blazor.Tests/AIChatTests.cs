using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Radzen;
using System;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class AIChatTests
    {
        private void RegisterChatService(TestContext ctx)
        {
            // Register a dummy HttpClient and default options for AIChatService
            ctx.Services.AddSingleton(new HttpClient());
            ctx.Services.AddScoped<IAIChatService, AIChatService>();
        }

        [Fact]
        public void RadzenAIChat_ShouldRenderWithDefaultProperties()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>();
            Assert.Contains("Type your message...", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldRenderWithCustomTitle()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Title, "Custom Chat"));
            Assert.Contains("Custom Chat", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldRenderWithCustomPlaceholder()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Placeholder, "Enter your message here..."));
            Assert.Contains("Enter your message here...", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldRenderWithCustomEmptyMessage()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.EmptyMessage, "No messages yet"));
            Assert.Contains("No messages yet", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldShowClearButtonByDefault()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>();
            Assert.Contains("rz-chat-header-clear", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldHideClearButtonWhenShowClearButtonIsFalse()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.ShowClearButton, false));
            Assert.DoesNotContain("clear_all", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldBeDisabledWhenDisabledIsTrue()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.Disabled, true));
            Assert.Contains("disabled", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldBeReadOnlyWhenReadOnlyIsTrue()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters
                .Add(p => p.ReadOnly, true));
            Assert.Contains("readonly", component.Markup);
        }

        [Fact]
        public void RadzenAIChat_ShouldHaveCorrectCssClass()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>();
            Assert.Contains("rz-chat", component.Markup);
        }

        [Fact]
        public void ChatMessage_ShouldHaveCorrectProperties()
        {
            // Arrange
            var message = new ChatMessage
            {
                Content = "Test message",
                IsUser = true,
                Timestamp = DateTime.Now
            };
            // Assert
            Assert.NotEmpty(message.Id);
            Assert.Equal("Test message", message.Content);
            Assert.True(message.IsUser);
            Assert.False(message.IsStreaming);
        }

        [Fact]
        public void RadzenAIChat_AddMessage_ShouldAddMessageToList()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>();
            // Act
            component.Instance.AddMessage("Test message", true);
            // Assert
            var messages = component.Instance.GetMessages();
            Assert.Single(messages);
            Assert.Equal("Test message", messages[0].Content);
            Assert.True(messages[0].IsUser);
        }

        [Fact]
        public void RadzenAIChat_ClearChat_ShouldRemoveAllMessages()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>();
            component.Instance.AddMessage("Test message 1", true);
            component.Instance.AddMessage("Test message 2", false);
            // Act
            component.InvokeAsync(async () => await component.Instance.ClearChat()).Wait();
            // Assert
            Assert.Empty(component.Instance.GetMessages());
        }

        [Fact]
        public void RadzenAIChat_ShouldLimitMessagesToMaxMessages()
        {
            using var ctx = new TestContext();
            RegisterChatService(ctx);
            var component = ctx.RenderComponent<RadzenAIChat>(parameters => parameters.Add(p => p.MaxMessages, 3));
            component.Instance.AddMessage("Message 1", true);
            component.Instance.AddMessage("Message 2", false);
            component.Instance.AddMessage("Message 3", true);
            component.Instance.AddMessage("Message 4", false);
            // Assert
            var messages = component.Instance.GetMessages();
            Assert.Equal(3, messages.Count);
            Assert.Equal("Message 2", messages[0].Content);
            Assert.Equal("Message 3", messages[1].Content);
            Assert.Equal("Message 4", messages[2].Content);
        }
    }
} 