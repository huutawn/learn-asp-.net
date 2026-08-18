using System.ComponentModel.DataAnnotations;

namespace IdentityService.Api.DTOs.Groups;

public sealed record CreateGroupReq(
    [param: Required, MaxLength(100)]
    string Name,
    [param: MaxLength(1_000)]
    string? Description,
    [param: Required, MaxLength(64)]
    string Type);
