using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace IdentityService.Api.Hub.Impl;

public sealed class SubjectUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        return connection.User?.FindFirstValue("sub")
            ?? connection.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
