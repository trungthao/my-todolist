using TodoApi.Models;

namespace TodoApi.Dtos;

public record CreateTeamRequest(string Name);
public record AddMemberRequest(string Username);
public record ChangeMemberRoleRequest(TeamRole Role);
public record TeamResponse(int Id, string Name, TeamRole MyRole, int MemberCount, DateTime CreatedAtUtc, int? CreatedByUserId);
public record MemberResponse(int UserId, string Username, TeamRole Role, DateTime JoinedAtUtc);
