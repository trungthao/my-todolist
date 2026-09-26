namespace TodoApi.Models;

public class Team
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public int? CreatedByUserId { get; set; }
    public User? CreatedBy { get; set; }
    public ICollection<TeamMember> Members { get; set; } = [];
    public ICollection<TodoItem> Tasks { get; set; } = [];
}
