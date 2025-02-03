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
            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (!Messages.Contains(message))
            {
                Messages.Add(message);
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
        public void Notify(NotificationSeverity severity, string summary,
            string detail, TimeSpan duration, Action<NotificationMessage> click = null)
        {
            Notify(severity, summary, detail, duration.TotalMilliseconds, click);
        }

        /// <summary>
        /// Notifies the specified severity.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="summary">The summary.</param>
        /// <param name="detail">The detail.</param>
        /// <param name="duration">The duration, default of 3 seconds.</param>
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
    public class NotificationMessage : IEquatable<NotificationMessage>
    {
        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>The duration.</value>
        public double? Duration { get; set; } = 3000;
        /// <summary>
        /// Gets or sets the severity.
        /// </summary>
        /// <value>The severity.</value>
        public NotificationSeverity Severity { get; set; } = NotificationSeverity.Info;
        /// <summary>
        /// Gets or sets the summary.
        /// </summary>
        /// <value>The summary.</value>
        public string Summary { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the detail.
        /// </summary>
        /// <value>The detail.</value>
        public string Detail { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        public string Style { get; set; } = string.Empty;
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

        /// <summary>
        /// Gets or sets the detail content.
        /// </summary>
        /// <value>The detail content.</value>
        public RenderFragment<NotificationService> DetailContent { get; set; }
        /// <summary>
        /// Gets or sets the summary content.
        /// </summary>
        /// <value>The summary content.</value>
        public RenderFragment<NotificationService> SummaryContent { get; set; }


        #region Implementation of IEquatable<NotificationMessage> and operators overloading

        /// <summary>
        /// Check if NotificationMessage instance is equal to current instance.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(NotificationMessage other)
        {
            if(other == null) return false;

            if(object.ReferenceEquals(this, other)) return true;

            return this.Severity == other.Severity
                && this.Summary == other.Summary
                && this.Detail == other.Detail
                && this.Duration == other.Duration
                && this.Style == other.Style
                && this.Click == other.Click
                && this.Close == other.Close
                && this.CloseOnClick == other.CloseOnClick
                && this.Payload == other.Payload
                && this.DetailContent == other.DetailContent
                && this.SummaryContent == other.SummaryContent;
        }

        /// <summary>
        /// Check if object instance is equal to current instance.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) => Equals(obj as NotificationMessage);

        /// <summary>
        ///  Return a hash code for the current object
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => (Summary, Detail, Duration, Style, Click, Close, CloseOnClick, Payload, SummaryContent, DetailContent).GetHashCode();

        /// <summary>
        /// Overloading == operator for NotificationMessage.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="otherMessage"></param>
        /// <returns></returns>
        public static bool operator ==(NotificationMessage message, NotificationMessage otherMessage)
        {
            if (message is null)
            {
                if (otherMessage is null)
                {
                    return true;
                }

                return false;
            }

            return message.Equals(otherMessage);
        }

        /// <summary>
        /// Overloading != operator for NotificationMessage.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="otherMessage"></param>
        /// <returns></returns>
        public static bool operator !=(NotificationMessage message, NotificationMessage otherMessage)
        {
            return !(message == otherMessage);
        }

        #endregion
    }
}
