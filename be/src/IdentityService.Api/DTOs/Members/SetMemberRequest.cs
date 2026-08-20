namespace IdentityService.Api.DTOs.Members;

public sealed record SetMemberRequest(
    bool IsMember = true,
    IReadOnlyList<Guid>? RoleIds = null,
    IReadOnlyList<Guid>? PermissionIds = null,
    bool IsOwner = false);

public sealed record MemberResponse(
    Guid UserId,
    Guid PrincipalId,
    string Email,
    string DisplayName,
    bool IsOwner,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> PermissionIds);
