using TodoApi.Models;

namespace TodoApi.Dtos;

public record CreateTodoRequest(string Title, string? Description);

public record UpdateTodoRequest(string Title, string? Description);

public record UpdateStatusRequest(TodoStatus Status);

public record TodoItemResponse(
    int Id,
    string Title,
    string? Description,
    TodoStatus Status,
    long AccumulatedSeconds,
    DateTime? CurrentStartedAtUtc,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc)
{
    public static TodoItemResponse FromEntity(TodoItem item) => new(
        item.Id,
        item.Title,
        item.Description,
        item.Status,
        item.AccumulatedSeconds,
        AsUtc(item.CurrentStartedAtUtc),
        AsUtc(item.CreatedAtUtc)!.Value,
        AsUtc(item.CompletedAtUtc));

    // MySQL DATETIME columns don't carry timezone info, so the driver hands back
    // Kind=Unspecified. Force Utc so JSON serialization always includes the 'Z'.
    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
