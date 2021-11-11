using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Radzen
{
    /// <summary>
    /// Class NotificationService. Contains variuos methods with options to open notifications. 
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
                Style = message.Style
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
        public void Notify(NotificationSeverity severity = NotificationSeverity.Info, string summary = "", string detail = "", double duration = 3000)
        {
            var newMessage = new NotificationMessage()
            {
                Duration = duration,
                Severity = severity,
                Summary = summary,
                Detail = detail
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
    }
}
