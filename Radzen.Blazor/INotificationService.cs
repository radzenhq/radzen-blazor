using System.Collections.ObjectModel;

namespace Radzen
{
    /// <summary>
    /// Interface INotificationService. Contains various methods with options to open notifications. 
    /// Should be added as scoped service in the application services and RadzenNotification should be added in application main layout.
    /// </summary>
    /// <example>
    /// <code>
    /// @inject INotificationService NotificationService
    /// &lt;RadzenButton Text="Show info notification" Click=@(args => NotificationService.Notify(new NotificationMessage { Severity = NotificationSeverity.Info, Summary = "Info Summary", Detail = "Info Detail", Duration = 4000 })) / &gt;
    /// </code>
    /// </example>
    public interface INotificationService
    {
        /// <summary>
        /// Gets the messages.
        /// </summary>
        /// <value>The messages.</value>
        ObservableCollection<NotificationMessage> Messages { get; }

        /// <summary>
        /// Notifies the specified severity.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="summary">The summary.</param>
        /// <param name="detail">The detail.</param>
        /// <param name="duration">The duration.</param>
        void Notify(NotificationSeverity severity = NotificationSeverity.Info, string summary = "", string detail = "", double duration = 3000);

        /// <summary>
        /// Notifies the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        void Notify(NotificationMessage message);
    }
}