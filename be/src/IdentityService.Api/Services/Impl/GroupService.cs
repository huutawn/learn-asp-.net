using IdentityService.Api.DTOs.Groups;
using IdentityService.Api.Entities;
using IdentityService.Api.Exceptions;
using IdentityService.Api.Repositories;

namespace IdentityService.Api.Services;

public sealed class GroupService(
    IGroupRepository groupRepository,
    TimeProvider timeProvider) : IGroupService
{
    public async Task<GroupResponse> CreateAsync(
        CreateGroupReq request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var type = request.Type.Trim();
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type))
        {
            throw new BadRequestException("Group name and type are required.");
        }

        if (await groupRepository.ExistsByNameAndTypeAsync(name, type, cancellationToken))
        {
            throw new ConflictException("A group with this name and type already exists.");
        }

        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim(),
            Type = type
        };
        await groupRepository.AddAsync(group, cancellationToken);
        return new GroupResponse(group.Id, group.Name, group.Description, group.Type);
    }

    public async Task<bool> SetMemberAsync(
        Guid groupId,
        Guid userId,
        bool isMember,
        CancellationToken cancellationToken)
    {
        if (!await groupRepository.GroupAndUserExistAsync(groupId, userId, cancellationToken))
        {
            return false;
        }

        var membership = await groupRepository.GetMembershipAsync(groupId, userId, cancellationToken);
        var now = timeProvider.GetUtcNow();
        if (membership is null && isMember)
        {
            groupRepository.AddMembership(new UserGroup
            {
                UserId = userId,
                GroupId = groupId,
                JoinedAtUtc = now
            });
        }
        else if (membership is not null)
        {
            membership.LeftAtUtc = isMember ? null : now;
            if (isMember)
            {
                membership.JoinedAtUtc = now;
            }
        }

        await groupRepository.SaveChangesAsync(cancellationToken);
        return true;
    }
}
