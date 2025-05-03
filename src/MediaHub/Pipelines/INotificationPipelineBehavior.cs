namespace MediaHub.Pipelines;

/// <summary>
/// Pipeline behavior for notification processing
/// </summary>
/// <typeparam name="TNotification">Notification type</typeparam>
public interface INotificationPipelineBehavior<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Pipeline handler for notifications
    /// </summary>
    /// <param name="notification">Notification instance</param>
    /// <param name="next">Next delegate in pipeline</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task representing the notification handling</returns>
    Task Handle(TNotification notification, NotificationHandlerDelegate next, CancellationToken cancellationToken);
}

/// <summary>
/// Delegate for the next notification handler in the pipeline
/// </summary>
/// <returns>Task representing completion</returns>
public delegate Task NotificationHandlerDelegate();