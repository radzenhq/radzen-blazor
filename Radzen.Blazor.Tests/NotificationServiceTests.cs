using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class NotificationServiceTests
    {
        [Fact]
        public void NotificationService_IsMessageIsNull_ExceptionExpected()
        {
            NotificationService notificationService = new NotificationService();
            NotificationMessage notificationMessage = null;


            var exception = Record.Exception(() => notificationService.Notify(notificationMessage));


            Assert.IsType<ArgumentNullException>(exception);
        }
        
        [Fact]
        public void NotificationService_CheckAreTwoMessages_Equals()
        {
            var messageOne = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            var messageTwo = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            Assert.True(messageOne.Equals(messageTwo));
        }

        [Fact]
        public void NotificationService_CheckAreTwoMessages_NotEquals()
        {
            var messageOne = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            var messageTwo = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary Two",
                Detail = "Info Detail Two",
                Duration = 6000
            };

            Assert.False(messageOne.Equals(messageTwo));
        }

        [Fact]
        public void NotificationService_CheckAreTwoMessages_EqualsByReference()
        {
            var messageOne = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            var messageTwo = messageOne;

            Assert.True(messageOne.Equals(messageTwo));
        }

        [Fact]
        public void NotificationService_CheckAreTwoMessages_EqualsByOperator()
        {
            var messageOne = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            var messageTwo = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            Assert.True(messageOne == messageTwo);
        }

        [Fact]
        public void NotificationService_CheckAreTwoMessages_NotEqualsByOperator()
        {
            var messageOne = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            };

            var messageTwo = new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary Two",
                Detail = "Info Detail Two",
                Duration = 6000
            };

            Assert.True(messageOne != messageTwo);
        }

        [Fact]
        public void NotificationService_MessagesCount_AfterAddingMessages()
        {
            NotificationService notificationService = new NotificationService();

            //Messages are the same so only one should be added
            notificationService.Notify(NotificationSeverity.Info, "Info Summary", "Info Detail", 4000);
            notificationService.Notify(new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            });

            int expectedMessagesNumber = 1;

            Assert.Equal(expectedMessagesNumber, notificationService.Messages.Count);
        }

        [Fact]
        public void NotificationService_MessagesCount_AfterAddingTwoDifferentMessages()
        {
            NotificationService notificationService = new NotificationService();

            //Messages are the same so only one should be added
            notificationService.Notify(NotificationSeverity.Info, "Info Summary 2", "Info Detail 2", 6000);
            notificationService.Notify(new NotificationMessage()
            {
                Severity = NotificationSeverity.Info,
                Summary = "Info Summary",
                Detail = "Info Detail",
                Duration = 4000
            });

            int expectedMessagesNumber = 2;

            Assert.Equal(expectedMessagesNumber, notificationService.Messages.Count);
        }
    }
}
