using TodoApi.Models;

namespace TodoApi.Dtos;

public record CreateTodoRequest(string Title, string? Description, int TeamId, int? AssignedToUserId);
public record UpdateTodoRequest(string Title, string? Description, int? AssignedToUserId);
public record UpdateStatusRequest(TodoStatus Status);
public record UpdateAssigneeRequest(int? AssignedToUserId);

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
    string? CreatedByFullName,
    int? AssignedToUserId,
    string? AssignedToFullName)
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
        DisplayName(item.CreatedBy),
        item.AssignedToUserId,
        DisplayName(item.AssignedTo));

    private static string? DisplayName(TodoApi.Models.User? user) =>
        user is null ? null : (string.IsNullOrWhiteSpace(user.FullName) ? user.Username : user.FullName);

    private static DateTime? AsUtc(DateTime? value) =>
        value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : null;
}
