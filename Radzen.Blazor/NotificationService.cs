using Microsoft.AspNetCore.Components;
using Radzen.Blazor;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Radzen
{
    /// <summary>
    /// Class NotificationService. Contains various methods with options to open notifications. 
    /// Should be added as scoped service in the application services and RadzenNotification should be added in application main layout.
    /// </summary>
    /// <example>
    /// <code>
    /// @inject NotificationService NotificationService
    /// &lt;RadzenButton Text="Show info notification" Click=@(args => NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Info Summary", Detail = "Info Detail", Duration = 4000 })) / &gt;
    /// </code>
    /// </example>
    public class NotificationService
    {
        /// <summary>
        /// Gets the messages.
        /// </summary>
        /// <value>The messages.</value>
        public ObservableCollection<NotificationMessage> Messages { get; private set; } = new ObservableCollection<NotificationMessage>();

        /// <summary>
        /// Notifies the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        public void Notify(NotificationMessage message)
        {
            var newMessage = new NotificationMessage()
            {
                Duration = message != null && message.Duration.HasValue ? message.Duration : 3000,
                Severity = message.Severity,
                Summary = message.Summary,
                Detail = message.Detail,
                Style = message.Style,
                Click = message.Click,
                Close = message.Close,
                CloseOnClick = message.CloseOnClick,
                Payload = message.Payload
            };

            if (!Messages.Contains(newMessage))
            {
                Messages.Add(newMessage);
            }
        }

        /// <summary>
        /// Notifies the specified severity.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="summary">The summary.</param>
        /// <param name="detail">The detail.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="click">The click event.</param>
        /// <param name="closeOnClick">If true, then the notification will be closed when clicked on.</param>
        /// <param name="payload">Used to store a custom payload that can be retreived later in the click event handler.</param>
        /// <param name="close">Action to be executed on close.</param>
        public void Notify(NotificationSeverity severity = NotificationSeverity.Info, string summary = "", string detail = "", double duration = 3000, Action<NotificationMessage> click = null, bool closeOnClick = false, object payload = null, Action<NotificationMessage> close = null)
        {
            var newMessage = new NotificationMessage()
            {
                Duration = duration,
                Severity = severity,
                Summary = summary,
                Detail = detail,
                Click = click,
                Close = close,
                CloseOnClick = closeOnClick,
                Payload = payload
            };

            if (!Messages.Contains(newMessage))
            {
                Messages.Add(newMessage);
            }
        }
    }

    /// <summary>
    /// Class NotificationMessage.
    /// </summary>
    public class NotificationMessage
    {
        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public double? Duration { get; set; }
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        /// <value>The severity.</value>
        public NotificationSeverity Severity { get; set; }
        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>The summary.</value>
        public string Summary { get; set; }
        /// <summary>
        /// Gets or sets the detail.
        /// </summary>
        /// <value>The detail.</value>
        public string Detail { get; set; }
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        public string Style { get; set; }
        /// <summary>
        /// Gets or sets the click event.
        /// </summary>
        /// <value>This event handler is called when the notification is clicked on.</value>
        public Action<NotificationMessage> Click { get; set; }
        /// <summary>
        /// Get or set the event for when the notification is closed
        /// </summary>
        public Action<NotificationMessage> Close { get; set; }
        /// <summary>
        /// Gets or sets click on close action.
        /// </summary>
        /// <value>If true, then the notification will be closed when clicked on.</value>
        public bool CloseOnClick { get; set; }
        /// <summary>
        /// Gets or sets notification payload.
        /// </summary>
        /// <value>Used to store a custom payload that can be retreived later in the click event handler.</value>
        public object Payload { get; set; }
   }
}
