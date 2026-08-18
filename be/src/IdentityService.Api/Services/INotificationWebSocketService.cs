using System.Net.WebSockets;
using IdentityService.Api.DTOs.Calendar;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Api.Services;

public interface INotificationWebSocketService
{
    Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket, CancellationToken cancellationToken);
    Task SendNotificationAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default);
    Task BroadcastMessageAsync(object message, CancellationToken cancellationToken = default);
}
