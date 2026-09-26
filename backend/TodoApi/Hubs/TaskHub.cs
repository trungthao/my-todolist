using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;

namespace TodoApi.Hubs;

[Authorize]
public class TaskHub : Hub
{
    private readonly AppDbContext _db;

    public TaskHub(AppDbContext db)
    {
        _db = db;
    }

    public async Task JoinTeam(int teamId)
    {
        var userId = int.Parse(Context.User!.FindFirstValue("uid")!);
        var isMember = await _db.TeamMembers
            .AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);

        if (isMember)
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(teamId));
    }

    public async Task LeaveTeam(int teamId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(teamId));

    public static string GroupName(int teamId) => $"team-{teamId}";
}
