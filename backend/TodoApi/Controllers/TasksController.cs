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
    public async Task<ActionResult<IEnumerable<TodoItemResponse>>> GetAll()
    {
        var items = await _db.TodoItems
            .OrderBy(t => t.Id)
            .ToListAsync();

        return Ok(items.Select(TodoItemResponse.FromEntity));
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemResponse>> Create(CreateTodoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Tiêu đề không được để trống." });
        }

        var item = new TodoItem
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = TodoStatus.Todo,
            CreatedAtUtc = DateTime.UtcNow
        };

        _db.TodoItems.Add(item);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, TodoItemResponse.FromEntity(item));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<TodoItemResponse>> Update(int id, UpdateTodoRequest request)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { message = "Tiêu đề không được để trống." });
        }

        item.Title = request.Title.Trim();
        item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

        await _db.SaveChangesAsync();

        return Ok(TodoItemResponse.FromEntity(item));
    }

    [HttpPut("{id:int}/status")]
    public async Task<ActionResult<TodoItemResponse>> UpdateStatus(int id, UpdateStatusRequest request)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        ApplyStatusTransition(item, request.Status);

        await _db.SaveChangesAsync();

        return Ok(TodoItemResponse.FromEntity(item));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.TodoItems.FindAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        _db.TodoItems.Remove(item);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Core time-tracking rule: leaving InProgress banks the elapsed seconds into
    /// AccumulatedSeconds (whether the task is done or dragged back to Todo), and
    /// entering InProgress starts a fresh timestamp without resetting past time.
    /// </summary>
    private static void ApplyStatusTransition(TodoItem item, TodoStatus newStatus)
    {
        if (item.Status == newStatus)
        {
            return;
        }

        var now = DateTime.UtcNow;

        if (item.Status == TodoStatus.InProgress && item.CurrentStartedAtUtc.HasValue)
        {
            item.AccumulatedSeconds += (long)(now - item.CurrentStartedAtUtc.Value).TotalSeconds;
            item.CurrentStartedAtUtc = null;
        }

        if (newStatus == TodoStatus.InProgress)
        {
            item.CurrentStartedAtUtc = now;
        }

        item.CompletedAtUtc = newStatus == TodoStatus.Done ? now : null;
        item.Status = newStatus;
    }
}
