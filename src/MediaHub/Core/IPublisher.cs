namespace MediaHub.Core;

/// <summary>
/// Defines a mediator interface for publishing notifications
/// </summary>
public interface IPublisher
{
    /// <summary>
    /// Publish a notification to multiple handlers
    /// </summary>
    /// <param name="notification">Notification object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that represents the publish operation</returns>
    Task Publish(INotification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publish a notification to multiple handlers
    /// </summary>
    /// <typeparam name="TNotification">Notification type</typeparam>
    /// <param name="notification">Notification object</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>Task that represents the publish operation</returns>
    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) 
        where TNotification : INotification;
}