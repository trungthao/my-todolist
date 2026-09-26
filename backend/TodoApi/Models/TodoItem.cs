namespace TodoApi.Models;

public class TodoItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TodoStatus Status { get; set; } = TodoStatus.Todo;
    public long AccumulatedSeconds { get; set; }
    public DateTime? CurrentStartedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAtUtc { get; set; }
    public int? TeamId { get; set; }
    public Team? Team { get; set; }
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public int? AssignedToUserId { get; set; }
    public User? AssignedTo { get; set; }
}
