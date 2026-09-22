namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.Todo;

    // Seconds accumulated across all past "In Progress" stints.
    public long AccumulatedSeconds { get; set; }

    // Set while the task is currently "In Progress"; null otherwise.
    public DateTime? CurrentStartedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
}
