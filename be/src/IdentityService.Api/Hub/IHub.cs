namespace IdentityService.Api.Hub;

public interface IHub
{
    Task SendToUserAsync<T>(
        Guid userId,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default);

    Task SendToUsersAsync<T>(
        IReadOnlyCollection<Guid> userIds,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default);

    Task SendToGroupAsync<T>(
        string groupName,
        string clientMethod,
        T message,
        CancellationToken cancellationToken = default);
}
