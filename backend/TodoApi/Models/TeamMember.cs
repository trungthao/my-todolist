namespace TodoApi.Models;

public class TeamMember
{
    public int TeamId { get; set; }
    public Team Team { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public TeamRole Role { get; set; } = TeamRole.Member;
    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;
}
