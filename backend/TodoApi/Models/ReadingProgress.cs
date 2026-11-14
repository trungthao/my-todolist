namespace TodoApi.Models;

public class ReadingProgress
{
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int DocumentId { get; set; }
    public ReadingDocument Document { get; set; } = null!;
    public float ScrollPercent { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
