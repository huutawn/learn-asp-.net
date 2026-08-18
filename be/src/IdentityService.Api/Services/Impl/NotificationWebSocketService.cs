using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IdentityService.Api.DTOs.Calendar;
using Microsoft.IdentityModel.Tokens;

namespace IdentityService.Api.Services.Impl;

public sealed class NotificationWebSocketService(
    IConfiguration configuration,
    ILogger<NotificationWebSocketService> logger) : INotificationWebSocketService
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, WebSocket>> _userSockets = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task HandleWebSocketAsync(HttpContext context, WebSocket webSocket, CancellationToken cancellationToken)
    {
        // Extract token from Query string or Header
        var token = context.Request.Query["access_token"].ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader["Bearer ".Length..].Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("WebSocket connection rejected: Missing access token.");
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized: Missing token", cancellationToken);
            return;
        }

        var userId = ValidateTokenAndGetUserId(token);
        if (userId is null)
        {
            logger.LogWarning("WebSocket connection rejected: Invalid or expired token.");
            await webSocket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Unauthorized: Invalid token", cancellationToken);
            return;
        }

        var connectionId = Guid.NewGuid().ToString("N");
        var userConnections = _userSockets.GetOrAdd(userId.Value, _ => new ConcurrentDictionary<string, WebSocket>());
        userConnections[connectionId] = webSocket;

        logger.LogInformation("WebSocket client connected. UserId: {UserId}, ConnectionId: {ConnectionId}", userId.Value, connectionId);

        // Send connection ACK
        var welcomeMsg = JsonSerializer.Serialize(new
        {
            type = "connected",
            userId = userId.Value,
            connectedAt = DateTimeOffset.UtcNow
        }, JsonOptions);
        await SendTextAsync(webSocket, welcomeMsg, cancellationToken);

        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (message.Contains("\"ping\"", StringComparison.OrdinalIgnoreCase))
                    {
                        var pong = JsonSerializer.Serialize(new { type = "pong", timestamp = DateTimeOffset.UtcNow }, JsonOptions);
                        await SendTextAsync(webSocket, pong, cancellationToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown or client abort
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "WebSocket error for user {UserId}", userId.Value);
        }
        finally
        {
            userConnections.TryRemove(connectionId, out _);
            if (userConnections.IsEmpty)
            {
                _userSockets.TryRemove(userId.Value, out _);
            }

            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
                }
                catch { }
            }

            webSocket.Dispose();
            logger.LogInformation("WebSocket client disconnected. UserId: {UserId}, ConnectionId: {ConnectionId}", userId.Value, connectionId);
        }
    }

    public async Task SendNotificationAsync(Guid userId, NotificationResponse notification, CancellationToken cancellationToken = default)
    {
        if (!_userSockets.TryGetValue(userId, out var connections) || connections.IsEmpty)
        {
            logger.LogDebug("No active WebSocket connections for user {UserId}", userId);
            return;
        }

        var payload = JsonSerializer.Serialize(new
        {
            type = "notification",
            data = notification
        }, JsonOptions);

        var deadConnections = new List<string>();
        foreach (var (connId, socket) in connections)
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await SendTextAsync(socket, payload, cancellationToken);
                    logger.LogInformation("Sent WebSocket notification {NotificationId} to user {UserId} (conn: {ConnId})", notification.Id, userId, connId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send WebSocket notification to connection {ConnId}", connId);
                    deadConnections.Add(connId);
                }
            }
            else
            {
                deadConnections.Add(connId);
            }
        }

        foreach (var dead in deadConnections)
        {
            connections.TryRemove(dead, out _);
        }
    }

    public async Task BroadcastMessageAsync(object message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        foreach (var (_, connections) in _userSockets)
        {
            foreach (var (_, socket) in connections)
            {
                if (socket.State == WebSocketState.Open)
                {
                    try
                    {
                        await SendTextAsync(socket, payload, cancellationToken);
                    }
                    catch { }
                }
            }
        }
    }

    private static async Task SendTextAsync(WebSocket socket, string text, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
    }

    private Guid? ValidateTokenAndGetUserId(string token)
    {
        try
        {
            var jwtKey = configuration["Jwt:Key"];
            var jwtIssuer = configuration["Jwt:Issuer"];
            var jwtAudience = configuration["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey)) return null;

            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            var subClaim = principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return Guid.TryParse(subClaim, out var userId) ? userId : null;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Token validation failed for WebSocket connection");
            return null;
        }
    }
}
