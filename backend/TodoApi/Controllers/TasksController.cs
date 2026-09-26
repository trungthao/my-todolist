using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly AppDbContext _db;

    public TasksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItemResponse>>> GetAll([FromQuery] int teamId)
    {
        var userId = GetUserId();
        if (!await IsMember(teamId, userId)) return Forbid();

        var items = await _db.TodoItems
            .Where(t => t.TeamId == teamId)
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .OrderBy(t => t.Id)
            .ToListAsync();

        return Ok(items.Select(TodoItemResponse.FromEntity));
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> Create(CreateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tiêu đề không được để trống." });

        var userId = GetUserId();
        if (!await IsMember(request.TeamId, userId)) return Forbid();

        var assigneeId = request.AssignedToUserId ?? userId;
        if (!await IsMember(request.TeamId, assigneeId))
            return BadRequest(new { message = "Người được giao không phải thành viên của team." });

        var item = new TodoItem
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = TodoStatus.Todo,
            CreatedAtUtc = DateTime.UtcNow,
            TeamId = request.TeamId,
            CreatedByUserId = userId,
            AssignedToUserId = assigneeId
        };

        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(t => t.CreatedBy).LoadAsync();
        await _db.Entry(item).Reference(t => t.AssignedTo).LoadAsync();

        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, TodoItemResponse.FromEntity(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TodoItemResponse>> Update(int id, UpdateTodoRequest request)
    {
        var item = await _db.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (item is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "Tiêu đề không được để trống." });

        var userId = GetUserId();
        var membership = await GetMembership(item.TeamId, userId);
        if (membership is null) return Forbid();
        if (membership.Role != TeamRole.Admin && item.CreatedByUserId != userId) return Forbid();

        if (request.AssignedToUserId.HasValue && !await IsMember(item.TeamId!.Value, request.AssignedToUserId.Value))
            return BadRequest(new { message = "Người được giao không phải thành viên của team." });

        item.Title = request.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        if (request.AssignedToUserId.HasValue)
            item.AssignedToUserId = request.AssignedToUserId.Value;

        await _db.SaveChangesAsync();
        await _db.Entry(item).Reference(t => t.AssignedTo).LoadAsync();

        return Ok(TodoItemResponse.FromEntity(item));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TodoItemResponse>> UpdateStatus(int id, UpdateStatusRequest request)
    {
        var item = await _db.TodoItems
            .Include(t => t.CreatedBy)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (item is null) return NotFound();

        var userId = GetUserId();
        if (!await IsMember(item.TeamId, userId)) return Forbid();

        ApplyStatusTransition(item, request.Status);
        await _db.SaveChangesAsync();

        return Ok(TodoItemResponse.FromEntity(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null) return NotFound();

        var userId = GetUserId();
        var membership = await GetMembership(item.TeamId, userId);
        if (membership is null) return Forbid();
        if (membership.Role != TeamRole.Admin && item.CreatedByUserId != userId) return Forbid();

        _db.TodoItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static void ApplyStatusTransition(TodoItem item, TodoStatus newStatus)
    {
        if (item.Status == newStatus) return;

        var now = DateTime.UtcNow;

        if (item.Status == TodoStatus.InProgress && item.CurrentStartedAtUtc.HasValue)
        {
            item.AccumulatedSeconds += (long)(now - item.CurrentStartedAtUtc.Value).TotalSeconds;
            item.CurrentStartedAtUtc = null;
        }

        if (newStatus == TodoStatus.InProgress)
            item.CurrentStartedAtUtc = now;

        item.CompletedAtUtc = newStatus == TodoStatus.Done ? now : null;
        item.Status = newStatus;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue("uid")!);

    private async Task<bool> IsMember(int? teamId, int userId)
    {
        if (teamId is null) return false;
        return await _db.TeamMembers.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }

    private async Task<TeamMember?> GetMembership(int? teamId, int userId)
    {
        if (teamId is null) return null;
        return await _db.TeamMembers.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId);
    }
}
