using System.Linq.Expressions;
using InteresMe.API.Modules.Initiatives.Contracts.Responses;
using InteresMe.API.Modules.Initiatives.Domain.Entities;

namespace InteresMe.API.Modules.Initiatives.Mapping;

public static class InitiativeMapper
{
    public static Expression<Func<Initiative, PublicInitiativeResponse>> PublicResponseProjection =>
        initiative => new PublicInitiativeResponse
        {
            Id = initiative.Id,
            Slug = initiative.Slug,
            Title = initiative.Title,
            ShortDescription = initiative.ShortDescription,
            GoalType = initiative.GoalType,
            Status = initiative.Status,
            Visibility = initiative.Visibility,
            University = initiative.University,
            TeamSize = initiative.TeamSize,
            OwnerDisplayName = initiative.OwnerUser.DisplayName,
            CreatedAt = initiative.CreatedAt,
            Interests = initiative.InitiativeInterests
                .OrderBy(initiativeInterest => initiativeInterest.Interest.Name)
                .Select(initiativeInterest => new InitiativeInterestResponse
                {
                    Id = initiativeInterest.Interest.Id,
                    Name = initiativeInterest.Interest.Name,
                    Slug = initiativeInterest.Interest.Slug
                })
                .ToList(),
            Roles = initiative.Roles
                .OrderBy(role => role.Name)
                .Select(role => new InitiativeRoleResponse
                {
                    Id = role.Id,
                    Name = role.Name
                })
                .ToList()
        };

    public static InitiativeResponse ToResponse(Initiative initiative) => new()
    {
        Id = initiative.Id,
        OwnerUserId = initiative.OwnerUserId,
        Slug = initiative.Slug,
        Title = initiative.Title,
        ShortDescription = initiative.ShortDescription,
        GoalType = initiative.GoalType,
        University = initiative.University,
        TeamSize = initiative.TeamSize,
        Status = initiative.Status,
        Visibility = initiative.Visibility,
        CreatedAt = initiative.CreatedAt,
        UpdatedAt = initiative.UpdatedAt,
        Interests = initiative.InitiativeInterests
            .OrderBy(initiativeInterest => initiativeInterest.Interest.Name)
            .Select(initiativeInterest => new InitiativeInterestResponse
            {
                Id = initiativeInterest.Interest.Id,
                Name = initiativeInterest.Interest.Name,
                Slug = initiativeInterest.Interest.Slug
            })
            .ToList(),
        Roles = initiative.Roles
            .OrderBy(role => role.Name)
            .Select(role => new InitiativeRoleResponse
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToList()
    };

    public static InitiativeJoinRequestResponse ToJoinRequestResponse(
        InitiativeJoinRequest joinRequest) => new()
    {
        Id = joinRequest.Id,
        InitiativeId = joinRequest.InitiativeId,
        UserId = joinRequest.UserId,
        RoleId = joinRequest.RoleId,
        Message = joinRequest.Message,
        Motivation = joinRequest.Motivation,
        Experience = joinRequest.Experience,
        Contribution = joinRequest.Contribution,
        Availability = joinRequest.Availability,
        Status = joinRequest.Status,
        CreatedAt = joinRequest.CreatedAt,
        UpdatedAt = joinRequest.UpdatedAt
    };
}
