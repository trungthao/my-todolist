using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TeamsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamResponse>>> GetMyTeams()
    {
        var userId = GetUserId();
        var memberships = await _db.TeamMembers
            .Where(tm => tm.UserId == userId)
            .Include(tm => tm.Team)
                .ThenInclude(t => t.Members)
            .ToListAsync();

        return Ok(memberships.Select(tm => new TeamResponse(
            tm.Team.Id,
            tm.Team.Name,
            tm.Role,
            tm.Team.Members.Count,
            DateTime.SpecifyKind(tm.Team.CreatedAtUtc, DateTimeKind.Utc))));
    }

    [HttpPost]
    public async Task<ActionResult<TeamResponse>> Create(CreateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Tên team không được để trống." });

        var userId = GetUserId();

        var team = new Team
        {
            Name = request.Name.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();

        var member = new TeamMember
        {
            TeamId = team.Id,
            UserId = userId,
            Role = TeamRole.Admin,
            JoinedAtUtc = DateTime.UtcNow
        };
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync();

        return Ok(new TeamResponse(team.Id, team.Name, TeamRole.Admin, 1,
            DateTime.SpecifyKind(team.CreatedAtUtc, DateTimeKind.Utc)));
    }

    [HttpGet("{teamId:int}/members")]
    public async Task<ActionResult<IEnumerable<MemberResponse>>> GetMembers(int teamId)
    {
        var userId = GetUserId();
        if (!await IsMember(teamId, userId)) return Forbid();

        var members = await _db.TeamMembers
            .Where(tm => tm.TeamId == teamId)
            .Include(tm => tm.User)
            .OrderBy(tm => tm.JoinedAtUtc)
            .ToListAsync();

        return Ok(members.Select(tm => new MemberResponse(
            tm.UserId,
            tm.User.Username,
            tm.Role,
            DateTime.SpecifyKind(tm.JoinedAtUtc, DateTimeKind.Utc))));
    }

    [HttpPost("{teamId:int}/members")]
    public async Task<ActionResult<MemberResponse>> AddMember(int teamId, AddMemberRequest request)
    {
        var userId = GetUserId();
        if (!await IsAdmin(teamId, userId)) return Forbid();

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (target is null)
            return NotFound(new { message = "Không tìm thấy người dùng." });

        var exists = await _db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == target.Id);
        if (exists)
            return Conflict(new { message = "Người dùng đã là thành viên của team." });

        var member = new TeamMember
        {
            TeamId = teamId,
            UserId = target.Id,
            Role = TeamRole.Member,
            JoinedAtUtc = DateTime.UtcNow
        };
        _db.TeamMembers.Add(member);
        await _db.SaveChangesAsync();

        return Ok(new MemberResponse(target.Id, target.Username, TeamRole.Member,
            DateTime.SpecifyKind(member.JoinedAtUtc, DateTimeKind.Utc)));
    }

    [HttpDelete("{teamId:int}/members/{targetUserId:int}")]
    public async Task<IActionResult> RemoveMember(int teamId, int targetUserId)
    {
        var userId = GetUserId();
        if (!await IsAdmin(teamId, userId)) return Forbid();

        if (targetUserId == userId)
            return BadRequest(new { message = "Không thể tự xóa mình khỏi team." });

        var member = await _db.TeamMembers.FirstOrDefaultAsync(
            tm => tm.TeamId == teamId && tm.UserId == targetUserId);
        if (member is null) return NotFound();

        _db.TeamMembers.Remove(member);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPut("{teamId:int}/members/{targetUserId:int}/role")]
    public async Task<IActionResult> ChangeMemberRole(int teamId, int targetUserId, ChangeMemberRoleRequest request)
    {
        var userId = GetUserId();
        if (!await IsAdmin(teamId, userId)) return Forbid();

        if (targetUserId == userId)
            return BadRequest(new { message = "Không thể thay đổi role của chính mình." });

        var member = await _db.TeamMembers.FirstOrDefaultAsync(
            tm => tm.TeamId == teamId && tm.UserId == targetUserId);
        if (member is null) return NotFound();

        member.Role = request.Role;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private async Task<bool> IsMember(int teamId, int userId) =>
        await _db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);

    private async Task<bool> IsAdmin(int teamId, int userId) =>
        await _db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.Role == TeamRole.Admin);
}
