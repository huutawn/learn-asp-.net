using Microsoft.AspNetCore.SignalR;

namespace IdentityService.Api.Hub.Impl;

public sealed class SignalRHub(IHubContext<NotificationHub> hubContext) : IHub
{
    public Task SendToUserAsync<T>(
        Guid userId,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientMethod);

        return hubContext.Clients
            .User(userId.ToString("D"))
            .SendAsync(clientMethod, message, cancellationToken);
    }

    public Task SendToUsersAsync<T>(
        IReadOnlyCollection<Guid> userIds,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientMethod);

        return hubContext.Clients
            .Users(userIds.Select(id => id.ToString("D")))
            .SendAsync(clientMethod, message, cancellationToken);
    }

    public Task SendToGroupAsync<T>(
        string groupName,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupName);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientMethod);

        return hubContext.Clients
            .Group(groupName)
            .SendAsync(clientMethod, message, cancellationToken);
    }
}
