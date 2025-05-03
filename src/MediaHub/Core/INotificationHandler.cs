using MediaHub.Contracts;

namespace MediaHub.Core;

/// <summary>
/// Defines a handler for a notification
/// </summary>
/// <typeparam name="TNotification">Notification type</typeparam>
public interface INotificationHandler<in TNotification> 
    where TNotification : INotification
{
    /// <summary>
    /// Handles a notification
    /// </summary>
    /// <param name="notification">The notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the notification handling</returns>
    Task Handle(TNotification notification, CancellationToken cancellationToken);
}