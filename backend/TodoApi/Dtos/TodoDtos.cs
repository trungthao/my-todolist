using TodoApi.Models;

namespace TodoApi.Dtos;

public record CreateTodoRequest(string Title, string? Description, int TeamId, int? AssignedToUserId);
public record UpdateTodoRequest(string Title, string? Description, int? AssignedToUserId);
public record UpdateStatusRequest(TodoStatus Status);

public record TodoItemResponse(
    int Id,
    string Title,
    string? Description,
    TodoStatus Status,
    long AccumulatedSeconds,
    DateTime? CurrentStartedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    int? TeamId,
    int? CreatedByUserId,
    string? CreatedByUsername,
    int? AssignedToUserId,
    string? AssignedToUsername)
{
    public static TodoItemResponse FromEntity(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.Status,
        item.AccumulatedSeconds,
        AsUtc(item.CurrentStartedAtUtc),
        AsUtc(item.CreatedAtUtc)!.Value,
        AsUtc(item.CompletedAtUtc),
        item.TeamId,
        item.CreatedByUserId,
        item.CreatedBy?.Username,
        item.AssignedToUserId,
        item.AssignedTo?.Username);

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
