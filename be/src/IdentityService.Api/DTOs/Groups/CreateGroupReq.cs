using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Groups;

public sealed record CreateGroupReq(
    [param: Required, MaxLength(100)]
    string Name,
    [param: Required, MaxLength(64)]
    string Type,
    [param: MaxLength(1_000)]
    string? Description = null,
    Guid? ScopeId = null);
