using Microsoft.AspNetCore.SignalR;

namespace IdentityService.Api.Hub;

public sealed class NotificationHub : Microsoft.AspNetCore.SignalR.Hub
{
}

public static class NotificationHubMethods
{
    public const string Notification = "notification";
}
